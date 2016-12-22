using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof(MeshGeneration))]
public class MeshGenerationEditor : Editor {

	public override void OnInspectorGUI(){
		MeshGeneration meshGen = (MeshGeneration)target;

		if (DrawDefaultInspector ()) {
			/*if (meshGen.autoUpdate) {
				meshGen.GenerateMesh();
			}*/
		}

		if (GUILayout.Button ("Generate")) {
			meshGen.Generate ();
		}
	}
}
