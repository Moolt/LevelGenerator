using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class LevelGeneratorWindow : EditorWindow {
	//Level Graph Properties
	private int roomCount;
	private int critPathLength;
	private int maxDoors;
	private float distribution;
	private LevelGraph levelGraph;
	//Procedural Level Properties
	private float roomDistance;
	private float spacing;
	private int seed = 0;
	//GUI Properties
	private bool showLevelGraph = true;
	private bool showProceduralLevel = true;
	private string path = "Chunks";
	//Used to delete old object before generating new ones
	private bool isAutoUpdate = false;
	private bool isSeparateRooms = true;

	[MenuItem("Window/Level Generator")]
	public static void ShowWindow(){
		EditorWindow.GetWindow (typeof(LevelGeneratorWindow));
	}

	void OnGUI(){

		EditorGUILayout.Space ();
		showLevelGraph = EditorGUILayout.Foldout (showLevelGraph, "Level Graph Properties");
		if (showLevelGraph) {
			roomCount = EditorGUILayout.IntField ("Room Count", roomCount);
			roomCount = Mathf.Clamp (roomCount, 2, 100);
			critPathLength = EditorGUILayout.IntField ("Critical Path", critPathLength);
			critPathLength = Mathf.Clamp (critPathLength, Mathf.Min (2, roomCount), Mathf.Max (2, roomCount));
			maxDoors = EditorGUILayout.IntField ("Max. Doors", maxDoors);
			maxDoors = Mathf.Clamp (maxDoors, 3, 10);
			distribution = EditorGUILayout.Slider ("Distribution", distribution, 0.05f, 1f);
		}

		EditorGUILayout.Space ();

		showProceduralLevel = EditorGUILayout.Foldout (showProceduralLevel, "Level Properties");
		if (showProceduralLevel) {
			roomDistance = EditorGUILayout.FloatField ("Distance", roomDistance);
			roomDistance = Mathf.Max (1.5f, roomDistance);

			EditorGUILayout.Space ();
			isSeparateRooms = EditorGUILayout.Toggle ("Separate Rooms", isSeparateRooms);
			if (isSeparateRooms) {
				spacing = EditorGUILayout.FloatField ("Spacing", spacing);
				spacing = Mathf.Max (0f, spacing);
			}
			EditorGUILayout.Space ();
		}

		EditorGUILayout.Space ();

		seed = EditorGUILayout.IntField ("Seed", seed);
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
		Random.InitState (seed);
		levelGraph = new LevelGraph ();
		levelGraph.GenerateGraph (roomCount, critPathLength, maxDoors, distribution);
		ProceduralLevel level = new ProceduralLevel (path, levelGraph, isSeparateRooms, roomDistance, isSeparateRooms, spacing);
		//generatedObjects = level.GeneratedRooms;
	}

	private void ClearLevel(){
		GameObject[] instances = GameObject.FindGameObjectsWithTag ("ChunkInstance");
		foreach (GameObject room in instances) {
			DestroyImmediate (room);
		}
	}
}
