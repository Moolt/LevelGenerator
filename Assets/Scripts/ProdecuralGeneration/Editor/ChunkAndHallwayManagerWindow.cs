using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System.IO;
using System;

public enum WorkingType{ CHUNK, HALLWAY }

/// <summary>
/// This class was originally only programmed for Chunks and later modified to also work with hallways.
/// Many variable names and functions were changed to fit this decision, but there are also many exceptions.
/// </summary>
public class ChunkAndHallwayManagerWindow : EditorWindow {

	private WorkingType workingType;
	private WorkingType prevType;
	private GameObject originalObject;
	private GameObject objectInstance;
	private int seed;
	private int prevSeed;
	private int durationMillis = 0;

	[MenuItem("Window/Chunk and Hallway Manager")]
	public static void ShowWindow(){
		EditorWindow.GetWindow (typeof(ChunkAndHallwayManagerWindow));
	}

	void Awake(){
		Debug.Log ("enable");
		workingType = TypeInScene;
		prevType = workingType;
	}

	// Use this for initialization
	void OnGUI(){

		EditorGUILayout.Space();

		EditorGUILayout.BeginHorizontal ();

		if (GUILayout.Button ("New", EditorStyles.miniButtonLeft)) {
			CreateNewChunk ();
		}

		GUI.enabled = OriginalObject != null;
		if (GUILayout.Button ("Save", EditorStyles.miniButtonMid)) {
			SaveChunk ();
		}
		GUI.enabled = true;

		if (GUILayout.Button ("Load", EditorStyles.miniButtonRight)) {
			LoadChunk ();
		}

		EditorGUILayout.EndHorizontal ();

		prevType = workingType;
		workingType = (WorkingType)EditorGUILayout.EnumPopup ("Target", workingType);

		if (workingType != prevType) {
			WorkingTypeChanged ();
		}

		if (OriginalObject == null) {	
			EditorGUILayout.HelpBox ("No " + WorkingTypeName + " found in the scene. " + WorkingTypeName + "s have to be tagged with " + 
				DefaultTag + " and there should only be one instance at a time", MessageType.Warning);
		}

		string chunkName = OriginalObject == null ? "none" : OriginalObject.name;

		EditorGUILayout.LabelField (WorkingTypeName + " name", chunkName);
		EditorGUILayout.LabelField ("Path", GlobalPaths.AbsoluteChunkPath);

		GUILayout.Space (20);

		EditorGUILayout.Space ();

		EditorGUILayout.BeginHorizontal ();
		GUI.enabled = OriginalObject != null;

		if (GUILayout.Button ("Preview " + WorkingTypeName, EditorStyles.miniButtonLeft)) {
			InstantiateChunk (DateTime.Now.Millisecond);
		}

		if (GUILayout.Button ("Restore", EditorStyles.miniButtonRight)) {
			Restore ();
		}
		EditorGUILayout.EndHorizontal ();

		seed = EditorGUILayout.IntField ("Seed", seed);
		EditorGUILayout.LabelField ("Generation time", durationMillis.ToString() + " ms");

		//Update Scene when seed changes
		if (seed != prevSeed) {
			InstantiateChunk (seed);
		}
		GUI.enabled = true;
		prevSeed = seed;
	}

	private void InstantiateChunk(int _seed){
		int startMillis = DateTime.Now.Millisecond;
		UnityEngine.Random.InitState (_seed);

		SceneUpdater.SetActive (false);
		DestroyMultiple (SceneInstances); //Remove old generated chunk
		SetObjectActive (true); //Has to be active or else the copy could be inactive, too
		GameObject instance = (GameObject)GameObject.Instantiate (OriginalObject, OriginalObject.transform.position , Quaternion.identity);
		GameObject instantiatable = FindInstantiatable (instance);
		SetObjectActive (false);
		ChunkInstantiator generator = ChunkInstantiator.Instance;
		generator.ProcessType = ProcessType.INEDITOR;
		generator.InstantiateChunk (instantiatable, false);
		instance.tag = InstanceTag;
		durationMillis = DateTime.Now.Millisecond - startMillis;
	}

