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

	[MenuItem("Window/Level Generation")]
	public static void ShowWindow(){
		EditorWindow.GetWindow (typeof(LevelGenerationWindow));
	}
	// Use this for initialization
	void OnGUI(){

		if (originalChunk == null) {
			originalChunk = GameObject.FindWithTag ("Chunk");
		}

		GUILayout.Space (20);

		if (GUILayout.Button ("Preview Chunk")) {
			SceneUpdater.SetActive (false);
			DestroyImmediate (mostRecentChunk); //Remove old generated chunk
			originalChunk.SetActive (true); //Has to be active or else the copy could be inactive, too
			mostRecentChunk = (GameObject)GameObject.Instantiate (originalChunk, originalChunk.transform.position , Quaternion.identity);
			originalChunk.SetActive (false);

			ChunkInstantiator generator = ScriptableObject.CreateInstance<ChunkInstantiator> ();
			generator.InstiantiateChunk (mostRecentChunk);
		}

		if (GUILayout.Button ("Restore")) {
			DestroyImmediate (mostRecentChunk); //Destroy the generated chunk
			originalChunk.SetActive(true);
			SceneUpdater.SetActive (true);
		}
	}
}
