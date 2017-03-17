using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System.IO;
using System;

public class ChunkManagerWindow : EditorWindow {

	private GameObject originalChunk;
	private GameObject mostRecentCopy;
	private int seed;
	private int prevSeed;
	private int durationMillis = 0;

	[MenuItem("Window/Chunk Manager")]
	public static void ShowWindow(){
		EditorWindow.GetWindow (typeof(ChunkManagerWindow));
	}
	// Use this for initialization
	void OnGUI(){

		EditorGUILayout.Space();

		EditorGUILayout.BeginHorizontal ();

		if (GUILayout.Button ("New", EditorStyles.miniButtonLeft)) {
			CreateNewChunk ();
		}

		GUI.enabled = OriginalChunk != null;
		if (GUILayout.Button ("Save", EditorStyles.miniButtonMid)) {
			SaveChunk ();
		}
		GUI.enabled = true;

		if (GUILayout.Button ("Load", EditorStyles.miniButtonRight)) {
			LoadChunk ();
		}
			

		EditorGUILayout.EndHorizontal ();

		if (OriginalChunk == null) {	
			EditorGUILayout.HelpBox ("No Chunk found in the scene. Chunks have to be tagged with \"Chunk\" and there should only be one instance at a time", MessageType.Warning);
		}

		string chunkName = OriginalChunk == null ? "" : OriginalChunk.name;

		EditorGUILayout.LabelField ("Chunk name", chunkName);
		EditorGUILayout.LabelField ("Path", GlobalPaths.AbsoluteChunkPath);

		GUILayout.Space (20);

		EditorGUILayout.Space ();

		EditorGUILayout.BeginHorizontal ();
		GUI.enabled = OriginalChunk != null;
		if (GUILayout.Button ("Preview Chunk", EditorStyles.miniButtonLeft)) {
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
		DestroyOldCopy (); //Remove old generated chunk
		SetChunkActive (true); //Has to be active or else the copy could be inactive, too
		MostRecentCopy = (GameObject)GameObject.Instantiate (OriginalChunk, OriginalChunk.transform.position , Quaternion.identity);
		SetChunkActive (false);

		ChunkInstantiator generator = ChunkInstantiator.Instance;
		generator.ProcessType = ProcessType.INEDITOR;
		generator.InstantiateChunk (MostRecentCopy);
		MostRecentCopy.tag = "ChunkCopy";
		durationMillis = DateTime.Now.Millisecond - startMillis;
	}

	//Remvoes any old copy of the chunk and sets the original, abstract chunk active
	private void Restore(){
		DestroyOldCopy (); //Destroy the generated chunk
		SetChunkActive(true);
		SceneUpdater.SetActive (true);
		SceneUpdater.UpdateScene ();
	}

	//If there is an instantiated copy of the chunk in the scene, remove it
	private void DestroyOldCopy(){
		if (MostRecentCopy != null) {
			DestroyImmediate (MostRecentCopy, true);
			MostRecentCopy = null;
		}
	}

	//Shows a Dialog, asking whether the current chunk should be saved first
	//Used by "New" and "Load", since the remove the current chunk
	private void ShowSaveFirstDialog(){
		bool dialogResult = EditorUtility.DisplayDialog ("Save progress", "Loading a new Chunk into the scene requires the old one to be removed first. " +
			"Do you want to safe your progress before loading?", "Save", "Don't save");

		if (dialogResult) {
			SaveChunk ();
		}
	}

	private void CreateNewChunk(){
		Restore ();
		ShowSaveFirstDialog ();
		DestroyImmediate (OriginalChunk, true);
		UnityEngine.Object newChunkPrefab = Resources.Load (GlobalPaths.RelativeChunkPath + "/" + GlobalPaths.NewChunkName);
		if (newChunkPrefab != null) {
			OriginalChunk = (GameObject)GameObject.Instantiate (newChunkPrefab);
			SceneUpdater.UpdateScene ();
		} else {
			Debug.LogError ("Default Chunk could not be loaded. Your Resources folder must contain a default Chunk named \"NewChunk\"");
		}
	}

	private void SaveChunk(){
		Restore ();
		if (OriginalChunk != null) {
			System.IO.Directory.CreateDirectory (GlobalPaths.AbsoluteChunkPath); //Create folders if they don't yet exist
			string dialogPath = EditorUtility.SaveFilePanelInProject ("Save Chunk", OriginalChunk.name, "prefab", "", GlobalPaths.AbsoluteChunkPath);

			if (dialogPath.Length > 0) {
				Debug.Log (dialogPath);
				PrefabUtility.CreatePrefab (dialogPath, OriginalChunk, ReplacePrefabOptions.ConnectToPrefab);
			}
		} else {
			EditorUtility.DisplayDialog("No chunk found", "There is no GameObject with the tag \"Chunk\" in your scene or it is set inactive", "OK");
		}
	}

	private void LoadChunk(){
		Restore ();
		ShowSaveFirstDialog ();
		System.IO.Directory.CreateDirectory (GlobalPaths.AbsoluteChunkPath); //Create folders if they don't yet exist
		string dialogPath = EditorUtility.OpenFilePanel("Load Chunk", GlobalPaths.AbsoluteChunkPath, "prefab");
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
			DestroyImmediate (OriginalChunk, true);
			OriginalChunk = null;
			MostRecentCopy = null;
			//OriginalChunk = (GameObject)GameObject.Instantiate (Resources.Load (filePath));
			OriginalChunk = (GameObject) PrefabUtility.InstantiatePrefab (Resources.Load (filePath));
			OriginalChunk.transform.position = Vector3.zero;
			//PrefabUtility.DisconnectPrefabInstance(OriginalChunk);
			SceneUpdater.UpdateScene ();
		}
	}

	//Getter will search for the GO in the scene by tag if the reference is null
	private GameObject MostRecentCopy{
		get{
			if (mostRecentCopy == null) {
				mostRecentCopy = FindInactiveWithTag ("ChunkCopy");
			}
			return mostRecentCopy;
		}
		set{ mostRecentCopy = value; }
	}

	private GameObject OriginalChunk{
		get{
			if (originalChunk == null) {
				originalChunk = FindInactiveWithTag ("Chunk");
			}
			return originalChunk;
		}
		set { originalChunk = value; }
	}

	private void SetChunkActive(bool state){
		if (OriginalChunk != null) {
			OriginalChunk.SetActive (state);
		}
	}

	//Since FindByTag only works for active objects, this function iterates through all
	//Objects in the scene and compares the tags
	private GameObject FindInactiveWithTag(string tag){
		Transform[] sceneObjects = Resources.FindObjectsOfTypeAll<Transform> ();
		foreach (Transform t in sceneObjects) {
			if (t.tag == tag && t.gameObject.scene.name != null) {
				return t.gameObject;
			}
		}
		return null;
	}
}
