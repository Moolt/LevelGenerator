using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof(AbstractPosition))]
public class AbstractPositionEditor : Editor {
	private AbstractPosition aPosition;

	void OnEnable(){
		aPosition = target as AbstractPosition;
	}

	public override void OnInspectorGUI(){
		if (SceneUpdater.IsActive) {
			aPosition.GizmoPreviewState = (GizmoPreviewState)EditorGUILayout.EnumPopup ("Gizmo visibility", aPosition.GizmoPreviewState);
			EditorGUILayout.Space ();

			aPosition.useRaycast = EditorGUILayout.Toggle ("Raycast", aPosition.useRaycast);
			aPosition.useAvailableSpace = EditorGUILayout.Toggle ("Fill", aPosition.useAvailableSpace);

			if (!aPosition.useAvailableSpace) {
				aPosition.minValue = EditorGUILayout.FloatField ("Min", aPosition.minValue);
				aPosition.maxValue = EditorGUILayout.FloatField ("Max", aPosition.maxValue);
				//Clamping
				aPosition.minValue = Mathf.Max (0f, aPosition.minValue);
				aPosition.maxValue = Mathf.Max (0f, aPosition.maxValue);
			}
			SceneUpdater.UpdateScene ();
		}
	}

	public void OnSceneGUI(){
		float sizeFactor = HandleUtility.GetHandleSize (Vector3.zero);
		if (EditorGUIExtension.DirectionHandle (Vector3.zero, Vector3.right, sizeFactor, Color.red)) {
			aPosition.direction = Vector3.right;
		} else if (EditorGUIExtension.DirectionHandle (Vector3.zero, Vector3.up, sizeFactor, Color.green)) {
			aPosition.direction = Vector3.up;
		} else if (EditorGUIExtension.DirectionHandle (Vector3.zero, Vector3.forward, sizeFactor, Color.blue)) {
			aPosition.direction = Vector3.forward;
		}
	}
}
