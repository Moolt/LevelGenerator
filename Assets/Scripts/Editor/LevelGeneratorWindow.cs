using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Xml.Serialization;
using System.IO;
using System.Text;

public struct TagMenuData{
	public Constraint constraint;
	public string Tag;

	public TagMenuData (Constraint constraint, string tag){
		this.constraint = constraint;
		this.Tag = tag;
	}
}

public class LevelGeneratorWindow : EditorWindow {
	private LevelGraph levelGraph;
	//Preset Properties
	private string presetPath = @"/Resources/Presets/";
	private string currentPresetPath = "";
	private string presetDefaultName = "New Preset";
	private string presetName = "New Preset";
	private bool isExternPreset = false;
	//GUI Properties
	private bool showProceduralLevel = true;
	private XmlSerializer xmlSerializer;
	private LevelGeneratorPreset preset;
	private bool showLevelGraph = true;
	private bool showDebugGUI = false;
	private bool showHallwayGUI = false;
	private bool showConstraintGUI = false;
	private bool isAutoUpdate = false;
	//Constraints
	private Vector2 scrollVector = Vector2.zero;
	//Debugging
	private DebugInfo debugInfo;
	private DebugData debugData;
	private DebugGizmo debugGizmo;

	[MenuItem("Window/Level Generator")]
	public static void ShowWindow(){
		EditorWindow.GetWindow (typeof(LevelGeneratorWindow));
	}

	void OnEnable(){

		if (preset == null) {
			preset = new LevelGeneratorPreset ();
			preset.Reset ();
		}
		if (debugInfo == null) {
			debugInfo = new DebugInfo ();
		}
		xmlSerializer = new XmlSerializer (typeof(LevelGeneratorPreset));
	}

