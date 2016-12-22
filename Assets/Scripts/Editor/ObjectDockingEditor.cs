using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof(ObjectDocking))]
public class ObjectDockingEditor : Editor {

	public override void OnInspectorGUI(){
		ObjectDocking child = (ObjectDocking)target;

		child.CalcOffset ();	
		if (DrawDefaultInspector ()) {
		}
	}
}
