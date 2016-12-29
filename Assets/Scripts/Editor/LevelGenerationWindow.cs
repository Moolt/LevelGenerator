using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System;

public class ChunkInstantiator{

	private static Type[] executeOrder = { typeof(InstantiatingProperty), typeof(TransformingProperty), typeof(MeshGeneration) };
	private Stack<GameObject> workStack;

	public ChunkInstantiator(){
		this.workStack = new Stack<GameObject> ();
	}

	//1. Depth first search, traversing through the Object tree
	//2. Obtaining list from each GameObject with components implementing the AbstractProperty class
	//3. Sort the list depending on their priorities (Instantiating, Transforming, MeshGen)
	//4. Execute the components

	public void InstiantiateChunk(GameObject chunk){
		workStack.Push (chunk);
		while(workStack.Count > 0){
			GameObject currentObj = workStack.Pop ();
			ExecuteAbstractProperties (currentObj);
			PushChildrenToStack (currentObj);
		};
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
			property.Generate ();
		}
	}

	//Sorts the Properties regarding to priority
	private ICollection<AbstractProperty> SortAbstractProperties(ICollection<AbstractProperty> properties){
		return properties.OrderBy (obj => obj.ExecutionOrder).ToList();
	}
}

public class LevelGenerationWindow : EditorWindow {
	
	[MenuItem("Window/Level Generation")]
	public static void ShowWindow(){
		EditorWindow.GetWindow (typeof(LevelGenerationWindow));
	}
	// Use this for initialization
	void OnGUI(){		

		GUILayout.Space (20);

		if (GUILayout.Button ("Generate Chunk")) {
			GameObject chunk = GameObject.FindWithTag ("Chunk");
			Vector3 pos = new Vector3 (chunk.transform.position.x + 100, chunk.transform.position.y, chunk.transform.position.z);
			GameObject copiedChunk = (GameObject)GameObject.Instantiate (chunk, pos, Quaternion.identity);

			ChunkInstantiator generator = new ChunkInstantiator ();
			generator.InstiantiateChunk (copiedChunk);

			/*
			IAbstractAsset[] abstractAssets = chunk.GetComponents<IAbstractAsset> ();

			foreach (IAbstractAsset comp in abstractAssets) {
				comp.Generate ();
			}*/
		}
	}
}
