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
		abstractMesh.extends = EditorGUILayout.Vector3Field ("Extends", abstractMesh.extends);
		abstractMesh.material = EditorGUILayout.ObjectField (abstractMesh.material, typeof(Material)) as Material;
		abstractMesh.tiling = EditorGUILayout.FloatField ("Tiling", abstractMesh.tiling);

		if (abstractMesh.meshShape == MeshShape.CYLINDER) {
			abstractMesh.iterations = (int)EditorGUILayout.Slider ("Iterations", abstractMesh.iterations, 3, 50);
		}

		SceneUpdater.UpdateScene ();
	}

}
