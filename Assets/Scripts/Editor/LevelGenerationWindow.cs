using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System;

public class ChunkInstantiator : ScriptableObject{

	//There may  still be dependencies to transforming objects. Therefore they are destroyed last.
	private ICollection<AbstractProperty> delayedRemovalCollection;
	private Stack<GameObject> workStack;

	public ChunkInstantiator(){
		this.delayedRemovalCollection = new List<AbstractProperty> ();
		this.workStack = new Stack<GameObject> ();
	}

	//1. Depth first search, traversing through the Object tree
	//2. Obtaining list from each GameObject with components implementing the AbstractProperty class
	//3. Sort the list depending on their priorities (Instantiating, Transforming, MeshGen)
	//4. Execute the components

	public void InstiantiateChunk(GameObject chunk){
		workStack.Push (chunk);
		chunk.tag = "Untagged";

		//Traversing, depth first
		while(workStack.Count > 0){
			GameObject currentObj = workStack.Pop ();
			ExecuteAbstractProperties (currentObj);
			PushChildrenToStack (currentObj);
		};

		CleanUp ();
	}

	private void PushChildrenToStack(GameObject parent){		
		Stack<Transform> children = new Stack<Transform>();

		foreach (Transform t in parent.transform) {
			if (t.gameObject.activeSelf) {
				children.Push (t);
			}
		}

		while (children.Count > 0) {
			workStack.Push (children.Pop().gameObject);
		}
	}

	//Components implementing AbstractProperty are sorted by priority and then executed
	private void ExecuteAbstractProperties(GameObject obj){
		ICollection<AbstractProperty> properties = obj.GetComponents<AbstractProperty> ();

		if (properties.Count > 0) {
			properties = SortAbstractProperties (properties);
		}

		foreach (AbstractProperty property in properties) {
			if(!property.IsDirty){
				property.Generate ();
				HandleGeneratedObjects (property); //Add generated objs to work stack, if there are any
				HandlePropertyRemoval (property); //Remove component after execution
			}
		}
	}

	//Arrays of the type InstantiatingProperty may generate Objects during generation time
	//They have to be added to the working stack in case they inherit abstract properties
	private void HandleGeneratedObjects(AbstractProperty property){		
		if (property.GeneratedObjects != null && property.GeneratedObjects.Count > 0) {
			foreach (GameObject genObj in property.GeneratedObjects) {
				workStack.Push (genObj);
			}
		}
	}

	//For purposes of cleaning up all abstract properties need to be removed during or after the creation process
	//As there may be dependencies, the removal of several properties can be delayed until the end of the generation process
	private void HandlePropertyRemoval(AbstractProperty property){
		if (property.DelayRemoval) {
			property.IsDirty = true; //Set dirty to avoid another execution
			delayedRemovalCollection.Add (property);			
		} else {
			DestroyImmediate (property);
		}
	}

	//Sorts the Properties regarding to priority
	private ICollection<AbstractProperty> SortAbstractProperties(ICollection<AbstractProperty> properties){
		return properties.OrderBy (obj => obj.ExecutionOrder).ToList();
	}

	//Remove all components with delayed removal
	private void CleanUp(){
		foreach (AbstractProperty property in delayedRemovalCollection) {
			DestroyImmediate (property);
		}
	}
}

public class LevelGenerationWindow : EditorWindow {

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

	[MenuItem("Window/Level Generation")]
	public static void ShowWindow(){
		EditorWindow.GetWindow (typeof(LevelGenerationWindow));
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

		ChunkInstantiator generator = ScriptableObject.CreateInstance<ChunkInstantiator> ();
		generator.InstiantiateChunk (MostRecentCopy);
		MostRecentCopy.tag = "ChunkCopy";
		durationMillis = DateTime.Now.Millisecond - startMillis;
	}

	//Remvoes any old copy of the chunk and sets the original, abstract chunk active
	private void Restore(){
		DestroyOldCopy (); //Destroy the generated chunk
		OriginalChunk.SetActive(true);
		SceneUpdater.SetActive (true);
	}

	//If there is an instantiated copy of the chunk in the scene, remove it
	private void DestroyOldCopy(){
		if (MostRecentCopy != null) {
			DestroyImmediate (MostRecentCopy);
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
		DestroyImmediate (OriginalChunk);
		OriginalChunk = (GameObject)GameObject.Instantiate(Resources.Load("EmptyChunk"));
		SceneUpdater.UpdateScene ();
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

			DestroyImmediate (OriginalChunk);
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
