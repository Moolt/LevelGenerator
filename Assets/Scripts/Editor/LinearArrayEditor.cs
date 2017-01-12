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
		float sizeFactor = HandleUtility.GetHandleSize (Vector3.zero);

		if (DirectionHandle (Vector3.right, sizeFactor, Color.red)) {
			lArray.arrayOrientation = Direction.XAXIS;
		} else if (DirectionHandle (Vector3.up, sizeFactor, Color.green)) {
			lArray.arrayOrientation = Direction.YAXIS;
		} else if (DirectionHandle (Vector3.forward, sizeFactor, Color.blue)) {
			lArray.arrayOrientation = Direction.ZAXIS;
		}
	}

	private bool DirectionHandle(Vector3 direction, float sizeFactor, Color color){
		Handles.color = color;
		Handles.DrawDottedLine (Vector3.zero, direction * sizeFactor, 3.5f);
		return Handles.Button (direction * sizeFactor, Quaternion.identity, sizeFactor / 5f, sizeFactor / 5f, Handles.SphereCap);
	}
}
