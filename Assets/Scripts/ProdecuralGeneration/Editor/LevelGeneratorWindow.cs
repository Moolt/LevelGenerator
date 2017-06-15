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
	private bool randomizeSeed = false;
	//Preset Properties
	//private string presetPath = @"/Resources/Presets/";
	private string currentPresetPath = "";
	//private string presetDefaultName = "New Preset";
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
		string presetLabelText = isExternPreset ? GlobalPaths.PresetPath + presetName : "Unsaved";
		EditorGUILayout.LabelField ("Preset: " + presetLabelText);

		EditorGUILayout.Space ();
		EditorGUILayout.BeginHorizontal ();
		if (GUILayout.Button ("Generate Level", EditorStyles.miniButtonLeft)) {
			Generate ();
		}
		GUI.enabled = !isAutoUpdate;
		if (GUILayout.Button ("Clear", EditorStyles.miniButtonRight)) {
			ClearLevel ();
		}
		GUI.enabled = true;
		EditorGUILayout.EndHorizontal ();
		EditorGUILayout.Space ();
		randomizeSeed = EditorGUILayout.Toggle ("Randomize Seed", randomizeSeed);
		GUI.enabled = !randomizeSeed;
		preset.Seed = EditorGUILayout.IntField ("Seed", preset.Seed);
		GUI.enabled = true;

		scrollVector = EditorGUILayout.BeginScrollView(scrollVector, GUILayout.Height(position.height - 100));
		#region LevelGraphProperties
		EditorGUILayout.Space ();
		showLevelGraph = EditorGUILayout.Foldout (showLevelGraph, "Level Graph Properties");
		if (showLevelGraph) {
			EditorGUI.indentLevel += 1;
			preset.RoomCount = EditorGUILayout.IntField ("Room Count", preset.RoomCount);
			preset.RoomCount = Mathf.Clamp (preset.RoomCount, 2, 1000);
			preset.CritPathLength = EditorGUILayout.IntField ("Main Path", preset.CritPathLength);
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
			preset.RoomDistance = Mathf.Clamp (preset.RoomDistance, 0.1f, 10f);

			if (preset.IsSeparateRooms) {
				preset.Spacing = EditorGUILayout.FloatField ("Minimal margin", preset.Spacing);
				preset.Spacing = Mathf.Clamp (preset.Spacing, DoorDefinition.GlobalSize * 2f, DoorDefinition.GlobalSize * 4f);
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
				constraint.Target = (ConstraintTarget)EditorGUILayout.EnumPopup("Target", constraint.Target);

				if(constraint.Target == ConstraintTarget.Hallways){
					constraint.HallwayAmount = (HallwayAmount)EditorGUILayout.EnumPopup("Amount", constraint.HallwayAmount);
					if(constraint.HallwayAmount == HallwayAmount.AtMost){
						DrawAmountValueInput(constraint, false);
					}
					if(constraint.HallwayAmount == HallwayAmount.SetPriority){
						constraint.HallwayPriority = EditorGUILayout.FloatField("Priority", constraint.HallwayPriority);
						constraint.HallwayPriority = Mathf.Clamp(constraint.HallwayPriority, 0f, 1f);
						EditorGUILayout.LabelField(" ", "(Value larger than 0.5 cancels out randomness)");
					}
					DrawTagSelection(constraint, TagTarget.HALLWAY);
				} else{
					constraint.Type = (ConstraintType)EditorGUILayout.EnumPopup("Type", constraint.Type);

					if(constraint.Target == ConstraintTarget.AllRooms ||
						constraint.Target == ConstraintTarget.MiddleRooms ||
						constraint.Target == ConstraintTarget.SideRooms){
						constraint.Amount = (ConstraintAmount)EditorGUILayout.EnumPopup("Rooms", constraint.Amount);

						if(constraint.Amount != ConstraintAmount.All && constraint.Amount != ConstraintAmount.None){
							DrawAmountValueInput(constraint, true);
						}
					}

					if(constraint.Type == ConstraintType.FuzzyProperties){
						constraint.AutoTagIndex = EditorGUILayout.Popup("Tag", constraint.AutoTagIndex, FuzzyTagDictionary.Descriptors);
						EditorGUILayout.MinMaxSlider(ref constraint.Min, ref constraint.Max, 0f, 1f);
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.LabelField(FuzzyTagDictionary.FindAttribute(constraint.AutoTagIndex, constraint.Min));
						EditorGUILayout.LabelField(FuzzyTagDictionary.FindAttribute(constraint.AutoTagIndex, constraint.Max), GUILayout.Width(80));
						EditorGUILayout.EndHorizontal();
					} else{
						DrawTagSelection(constraint, TagTarget.CHUNK);
					}
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
			if (GUILayout.Button ("Add Constraint", EditorStyles.miniButton)) {
				constraints.Add(new Constraint());
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();
		}
		#endregion

		#region Debug
		ManageDebugObject ();
		showDebugGUI = EditorGUILayout.Foldout (showDebugGUI, "Debug");
		if (showDebugGUI) {
			preset.IsSeparateRooms = EditorGUILayout.Toggle ("Separate Rooms", preset.IsSeparateRooms);
			debugInfo.ShowPaths = EditorGUILayout.Toggle ("Show paths", debugInfo.ShowPaths);
			debugInfo.ShowConnections = EditorGUILayout.Toggle ("Show connections", debugInfo.ShowConnections);
			debugInfo.ShowAStarGrid = EditorGUILayout.Toggle ("Path grid", debugInfo.ShowAStarGrid);
			debugInfo.ShowRoomTypes = EditorGUILayout.Toggle ("Show room types", debugInfo.ShowRoomTypes);
			debugInfo.SetStatic = EditorGUILayout.Toggle ("Set rooms static", debugInfo.SetStatic);
		}

		EditorGUILayout.Space ();
		#endregion

		isAutoUpdate = EditorGUILayout.Toggle ("Auto Update", isAutoUpdate);

		EditorGUILayout.EndScrollView();

		if (isAutoUpdate) {
			int startMillis = System.DateTime.Now.Millisecond;
			Generate ();
			if (System.DateTime.Now.Millisecond - startMillis > 500) {
				isAutoUpdate = false;
			}
		}
	}

	private void DrawAmountValueInput(Constraint constraint, bool useChunkValues){
		EditorGUILayout.BeginHorizontal();
		if(constraint.AmountType == ConstraintAmountType.Absolute){
			constraint.AbsoluteAmount = EditorGUILayout.IntField("Amount (absolute)", constraint.AbsoluteAmount);
			int maxVal = 0;

			if (useChunkValues) {
				switch (constraint.Target) {
				case ConstraintTarget.AllRooms:
					maxVal = preset.RoomCount;
					break;
				case ConstraintTarget.SideRooms:
					maxVal = preset.RoomCount - preset.CritPathLength;
					break;
				case ConstraintTarget.MiddleRooms:
					maxVal = preset.CritPathLength;
					break;
				}
			} else {
				maxVal = 1000;
			}
			constraint.AbsoluteAmount = Mathf.Clamp(constraint.AbsoluteAmount, 0, maxVal);
		} else{
			constraint.RelativeAmount = EditorGUILayout.FloatField("Amount (relative)", constraint.RelativeAmount);
			constraint.RelativeAmount = Mathf.Clamp(constraint.RelativeAmount, 0f, 1f);
		}
		string toggleButtonText = constraint.AmountType == ConstraintAmountType.Absolute ? "P" : "A";
		if (GUILayout.Button (toggleButtonText, GUILayout.Width(20))) {
			if(constraint.AmountType == ConstraintAmountType.Absolute){
				constraint.AmountType = ConstraintAmountType.Percentual;
			} else {
				constraint.AmountType = ConstraintAmountType.Absolute;
			}
		}
		EditorGUILayout.EndHorizontal();
	}

	private void DrawTagSelection(Constraint constraint, TagTarget target){
		EditorGUILayout.BeginHorizontal();
		constraint.RawTags = EditorGUILayout.TextField("Containing tag(s)", constraint.RawTags);
		if (GUILayout.Button (constraint.TagMethod.ToString(), GUILayout.Width(32))) {
			constraint.ToggleTagMethod ();
		}
		GUI.SetNextControlName("PlusButton");
		if (GUILayout.Button ("+", GUILayout.Width(20))) {
			GUI.FocusControl("PlusButton");
			List<string> userTags = new List<string>();
			string[] allUserTags = ChunkTags.GlobalUserTags (target);
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
		if(randomizeSeed){
			preset.Seed = Random.Range (0, 10000);
		}
		Random.InitState (preset.Seed);
		levelGraph = new LevelGraph ();
		levelGraph.GenerateGraph (preset.RoomCount, preset.CritPathLength, preset.MaxDoors, preset.Distribution);
		ProceduralLevel level = new ProceduralLevel (levelGraph, preset, debugInfo.SetStatic);
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
		LevelGenerator.ClearLevel ();
		GameObject _debug = GameObject.FindGameObjectWithTag ("Debug");
		if (_debug != null) {
			DestroyImmediate (_debug);
		}
	}

	private void ResetValues(){
		isExternPreset = false;
		presetName = GlobalPaths.PresetDefaultName;
		currentPresetPath = "";
		
		if (preset != null) {
			preset.Reset ();
		}
	}

	private void Separator(){
		GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
	}

	private void SavePreset(bool isShowDialog){
		string absolutePath = Application.dataPath + GlobalPaths.PresetPath;
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
		string absolutePath = Application.dataPath + GlobalPaths.PresetPath;
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
