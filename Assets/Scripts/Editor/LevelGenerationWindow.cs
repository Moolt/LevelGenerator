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
		foreach (Transform t in parent.transform) {
			if (t.gameObject.activeSelf) {
				workStack.Push (t.gameObject);
			}
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
	private GameObject mostRecentChunk;
	private int seed;
	private int prevSeed;
	private string path = @"Assets/Resources/Chunks";

	[MenuItem("Window/Level Generation")]
	public static void ShowWindow(){
		EditorWindow.GetWindow (typeof(LevelGenerationWindow));
	}
	// Use this for initialization
	void OnGUI(){

		if (originalChunk == null) {
			originalChunk = GameObject.FindWithTag ("Chunk");
		}

		EditorGUILayout.LabelField ("Chunk name", "");
		EditorGUILayout.LabelField ("Path", path);

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

		GUILayout.Space (20);

		seed = EditorGUILayout.IntField ("Seed", seed);

		//Update Scene when seed changes
		if (seed != prevSeed) {
			InstantiateChunk (seed);
		}

		EditorGUILayout.Space ();

		EditorGUILayout.BeginHorizontal ();
		if (GUILayout.Button ("Preview Chunk")) {
			InstantiateChunk (DateTime.Now.Millisecond);
		}

		if (GUILayout.Button ("Restore")) {
			Restore ();
		}
		EditorGUILayout.EndHorizontal ();

		prevSeed = seed;
	}

	private void InstantiateChunk(int _seed){
		UnityEngine.Random.InitState (_seed);

		SceneUpdater.SetActive (false);
		DestroyOldCopy (); //Remove old generated chunk
		originalChunk.SetActive (true); //Has to be active or else the copy could be inactive, too
		mostRecentChunk = (GameObject)GameObject.Instantiate (originalChunk, originalChunk.transform.position , Quaternion.identity);
		mostRecentChunk.tag = "ChunkCopy";
		originalChunk.SetActive (false);

		ChunkInstantiator generator = ScriptableObject.CreateInstance<ChunkInstantiator> ();
		generator.InstiantiateChunk (mostRecentChunk);
	}

	private void Restore(){
		DestroyOldCopy (); //Destroy the generated chunk
		originalChunk.SetActive(true);
		SceneUpdater.SetActive (true);
	}

	private void DestroyOldCopy(){
		if (mostRecentChunk == null) {
			mostRecentChunk = GameObject.FindGameObjectWithTag ("ChunkCopy");
		}
		if (mostRecentChunk != null) {
			DestroyImmediate (mostRecentChunk);
		}
	}

	private void CreateNewChunk(){		
		GameObject.Instantiate(Resources.Load("EmptyChunk"));
	}

	private void SaveChunk(){
		Restore ();
		GameObject chunk = GameObject.FindGameObjectWithTag ("Chunk");
		if (chunk != null) {
			System.IO.Directory.CreateDirectory (path); //Create folders if they don't yet exist
			string dialogPath = EditorUtility.SaveFilePanelInProject ("Save Chunk", chunk.name, "prefab", "", path);

			/*Debug.Log (dialogPath);
			if (!dialogPath.StartsWith (path)) {
				EditorUtility.DisplayDialog("Wrong filepath", "Please note, that the algorithm will only search the \"Chunks\" folder for the generation process.", "OK");
			}*/

			PrefabUtility.CreatePrefab (dialogPath, chunk, ReplacePrefabOptions.ReplaceNameBased);

		} else {
			EditorUtility.DisplayDialog("No chunk found", "There is no GameObject with the tag \"Chunk\" in your scene or it is set inactive", "OK");
		}
	}

	private void LoadChunk(){
		Restore ();

		System.IO.Directory.CreateDirectory (path); //Create folders if they don't yet exist
		string dialogPath = EditorUtility.OpenFilePanel("Load Chunk", path, "prefab");

		/*Debug.Log (dialogPath);
		if (!dialogPath.StartsWith (path)) {
			EditorUtility.DisplayDialog("Wrong filepath", "Please note, that the algorithm will only search the \"Chunks\" folder for the generation process.", "OK");
		}*/

		GameObject.Instantiate (Resources.Load (dialogPath));

	}
}