	void OnGUI(){

		EditorGUILayout.Space ();

		EditorGUILayout.BeginHorizontal ();
		if(GUILayout.Button ("Save", EditorStyles.miniButtonLeft)){
			SavePreset (false);
		}
		if(GUILayout.Button ("Load", EditorStyles.miniButtonMid)){
			LoadPreset ();
		}
		if(GUILayout.Button ("Save as...", EditorStyles.miniButtonMid)){
			SavePreset (true);
		}
		if(GUILayout.Button ("Reset", EditorStyles.miniButtonRight)){
			ResetValues ();
		}
		EditorGUILayout.EndHorizontal();
		string presetLabelText = isExternPreset ? presetPath + presetName : "Unsaved";
		EditorGUILayout.LabelField ("Preset: " + presetLabelText);

		scrollVector = EditorGUILayout.BeginScrollView(scrollVector, GUILayout.Height(400));
		#region LevelGraphProperties
		EditorGUILayout.Space ();
		showLevelGraph = EditorGUILayout.Foldout (showLevelGraph, "Level Graph Properties");
		if (showLevelGraph) {
			EditorGUI.indentLevel += 1;
			preset.RoomCount = EditorGUILayout.IntField ("Room Count", preset.RoomCount);
			preset.RoomCount = Mathf.Clamp (preset.RoomCount, 2, 100);
			preset.CritPathLength = EditorGUILayout.IntField ("Critical Path", preset.CritPathLength);
			preset.CritPathLength = Mathf.Clamp (preset.CritPathLength, Mathf.Min (2, preset.RoomCount), Mathf.Max (2, preset.RoomCount));
			preset.MaxDoors = EditorGUILayout.IntField ("Max. Doors", preset.MaxDoors);
			preset.MaxDoors = Mathf.Clamp (preset.MaxDoors, 3, 10);
			preset.Distribution = EditorGUILayout.Slider ("Distribution", preset.Distribution, 0.05f, 1f);
			EditorGUI.indentLevel -= 1;
			EditorGUILayout.Space ();
		}
		#endregion

		#region LevelProperties
		showProceduralLevel = EditorGUILayout.Foldout (showProceduralLevel, "Level Properties");
		if (showProceduralLevel) {
			EditorGUI.indentLevel += 1;
			//preset.DoorSize = EditorGUILayout.IntField ("Global door size", preset.DoorSize);
			//preset.DoorSize = (int)Mathf.Floor (Mathf.Clamp (preset.DoorSize, 2f, preset.Spacing / 2f));
			preset.RoomDistance = EditorGUILayout.FloatField ("Global distance", preset.RoomDistance);
			preset.RoomDistance = Mathf.Max (1.5f, preset.RoomDistance);

			if (preset.IsSeparateRooms) {
				preset.Spacing = EditorGUILayout.FloatField ("Minimal margin", preset.Spacing);
				preset.Spacing = Mathf.Clamp (preset.Spacing, preset.DoorSize * 2f, preset.DoorSize * 4f);
			}
			EditorGUI.indentLevel -= 1;
			EditorGUILayout.Space ();
		}
		#endregion

		#region HallwayProperties
		showHallwayGUI = EditorGUILayout.Foldout (showHallwayGUI, "Hallway");
		if (showHallwayGUI) {
			EditorGUI.indentLevel += 1;
			preset.HallwayTiling = EditorGUILayout.FloatField ("Texture tiling:", preset.HallwayTiling);
			preset.HallwayTiling = Mathf.Clamp (preset.HallwayTiling, 0.01f, 10f);
			EditorGUILayout.Space ();
			EditorGUILayout.LabelField ("Materials:");
			preset.HallwayMaterials [0] = EditorGUILayout.ObjectField ("Ceil", preset.HallwayMaterials [0], typeof(Material), false) as Material;
			preset.HallwayMaterials [1] = EditorGUILayout.ObjectField ("Floor", preset.HallwayMaterials [1], typeof(Material), false) as Material;
			preset.HallwayMaterials [2] = EditorGUILayout.ObjectField ("Walls", preset.HallwayMaterials [2], typeof(Material), false) as Material;
			EditorGUI.indentLevel -= 1;
			EditorGUILayout.Space ();
		}
		#endregion

		#region Constraints
		showConstraintGUI = EditorGUILayout.Foldout (showConstraintGUI, "Constraints");
		if (showConstraintGUI) {
			List<Constraint> constraints = preset.Constraints;
			List<Constraint> toDelete = new List<Constraint>();

			Separator();
			EditorGUILayout.Space();
			for (int i = 0; i < constraints.Count; i++) {				
				Constraint constraint = constraints[i];
				constraint.Type = (ConstraintType)EditorGUILayout.EnumPopup("Type", constraint.Type);
				constraint.Target = (ConstraintTarget)EditorGUILayout.EnumPopup("Target", constraint.Target);

				if(constraint.Type == ConstraintType.FuzzyProperties){
					constraint.AutoTagIndex = EditorGUILayout.Popup("Tag", constraint.AutoTagIndex, FuzzyTagDictionary.Descriptors);
					EditorGUILayout.MinMaxSlider(ref constraint.Min, ref constraint.Max, 0f, 1f);
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(FuzzyTagDictionary.FindAttribute(constraint.AutoTagIndex, constraint.Min));
					EditorGUILayout.LabelField(FuzzyTagDictionary.FindAttribute(constraint.AutoTagIndex, constraint.Max), GUILayout.Width(80));
					EditorGUILayout.EndHorizontal();
				} else{
					EditorGUILayout.BeginHorizontal();
					constraint.RawTags = EditorGUILayout.TextField("User Tags", constraint.RawTags);
					GUI.SetNextControlName("PlusButton");
					if (GUILayout.Button ("+", GUILayout.Width(20))) {
						GUI.FocusControl("PlusButton");
						int selectedTag = 0;
						List<string> userTags = new List<string>();
						string[] allUserTags = ChunkHelper.GlobalUserTags;
						//Filtering all tags that are already used
						allUserTags.ToList()
							.Where(s => !constraint.ParsedTags.Contains(s)).ToList()
							.ForEach(s => userTags.Add(s));

						TagContextMenu(userTags, constraint);
						//selectedTag = EditorGUILayout.GetControlRect(selectedTag, userTags);
						//constraint.RawTags += ";" + userTags[selectedTag];
					}
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.LabelField(" ", "(Separated by semicolons)");
				}

				if (GUILayout.Button ("Remove", EditorStyles.miniButton)) {
					toDelete.Add(constraints[i]);
				}
				//EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space();
				Separator();
				EditorGUILayout.Space();
			}

			toDelete.ForEach(c => constraints.Remove(c));

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button ("Add Constraint", EditorStyles.miniButtonLeft)) {
				constraints.Add(new Constraint());
			}
			if (GUILayout.Button ("Display User Tags", EditorStyles.miniButtonRight)) {
				constraints.Add(new Constraint());
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();
		}
		#endregion
		ManageDebugObject ();
		showDebugGUI = EditorGUILayout.Foldout (showDebugGUI, "Debug");
		if (showDebugGUI) {
			preset.IsSeparateRooms = EditorGUILayout.Toggle ("Separate Rooms", preset.IsSeparateRooms);
			debugInfo.ShowPaths = EditorGUILayout.Toggle ("Show paths", debugInfo.ShowPaths);
			debugInfo.ShowConnections = EditorGUILayout.Toggle ("Show connections", debugInfo.ShowConnections);
			debugInfo.ShowAStarGrid = EditorGUILayout.Toggle ("Path grid", debugInfo.ShowAStarGrid);
		}

		EditorGUILayout.Space ();

		preset.Seed = EditorGUILayout.IntField ("Seed", preset.Seed);
		isAutoUpdate = EditorGUILayout.Toggle ("Auto Update", isAutoUpdate);

		EditorGUILayout.EndScrollView();
		EditorGUILayout.Space ();

		EditorGUILayout.BeginHorizontal ();
		if (GUILayout.Button ("Generate Level")) {
			Generate ();
		}
		GUI.enabled = !isAutoUpdate;
		if (GUILayout.Button ("Clear")) {
			ClearLevel ();
		}
		GUI.enabled = true;
		if (isAutoUpdate) {
			int startMillis = System.DateTime.Now.Millisecond;
			Generate ();
			if (System.DateTime.Now.Millisecond - startMillis > 500) {
				isAutoUpdate = false;
			}
		}

		EditorGUILayout.EndHorizontal ();
	}

