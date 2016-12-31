using UnityEngine;
using System.Collections;

public class ChunkEditorBehaviour : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnTransformChildrenChanged(){
		Debug.Log ("fukking worjked");
	}
}
