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
		Vector3 pos = lArray.transform.position;
		float sizeFactor = HandleUtility.GetHandleSize (pos);
		float length = 0.6f;

		if (EditorGUIExtension.DirectionHandle (pos, Vector3.right * length, sizeFactor, Color.red)) {
			lArray.arrayOrientation = Direction.XAXIS;
		} else if (EditorGUIExtension.DirectionHandle (pos, Vector3.up * length, sizeFactor, Color.green)) {
			lArray.arrayOrientation = Direction.YAXIS;
		} else if (EditorGUIExtension.DirectionHandle (pos, Vector3.forward * length, sizeFactor, Color.blue)) {
			lArray.arrayOrientation = Direction.ZAXIS;
		}
	}
}