	private void ManageDebugObject(){
		GameObject debugObject = GameObject.FindWithTag ("Debug");
		if (debugObject == null && debugInfo.IsDebugUsed) {
			GameObject debug = new GameObject ("Debug");
			debug.tag = "Debug";
			debugGizmo = debug.AddComponent<DebugGizmo> ();
			debugGizmo.DebugInfo = debugInfo;

			if (debugData != null) {
				debugGizmo.DebugData = debugData;
			}

		} else if (debugObject != null && !debugInfo.IsDebugUsed) {
			DestroyImmediate (debugObject);
			debugGizmo = null;
		}
	}

	private void Generate(){
		ClearLevel ();
		Random.InitState (preset.Seed);
		levelGraph = new LevelGraph ();
		levelGraph.GenerateGraph (preset.RoomCount, preset.CritPathLength, preset.MaxDoors, preset.Distribution);
		ProceduralLevel level = new ProceduralLevel (levelGraph, preset);
		SetDebugData (level.DebugData);
		//generatedObjects = level.GeneratedRooms;
	}

	private void SetDebugData(DebugData data){
		debugData = data;
		if (debugGizmo != null) {
			debugGizmo.DebugData = data;
		}
	}

	private void ClearLevel(){
		GameObject[] instances = GameObject.FindGameObjectsWithTag ("ChunkInstance");
		foreach (GameObject room in instances) {
			DestroyImmediate (room);
		}
	}

	private void ResetValues(){
		isExternPreset = false;
		presetName = presetDefaultName;
		currentPresetPath = "";
		
		if (preset != null) {
			preset.Reset ();
		}
	}

	private void Separator(){
		GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
	}

	private void SavePreset(bool isShowDialog){
		string absolutePath = Application.dataPath + presetPath;
		Directory.CreateDirectory (absolutePath);
		string path;

		if (isShowDialog || !isExternPreset) {
			path = EditorUtility.SaveFilePanelInProject ("Save Preset", presetName, "xml", "", absolutePath);
			currentPresetPath = path;
		} else {
			path = currentPresetPath;
		}

		if (path.Length != 0) {
			presetName = Path.GetFileName (path);
			FileStream fileStream = new FileStream (path, FileMode.Create);
			using (TextWriter t = new StreamWriter (fileStream, new UnicodeEncoding ())) {
				xmlSerializer.Serialize (t, preset);
			}
			fileStream.Close ();
			isExternPreset = true;
		}
	}

	private void LoadPreset(){
		string absolutePath = Application.dataPath + presetPath;
		Directory.CreateDirectory (absolutePath);
		string path = EditorUtility.OpenFilePanel ("Load Preset", absolutePath, "xml");
		if (path.Length != 0) {
			if (File.Exists (path)) {
				isExternPreset = true;
				presetName = Path.GetFileName (path);
				FileStream fileStream = new FileStream (path, FileMode.Open);
				LevelGeneratorPreset loadedPreset = xmlSerializer.Deserialize (fileStream) as LevelGeneratorPreset;
				loadedPreset.LoadMaterials ();
				fileStream.Close ();
				if (loadedPreset != null) {
					preset = loadedPreset;
				}
			}
		}
	}

	private void TagContextMenu(List<string> items, Constraint constraint){
		GenericMenu contextMenu = new GenericMenu();
		foreach (string s in items) {
			contextMenu.AddItem (new GUIContent (s), false, TagMenuCallback, new TagMenuData(constraint, s));
		}
		contextMenu.ShowAsContext();
	}

	public void TagMenuCallback(object o){
		TagMenuData data = (TagMenuData)o;
		data.constraint.RawTags += data.constraint.RawTags.Length == 0 ? "" : ";";
		data.constraint.RawTags += data.Tag;
		Repaint ();
	}
}
