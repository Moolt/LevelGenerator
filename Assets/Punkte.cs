using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Punkte : MonoBehaviour {

	void OnDrawGizmos(){
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(PolToCar(new Vector3(1f, 1/4f, 0f)), .3f);
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(PolToCar(new Vector3(1f, 3/4f, 0f)), .3f);
		Gizmos.color = Color.blue;
		Gizmos.DrawSphere(PolToCar(new Vector3(1f, 5/4f, 0f)), .3f);
		Gizmos.color = Color.black;
		Gizmos.DrawSphere(PolToCar(new Vector3(1f, 7/4f, 0f)), .3f);
	}

	private Vector3 PolToCar(Vector3 vec){
		Vector3 cartesian = Vector3.zero;
		float cos = Mathf.Cos (vec.y * Mathf.PI);
		float sin = Mathf.Sin (vec.y * Mathf.PI);
		cartesian.x = vec.x * cos;
		cartesian.z = vec.x * sin;
		return cartesian;
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
