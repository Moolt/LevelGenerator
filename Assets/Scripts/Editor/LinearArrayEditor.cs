using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof(LinearArray))]
public class LinearArrayEditor : Editor {
	private LinearArray lArray;

	void OnEnable(){
		lArray = target as LinearArray;
	}

	public override void OnInspectorGUI(){
		if (SceneUpdater.IsActive) {
			lArray.GizmoPreviewState = (GizmoPreviewState)EditorGUILayout.EnumPopup ("Gizmo visibility", lArray.GizmoPreviewState);
			EditorGUILayout.Space ();

			lArray.autoCount = EditorGUILayout.Toggle ("Autocount", lArray.autoCount);
			lArray.closeGap = !EditorGUILayout.Toggle ("Auto spacing", !lArray.closeGap);

			if (lArray.autoCount && !lArray.closeGap) {
				lArray.spacing = EditorGUILayout.FloatField ("Spacing", lArray.spacing);
				lArray.spacing = Mathf.Max (lArray.spacing, 0f);
			} else if(!lArray.autoCount) {
				lArray.duplicateCount = EditorGUILayout.IntField ("Copies", lArray.duplicateCount);
				lArray.duplicateCount = Mathf.Max (lArray.duplicateCount, 0);
			}

		}
		SceneUpdater.UpdateScene ();
	}

	public void OnSceneGUI(){
		Vector3 pos = lArray.transform.position;
		float sizeFactor = HandleUtility.GetHandleSize (pos) * 0.7f;

		if (EditorGUIExtension.DirectionHandle (pos, Vector3.right, sizeFactor, Color.red)) {
			lArray.orientation = Vector3.right;
		} else if (EditorGUIExtension.DirectionHandle (pos, Vector3.up, sizeFactor, Color.green)) {
			lArray.orientation = Vector3.up;
		} else if (EditorGUIExtension.DirectionHandle (pos, Vector3.forward, sizeFactor, Color.blue)) {
			lArray.orientation = Vector3.forward;
		}
	}
}
