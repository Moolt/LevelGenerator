using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System;

public class ChunkManagerWindow : EditorWindow {

	private GameObject originalChunk;
	private GameObject mostRecentCopy;
	private int seed;
	private int prevSeed;
	private string chunkFolderName = "Chunks";
	private string path;
	private int durationMillis = 0;

	void OnEnable(){
		path = @"Assets/Resources/" + chunkFolderName;
	}

	[MenuItem("Window/Chunk Manager")]
	public static void ShowWindow(){
		EditorWindow.GetWindow (typeof(ChunkManagerWindow));
	}
	// Use this for initialization
	void OnGUI(){

		if (OriginalChunk == null) {
			OriginalChunk = GameObject.FindWithTag ("Chunk");
		}

		EditorGUILayout.Space();

		EditorGUILayout.BeginHorizontal ();

		if (GUILayout.Button ("New")) {
			CreateNewChunk ();
		}

		if (GUILayout.Button ("Save")) {
			SaveChunk ();
		}

		if (GUILayout.Button ("Load")) {
			LoadChunk ();
		}

		EditorGUILayout.EndHorizontal ();

		EditorGUILayout.LabelField ("Chunk name", originalChunk.name);
		EditorGUILayout.LabelField ("Path", path);

		GUILayout.Space (20);

		EditorGUILayout.Space ();

		EditorGUILayout.BeginHorizontal ();
		if (GUILayout.Button ("Preview Chunk")) {
			InstantiateChunk (DateTime.Now.Millisecond);
		}

		if (GUILayout.Button ("Restore")) {
			Restore ();
		}
		EditorGUILayout.EndHorizontal ();

		seed = EditorGUILayout.IntField ("Seed", seed);
		EditorGUILayout.LabelField ("Generation time", durationMillis.ToString() + " ms");

		//Update Scene when seed changes
		if (seed != prevSeed) {
			InstantiateChunk (seed);
		}

		prevSeed = seed;
	}

	private void InstantiateChunk(int _seed){
		int startMillis = DateTime.Now.Millisecond;
		UnityEngine.Random.InitState (_seed);

		SceneUpdater.SetActive (false);
		DestroyOldCopy (); //Remove old generated chunk
		OriginalChunk.SetActive (true); //Has to be active or else the copy could be inactive, too
		MostRecentCopy = (GameObject)GameObject.Instantiate (OriginalChunk, OriginalChunk.transform.position , Quaternion.identity);
		OriginalChunk.SetActive (false);

		ChunkInstantiator generator = ChunkInstantiator.Instance;
		generator.ProcessType = ProcessType.GENERATE;
		generator.InstiantiateChunk (MostRecentCopy);
		MostRecentCopy.tag = "ChunkCopy";
		durationMillis = DateTime.Now.Millisecond - startMillis;
	}

	//Remvoes any old copy of the chunk and sets the original, abstract chunk active
	private void Restore(){
		DestroyOldCopy (); //Destroy the generated chunk
		OriginalChunk.SetActive(true);
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
		UnityEngine.Object newChunkPrefab = Resources.Load ("NewChunk");
		if (newChunkPrefab != null) {
			OriginalChunk = (GameObject)GameObject.Instantiate (newChunkPrefab);
			SceneUpdater.UpdateScene ();
		} else {
			Debug.LogError ("Default Chunk could not be loaded. Your Resources folder must contain a default Chunk named \"NewChunk\"");
		}
	}

	private void SaveChunk(){
		Restore ();
		GameObject chunk = GameObject.FindGameObjectWithTag ("Chunk");
		if (chunk != null) {
			System.IO.Directory.CreateDirectory (path); //Create folders if they don't yet exist
			string dialogPath = EditorUtility.SaveFilePanelInProject ("Save Chunk", chunk.name, "prefab", "", path);

			if (dialogPath.Length > 0) {
				PrefabUtility.CreatePrefab (dialogPath, chunk, ReplacePrefabOptions.ReplaceNameBased);
			}

		} else {
			EditorUtility.DisplayDialog("No chunk found", "There is no GameObject with the tag \"Chunk\" in your scene or it is set inactive", "OK");
		}
	}

	private void LoadChunk(){
		Restore ();
		ShowSaveFirstDialog ();
		System.IO.Directory.CreateDirectory (path); //Create folders if they don't yet exist
		string dialogPath = EditorUtility.OpenFilePanel("Load Chunk", path, "prefab");
		if (dialogPath.Length > 0) {
			//Since the dialog outputs the complete path, which Resources.Load doesn't work with, a relative path is used
			//Resources.Load only works with files inside the Resources folder. The path therefore only has to contain the folder the chunks
			//Are stored in
			string filePath = chunkFolderName + "/" + System.IO.Path.GetFileNameWithoutExtension (dialogPath);

			DestroyImmediate (OriginalChunk, true);
			OriginalChunk = (GameObject)GameObject.Instantiate (Resources.Load (filePath));
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

	//Since FindByTag only works for active objects, this function iterates through all
	//Objects in the scene and compares the tags
	private GameObject FindInactiveWithTag(string tag){
		Transform[] sceneObjects = Resources.FindObjectsOfTypeAll<Transform> ();
		foreach (Transform t in sceneObjects) {
			if (t.tag == tag) {
				return t.gameObject;
			}
		}
		return null;
	}
}
