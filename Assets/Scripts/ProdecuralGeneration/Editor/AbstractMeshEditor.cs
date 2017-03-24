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

		abstractMesh.externBounds = (AbstractBounds)EditorGUILayout.ObjectField ("Abstract Bounds", abstractMesh.externBounds, typeof(AbstractBounds), true) as AbstractBounds;

		if (abstractMesh.meshShape == MeshShape.CYLINDER) {
			abstractMesh.iterations = (int)EditorGUILayout.Slider ("Iterations", abstractMesh.iterations, 3, 20);
		}

		if (abstractMesh.meshShape == MeshShape.TERRAIN) {
			abstractMesh.terrainScale = EditorGUILayout.Slider ("Scale", abstractMesh.terrainScale, 0.1f, 10);
			abstractMesh.terrainCellSize = EditorGUILayout.Slider ("Cell Size", abstractMesh.terrainCellSize, 0.1f, 10);
			abstractMesh.terrainElevation = EditorGUILayout.Slider ("Elevation", abstractMesh.terrainElevation, 0.1f, 10);
			abstractMesh.zeroEdges = EditorGUILayout.Toggle ("Zero Edges", abstractMesh.zeroEdges);
		}

		EditorGUILayout.Space ();

		abstractMesh.material = EditorGUILayout.ObjectField ("Material", abstractMesh.material, typeof(Material), false) as Material;
		abstractMesh.tiling = EditorGUILayout.FloatField ("Tiling", abstractMesh.tiling);

		EditorGUILayout.Space ();

		if (GUILayout.Button ("Break Shared Mesh")) {
			abstractMesh.BreakSharedMeshLink ();
		}
			
		SceneUpdater.UpdateScene ();
	}
}
