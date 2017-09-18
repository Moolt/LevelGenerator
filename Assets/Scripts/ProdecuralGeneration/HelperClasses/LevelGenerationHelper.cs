using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerationHelper : MonoBehaviour {

	public GameObject[] prefabs;
	public int seed = 0;
	public bool randomSeed = false;
	public string presetName;

	// Use this for initialization
	void Start () {
		foreach (GameObject go in prefabs) {
			Instantiate (go);
		}
		LevelGenerator levelGenerator = new LevelGenerator ();
		int _seed = randomSeed ? Random.Range (0, 10000) : seed;
		levelGenerator.GenerateLevel (presetName, _seed);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
