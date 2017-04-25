using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEngine;
using System.Linq;
using System;

[System.Serializable]
public struct WildcardChance{
	public GameObject Asset;
	public int Chance;
}
[System.Serializable]
public class WildcardPreviewData{
	public GameObject Asset{ get; set; }
	public Mesh[] Meshes { get; set; }
	public Transform[] Transforms{ get; set; }
	public Vector3[] Scale{get; set; }
}

public enum WildcardTarget{ PREFABS, CHILDREN }

[Serializable]
public class ChildKeyValue {
	public string name;
	public int chance;

	public ChildKeyValue (string name, int chance){
		this.name = name;
		this.chance = chance;
	}
}

[DisallowMultipleComponent]
public class WildcardAsset : InstantiatingProperty {
	public WildcardTarget wildcardTarget;
	[HideInInspector]
	public List<WildcardChance> chancesList = new List<WildcardChance>(0);
	[HideInInspector]
	public int selectedIndex = 0;
	[SerializeField]
	public List<ChildKeyValue> children = new List<ChildKeyValue> ();

	public override void Preview(){
		//Nothing to be done in preview
	}

	public override void Generate(){
		if (wildcardTarget == WildcardTarget.PREFABS) {
			GeneratePrefabs ();
		} else {
			SelectChild ();
		}
	}

	private void SelectChild(){
		List<int> chances = children.Select (c => c.chance).ToList ();
		GameObject chosenAsset = ChooseRandomAsset (chances, WildcardTarget.CHILDREN);

		foreach (Transform _child in transform) {
			if (_child.gameObject != null && _child.gameObject.name != chosenAsset.name) {
				RemoveChild (_child);
			}
		}
	}

	private void RemoveChild(Transform _child){
		ChunkInstantiator.RegisterForRemoval (_child.gameObject);
		List<AbstractProperty> properties = _child.GetComponents<AbstractProperty> ().ToList ();
		properties.ForEach (p => p.HasBeenDeleted = true);
	}

	private void GeneratePrefabs(){
		List<int> chances = chancesList.Select (c => c.Chance).ToList();
		GameObject chosenAsset = ChooseRandomAsset (chances, WildcardTarget.PREFABS);
		if (chosenAsset != null) {
			Component[] remainingProperties = gameObject.GetComponents<AbstractProperty> ();
			GameObject instance = InstantiatePrefab (chosenAsset);

			foreach (Component go in remainingProperties) {

				if (go is WildcardAsset) {
					continue;
				}

				Component newComponent = instance.AddComponent (go.GetType ());
				if (newComponent != null) {
					newComponent.GetCopyOf (go);
				}

				AbstractProperty property = go as AbstractProperty;
				if (property != null) {
					property.HasBeenDeleted = true;
				}
			}
			RegisterNewProperties (instance);
			RestoreOriginalAttributes (instance, chosenAsset);
			ChunkInstantiator.RegisterForRemoval (gameObject);
		}
	}

	//If this gameObject contained any other abstract properties, they were copied to the
	//Newly instantiated one. Register these properties for them to get interpreted by the instantiator.
	private void RegisterNewProperties(GameObject copy){
		ChunkInstantiator instantiator = ChunkInstantiator.Instance;
		instantiator.PushToWorkStack (copy);
	}

	//When copying the components, the attributes seem to get lost for some reason
	//The attributes in question are restored here
	private void RestoreOriginalAttributes(GameObject instance, GameObject prefab){
		instance.tag = prefab.gameObject.tag;
		instance.name = prefab.gameObject.name;
		instance.layer = prefab.gameObject.layer;
		instance.transform.position = prefab.transform.position + transform.position;
	}

	private GameObject InstantiatePrefab(GameObject prefab){
		GameObject instance = (GameObject)Instantiate (prefab);
		instance.transform.SetParent (transform.parent, true);
		instance.transform.position = prefab.transform.position + transform.position;
		instance.transform.rotation = transform.rotation * prefab.transform.rotation;
		return instance;
	}

	//Assign the scale and rotation instead of copying / adding the other transform
	//Keep the position of the wildcard
	private void AssignTransform(Transform otherTransform){
		//Adding up rotation
		transform.eulerAngles = otherTransform.rotation.eulerAngles + transform.rotation.eulerAngles;
		//Adding up scale. Since default scale is 1, the scale should also stay 1 if both objects have the default scale
		transform.localScale = otherTransform.localScale + transform.localScale - Vector3.one;
	}

