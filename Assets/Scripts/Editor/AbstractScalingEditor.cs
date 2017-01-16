using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof(AbstractScaling))]
public class AbstractScalingEditor : Editor {
	private AbstractScaling aScaling;

	void OnEnable(){
		aScaling = target as AbstractScaling;
	}

	public override void OnInspectorGUI(){
		if (SceneUpdater.IsActive) {
			aScaling.GizmoPreviewState = (GizmoPreviewState)EditorGUILayout.EnumPopup ("Gizmo visibility", aScaling.GizmoPreviewState);
			EditorGUILayout.Space ();

			aScaling.uniformScaling = EditorGUILayout.Toggle ("Uniform scale", aScaling.uniformScaling);
			aScaling.maxVal = EditorGUILayout.DelayedIntField ("Interval max value", aScaling.maxVal);
			aScaling.maxVal = Mathf.Max (1, aScaling.maxVal);

			EditorGUILayout.Space ();

			if (aScaling.uniformScaling) {
				LabeledMinMaxSlider (ref aScaling.uniformMinSize, ref aScaling.uniformMaxSize, aScaling.maxVal);
			} else {
				LabeledMinMaxSlider (ref aScaling.minSize.x, ref aScaling.maxSize.x, aScaling.maxVal);
				LabeledMinMaxSlider (ref aScaling.minSize.y, ref aScaling.maxSize.y, aScaling.maxVal);
				LabeledMinMaxSlider (ref aScaling.minSize.z, ref aScaling.maxSize.z, aScaling.maxVal);
			}
		}

		SceneUpdater.UpdateScene ();
	}

	private void LabeledMinMaxSlider(ref float min, ref float max, float maxVal){
		EditorGUILayout.BeginHorizontal ();
		EditorGUILayout.LabelField (min.ToString ("0.00"), GUILayout.Width (30));
		EditorGUILayout.MinMaxSlider (ref min, ref max, 0f, maxVal);
		EditorGUILayout.LabelField (max.ToString ("0.00"), GUILayout.Width (30));
		EditorGUILayout.EndHorizontal ();
	}
}