	private void WorkingTypeChanged(){
		string text = "When changing the working type, any other hallway or chunk objects have to be removed from the scene first.\nRemove instances?";
		bool dialogResult = EditorUtility.DisplayDialog ("Working type changed", text, "Yes", "Cancel");
		if (dialogResult) {
			DestroyMultiple (SceneInstances);
			DestroyMultiple (SceneObjects);
			OriginalObject = null;
		} else {
			workingType = prevType;
		}
	}

	//Remvoes any old copy of the chunk and sets the original, abstract chunk active
	private void Restore(){
		DestroyMultiple (SceneInstances); //Destroy the generated chunk
		SetObjectActive(true);
		SceneUpdater.SetActive (true);
		SceneUpdater.UpdateScene ();
	}

	//Shows a Dialog, asking whether the current chunk should be saved first
	//Used by "New" and "Load", since the remove the current chunk
	private void ShowSaveFirstDialog(){
		string typeName = IsChunk ? "Chunk" : "Hallway";
		string text = "Loading a new " + typeName + " into the scene requires the old one to be removed first. Do you want to safe your progress before loading?";
		bool dialogResult = EditorUtility.DisplayDialog ("Save progress", text, "Save", "Don't save");

		if (dialogResult) {
			SaveChunk ();
		}
	}

	private void CreateNewChunk(){
		Restore ();
		ShowSaveFirstDialog ();
		DestroyMultiple (SceneObjects);
		UnityEngine.Object newPrefab = Resources.Load (RelativePath + "/" + NewName);
		if (newPrefab != null) {
			OriginalObject = (GameObject)GameObject.Instantiate (newPrefab);
			SceneUpdater.UpdateScene ();
		} else {
			if (IsChunk) {
				Debug.LogError ("Default Chunk could not be loaded. Your Chunks folder must contain a default Chunk named \"NewChunk\"");
			} else {
				Debug.LogError ("Default Hallway could not be loaded. Your Hallways folder must contain a Prefab named \"HallwayTemplate\"");
			}
		}
	}

	private void SaveChunk(){
		Restore ();
		if (OriginalObject != null) {
			System.IO.Directory.CreateDirectory (AbsolutePath); //Create folders if they don't yet exist
			string title = IsChunk ? "Save Chunk" : "Save Hallway Template";
			string dialogPath = EditorUtility.SaveFilePanelInProject (title, OriginalObject.name, "prefab", "", AbsolutePath);

			if (dialogPath.Length > 0) {
				Debug.Log (dialogPath);
				PrefabUtility.CreatePrefab (dialogPath, OriginalObject, ReplacePrefabOptions.ConnectToPrefab);
			}
		} else {
			if (IsChunk) {
				EditorUtility.DisplayDialog ("No chunk found", "There is no GameObject with the tag \"Chunk\" in your scene or it is set inactive", "OK");
			} else {
				EditorUtility.DisplayDialog ("No hallway template found", "There is no GameObject with the tag \"HallwayTemplate\" in your scene or it is set inactive", "OK");
			}
		}
	}

	private void LoadChunk(){
		Restore ();
		ShowSaveFirstDialog ();
		System.IO.Directory.CreateDirectory (GlobalPaths.AbsoluteChunkPath); //Create folders if they don't yet exist
		System.IO.Directory.CreateDirectory (GlobalPaths.AbsoluteHallwayPath); //Create folders if they don't yet exist
		string title = IsChunk ? "Load Chunk" : "Load Hallway";
		string dialogPath = EditorUtility.OpenFilePanel (title, AbsolutePath, "prefab");
		if (dialogPath.Length > 0) {
			//Since the dialog outputs the complete path, which Resources.Load doesn't work with, a relative path is used
			//Resources.Load only works with files inside the Resources folder. The path therefore only has to contain the folder the chunks
			//Are stored in+
			string parentFolder = Path.GetDirectoryName(dialogPath).Split(new string[]{"Resources"}, StringSplitOptions.None)[1];
			string filePath = parentFolder + "/" + System.IO.Path.GetFileNameWithoutExtension (dialogPath);
			if (filePath.StartsWith ("/")) {
				filePath = filePath.Remove (0, 1);
			}
			//Debug.Log (filePath);
			//OriginalChunk.tag = "ChunkRemove";
			DestroyMultiple(SceneObjects);
			OriginalObject = null;
			//OriginalChunk = (GameObject)GameObject.Instantiate (Resources.Load (filePath));
			OriginalObject = (GameObject) PrefabUtility.InstantiatePrefab (Resources.Load (filePath));
			OriginalObject.transform.position = Vector3.zero;
			//PrefabUtility.DisconnectPrefabInstance(OriginalChunk);
			SceneUpdater.UpdateScene ();
		}
	}