	//Choses a random GameObject and returns it
	//Chances are considered
	private GameObject ChooseRandomAsset(List<int> chances, WildcardTarget target){
		float[] rangeTable = GenerateRangeTable (chances);
		float randomFloat = UnityEngine.Random.value;

		for (int i = 0; i < rangeTable.Length; i++) {
			if (randomFloat > rangeTable [i] && randomFloat < rangeTable [i + 1]) {
				if (target == WildcardTarget.PREFABS) {
					return chancesList [i].Asset;
				} else {
					return GameObject.Find (children.ElementAt(i).name);
				}
			}
		}
		return null;
	}

	//Creates a table with ranges from 0f to 1f which represent the chances given by each asset
	//The table is later used to randomly choose an asset
	private float[] GenerateRangeTable(List<int> chances){
		float[] rangeTable = new float[chances.Count + 1];
		float sum = 0f;

		rangeTable [0] = 0f;

		for (int i = 0; i < chances.Count - 1; i++) {
			sum += chances [i] / 100f;
			rangeTable [i + 1] = sum;
		}

		rangeTable [rangeTable.Length - 1] = 1f;

		return rangeTable;
	}		

	//Sums up the total amount of chances from all wildcards
	//Must sum up to 100
	//Used by the Editor script for the error notification if sum != 100
	public int SumUpChances(){
		int sum = 0;
		foreach (WildcardChance wc in chancesList) {
			sum += wc.Chance;
		}
		return sum;
	}

	public override void DrawEditorGizmos(){
		if (chancesList.Count > 0 && chancesList[selectedIndex].Asset != null) {
			
			WildcardPreviewData previewData = new WildcardPreviewData ();
			previewData.Asset = chancesList [selectedIndex].Asset;
			MeshFilter[] meshFilters = previewData.Asset.GetComponentsInChildren<MeshFilter> ();
			previewData.Meshes = meshFilters.Select(mf => mf.sharedMesh).ToArray();
			previewData.Transforms = meshFilters.Select (mf => mf.gameObject.transform).ToArray();
			previewData.Scale = previewData.Transforms.ToList ().Select (t => FindAbsoluteScale (t)).ToArray();

			for (int i = 0; i < meshFilters.Length; i++) {
				if (previewData.Meshes[i] != null) {

					Gizmos.color = Color.cyan;
					Gizmos.DrawMesh (previewData.Meshes[i], previewData.Transforms[i].position + transform.position, 
						previewData.Transforms[i].rotation,
						previewData.Scale[i]);
				}
			}
		}
	}

	public void UpdateChildren(){
		List<ChildKeyValue> newChildren = new List<ChildKeyValue> ();

		//Fill with all children
		foreach (Transform child in transform) {
			newChildren.Add (new ChildKeyValue (child.name, 0));
		}
			
		//Add new children to the main dict
		foreach (ChildKeyValue _child in newChildren) {

			bool childAlreadyExists = children.Any (c => c.name == _child.name);

			if (childAlreadyExists) {
				ChildKeyValue existing = children.Single (c => c.name == _child.name);
				_child.chance = existing.chance;
			}
		}
		children = newChildren;
	}

	//Debug function printing out the actual percantages when generating the assets
	//Choose an accuracy of 1000 for good enough results
	private void TestRandomFunctionality(int accuracy){
		List<int> chances = chancesList.Select (c => c.Chance).ToList ();
		Dictionary<string, int> results = new Dictionary<string,int> ();
		string generatedObj;

		for (int i = 0; i < accuracy; i++) {
			generatedObj = ChooseRandomAsset (chances, WildcardTarget.PREFABS).name;
			if (results.ContainsKey (generatedObj)) {
				results [generatedObj] += 1;
			} else {
				results.Add (generatedObj, 1);
			}
		}			

		float percent = 100f / accuracy;

		foreach (KeyValuePair<string, int> pair in results) {
			Debug.Log (pair.Key + ": " + pair.Value * percent + "%");
		}
	}

	public MeshFilter[] IndexedPreviewMeshes{
		get{
			if (chancesList.Count > 0) {
				return chancesList [selectedIndex].Asset.GetComponentsInChildren<MeshFilter> ();
			} else{
				return new MeshFilter[]{ };
			}
		}
	}
}
