using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public struct WildcardChance{
	public GameObject Asset;
	public int Chance;
}

public class WildcardPreviewData{
	public GameObject Asset{ get; set; }
	public Mesh Mesh { get; set; }
	public Transform Transform{ get; set; }
}

public class WildcardAsset : InstantiatingProperty {
	[HideInInspector]
	public List<WildcardChance> chancesList = new List<WildcardChance>(0);
	[HideInInspector]
	public int selectedIndex = 0;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public override void Preview(){
		
	}

	public override GameObject[] Generate(){
		TestRandomFunctionality (10000);
		return null;
	}

	private GameObject ChooseRandomAsset(){
		float[] rangeTable = GenerateRangeTable ();
		float randomFloat = Random.value;

		for (int i = 0; i < rangeTable.Length; i++) {
			if (randomFloat > rangeTable [i] && randomFloat < rangeTable [i + 1]) {
				return chancesList [i].Asset;
			}
		}
		return null;
	}

	private float[] GenerateRangeTable(){
		float[] rangeTable = new float[chancesList.Count + 1];
		float sum = 0f;

		rangeTable [0] = 0f;

		for (int i = 0; i < chancesList.Count - 1; i++) {
			sum += chancesList [i].Chance / 100f;
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

	void OnDrawGizmos(){
		if (chancesList.Count > 0) {
			
			WildcardPreviewData previewData = new WildcardPreviewData ();
			previewData.Asset = chancesList [selectedIndex].Asset;
			MeshFilter meshFilter = previewData.Asset.GetComponent<MeshFilter> ();
			previewData.Mesh = meshFilter.sharedMesh;
			previewData.Transform = previewData.Asset.GetComponent<Transform> ();

			if (meshFilter != null) {

				Gizmos.color = Color.cyan;
				Gizmos.DrawMesh (previewData.Mesh, transform.position, 
					previewData.Transform.rotation,
					previewData.Transform.localScale);				
			}
		}
	}

	//Debug function printing out the actual percantages when generating the assets
	//Choose an accuracy of 1000 for good enough results
	private void TestRandomFunctionality(int accuracy){
		Dictionary<string, int> results = new Dictionary<string,int> ();
		string generatedObj;

		for (int i = 0; i < accuracy; i++) {
			generatedObj = ChooseRandomAsset ().name;
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
}