	private GameObject OriginalObject{
		get{
			if (originalObject == null) {
				originalObject = FindInactiveObjectWithTag (DefaultTag);
			}
			return originalObject;
		}
		set { originalObject = value; }
	}

	private void SetObjectActive(bool state){
		if (OriginalObject != null) {
			OriginalObject.SetActive (state);
		}
	}

	//Since FindByTag only works for active objects, this function iterates through all
	//Objects in the scene and compares the tags
	private List<GameObject> FindInactiveObjectsWithTag(string tag){
		List<GameObject> found = new List<GameObject> ();
		Transform[] sceneObjects = Resources.FindObjectsOfTypeAll<Transform> ();
		foreach (Transform t in sceneObjects) {
			if (t.tag == tag && t.gameObject.scene.name != null) {
				found.Add (t.gameObject);
			}
		}
		return found;
	}

	private GameObject FindInactiveObjectWithTag(string tag){
		List<GameObject> found = FindInactiveObjectsWithTag (tag);
		if (found.Count > 0) {
			return found [0];
		}
		return null;
	}

	private string AbsolutePath {
		get{ return IsChunk ? GlobalPaths.AbsoluteChunkPath : GlobalPaths.AbsoluteHallwayPath; }
	}

	private string RelativePath{
		get{ return IsChunk ? GlobalPaths.RelativeChunkPath : GlobalPaths.RelativeHallwayPath; }
	}

	private string NewName {
		get{ return IsChunk ? GlobalPaths.NewChunkName : GlobalPaths.NewHallwayName; }
	}

	private string WorkingTypeName {
		get{ return IsChunk ? "Chunk" : "Hallway"; }
	}

	private bool IsChunk{
		get { return workingType == WorkingType.CHUNK;}
	}

	private string InstanceTag{
		get{ return IsChunk ? "ChunkCopy" : "HallwayInstance"; }
	}

	private string DefaultTag{
		get{ return IsChunk ? "Chunk" : "HallwayTemplate"; }
	}

	private List<GameObject> SceneObjects{
		get{ 
			List<GameObject> chunks = FindInactiveObjectsWithTag ("Chunk");
			List<GameObject> hallways = FindInactiveObjectsWithTag ("HallwayTemplate");
			chunks.AddRange (hallways);
			return chunks;
		}
	}

	private List<GameObject> SceneInstances{
		get{ 
			List<GameObject> chunks = FindInactiveObjectsWithTag ("ChunkCopy");
			List<GameObject> hallways = FindInactiveObjectsWithTag ("HallwayInstance");
			chunks.AddRange (hallways);
			return chunks;
		}
	}

	private void DestroyMultiple(List<GameObject> objects){
		objects.ForEach (o => DestroyImmediate (o, true));
	}

	private GameObject FindInstantiatable(GameObject source){
		if (IsChunk) {
			return source;
		} else {
			foreach (Transform t in source.transform) {
				if (t.tag == "HallwayPrototype") {
					return t.gameObject;
				}
			}
			return null;
		}
	}

	private WorkingType TypeInScene{
		get{
			if (FindInactiveObjectWithTag ("HallwayTemplate") != null) {
				return WorkingType.HALLWAY;
			} else  {
				return WorkingType.CHUNK;
			}
		}
	}
}
