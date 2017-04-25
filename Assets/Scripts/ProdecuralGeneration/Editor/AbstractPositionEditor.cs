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
			if (aPosition.useRaycast) {				
				aPosition.useAvailableSpace = EditorGUILayout.Toggle ("Fill", aPosition.useAvailableSpace);
			}

			if (!aPosition.useAvailableSpace || !aPosition.useRaycast) {
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
		Vector3 pos = aPosition.transform.position;
		float sizeFactor = HandleUtility.GetHandleSize (pos) * 0.7f;
		if (EditorGUIExtension.DirectionHandle (pos, Vector3.right, sizeFactor, Color.red)) {
			aPosition.direction = Vector3.right;
		} else if (EditorGUIExtension.DirectionHandle (pos, Vector3.up, sizeFactor, Color.green)) {
			aPosition.direction = Vector3.up;
		} else if (EditorGUIExtension.DirectionHandle (pos, Vector3.forward, sizeFactor, Color.blue)) {
			aPosition.direction = Vector3.forward;
		}
	}
}
