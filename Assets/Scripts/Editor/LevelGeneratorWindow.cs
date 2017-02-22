using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Xml.Serialization;
using System.IO;

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
	private bool isAutoUpdate = false;
	private string chunkPath = "Chunks";

	[MenuItem("Window/Level Generator")]
	public static void ShowWindow(){
		EditorWindow.GetWindow (typeof(LevelGeneratorWindow));
	}

	void OnEnable(){
		if (preset == null) {
			preset = new LevelGeneratorPreset ();
			preset.Reset ();
		}
		xmlSerializer = new XmlSerializer (typeof(LevelGeneratorPreset));
	}

	void OnGUI(){

		EditorGUILayout.Space ();

		EditorGUILayout.BeginHorizontal ();
		if(GUILayout.Button ("Save")){
			SavePreset (false);
		}
		if(GUILayout.Button ("Load")){
			LoadPreset ();
		}
		if(GUILayout.Button ("Save as...")){
			SavePreset (true);
		}
		if(GUILayout.Button ("Reset")){
			ResetValues ();
		}
		EditorGUILayout.EndHorizontal();
		string presetLabelText = isExternPreset ? presetPath + presetName : "Unsaved";
		EditorGUILayout.LabelField ("Preset: " + presetLabelText);

		EditorGUILayout.Space ();
		showLevelGraph = EditorGUILayout.Foldout (showLevelGraph, "Level Graph Properties");
		if (showLevelGraph) {
			preset.RoomCount = EditorGUILayout.IntField ("Room Count", preset.RoomCount);
			preset.RoomCount = Mathf.Clamp (preset.RoomCount, 2, 100);
			preset.CritPathLength = EditorGUILayout.IntField ("Critical Path", preset.CritPathLength);
			preset.CritPathLength = Mathf.Clamp (preset.CritPathLength, Mathf.Min (2, preset.RoomCount), Mathf.Max (2, preset.RoomCount));
			preset.MaxDoors = EditorGUILayout.IntField ("Max. Doors", preset.MaxDoors);
			preset.MaxDoors = Mathf.Clamp (preset.MaxDoors, 3, 10);
			preset.Distribution = EditorGUILayout.Slider ("Distribution", preset.Distribution, 0.05f, 1f);
		}

		EditorGUILayout.Space ();

		showProceduralLevel = EditorGUILayout.Foldout (showProceduralLevel, "Level Properties");
		if (showProceduralLevel) {
			preset.DoorSize = EditorGUILayout.IntField ("Global door size", preset.DoorSize);
			preset.DoorSize = (int)Mathf.Max (2f, preset.DoorSize);
			preset.RoomDistance = EditorGUILayout.FloatField ("Distance", preset.RoomDistance);
			preset.RoomDistance = Mathf.Max (1.5f, preset.RoomDistance);

			EditorGUILayout.Space ();
			preset.IsSeparateRooms = EditorGUILayout.Toggle ("Separate Rooms", preset.IsSeparateRooms);
			if (preset.IsSeparateRooms) {
				preset.Spacing = EditorGUILayout.FloatField ("Spacing", preset.Spacing);
				preset.Spacing = Mathf.Max (0f, preset.Spacing);
			}
			EditorGUILayout.Space ();
		}

		EditorGUILayout.Space ();

		preset.Seed = EditorGUILayout.IntField ("Seed", preset.Seed);
		isAutoUpdate = EditorGUILayout.Toggle ("Auto Update", isAutoUpdate);

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
			Generate ();
		}

		EditorGUILayout.EndHorizontal ();
	}

	private void Generate(){
		ClearLevel ();
		Random.InitState (preset.Seed);
		levelGraph = new LevelGraph ();
		levelGraph.GenerateGraph (preset.RoomCount, preset.CritPathLength, preset.MaxDoors, preset.Distribution);
		ProceduralLevel level = new ProceduralLevel (chunkPath, levelGraph, preset.IsSeparateRooms, preset.RoomDistance, preset.IsSeparateRooms, preset.Spacing, preset.DoorSize);
		//generatedObjects = level.GeneratedRooms;
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
			FileStream fileStream = new FileStream (path, FileMode.OpenOrCreate);
			xmlSerializer.Serialize (fileStream, preset);
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
				fileStream.Close ();
				if (loadedPreset != null) {
					preset = loadedPreset;
				}
			}
		}
	}
}
