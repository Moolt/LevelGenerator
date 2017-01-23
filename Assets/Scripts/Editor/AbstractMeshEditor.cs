using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(AbstractMesh))]
public class AbstractMeshEditor : Editor{
	private AbstractMesh abstractMesh;

	void OnEnable(){
		abstractMesh = target as AbstractMesh;
	}

	public override void OnInspectorGUI(){
		abstractMesh.meshShape = (MeshShape)EditorGUILayout.EnumPopup ("Shape", abstractMesh.meshShape);
		EditorGUILayout.Space ();

		if (abstractMesh.HasAbstractBounds) {
			EditorGUILayout.HelpBox ("Extends is currently modified by AbstractBounds", MessageType.Info);
		}

		GUI.enabled = !abstractMesh.HasAbstractBounds;
		abstractMesh.extends = EditorGUILayout.Vector3Field ("Extends", abstractMesh.extends);
		GUI.enabled = true;

		if (abstractMesh.meshShape == MeshShape.CYLINDER) {
			abstractMesh.iterations = (int)EditorGUILayout.Slider ("Iterations", abstractMesh.iterations, 3, 20);
		}

		EditorGUILayout.Space ();

		abstractMesh.material = EditorGUILayout.ObjectField ("Material", abstractMesh.material, typeof(Material)) as Material;
		abstractMesh.tiling = EditorGUILayout.FloatField ("Tiling", abstractMesh.tiling);

		EditorGUILayout.Space ();

		if (GUILayout.Button ("Break Shared Mesh")) {
			abstractMesh.BreakSharedMeshLink ();
		}
			
		SceneUpdater.UpdateScene ();
	}
}
