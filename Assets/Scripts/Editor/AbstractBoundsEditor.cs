using System.Collections;
using UnityEngine;
using UnityEditor;


[CustomEditor (typeof(AbstractBounds))]
public class AbstractBoundsEditor : Editor {
	private AbstractBounds abstractBounds;
	[SerializeField]
	private bool showStretch;

	void OnEnable(){
		abstractBounds = target as AbstractBounds;
	}

	public override void OnInspectorGUI(){
		if (SceneUpdater.IsActive) {
			abstractBounds.GizmoPreviewState = (GizmoPreviewState)EditorGUILayout.EnumPopup ("Gizmo visibility", abstractBounds.GizmoPreviewState);
			EditorGUILayout.Space ();

			//ITransformable[] children = abstractBounds.gameObject.GetComponents<ITransformable> ();
			abstractBounds.hasFixedSize = EditorGUILayout.Toggle("Fixed size", abstractBounds.hasFixedSize);

			EditorGUILayout.Space ();
			if (!abstractBounds.hasFixedSize) {
				ConditionalVectorField ("Minimal", ref abstractBounds.minSize);
				ConditionalVectorField ("Maximal", ref abstractBounds.maxSize);
			} else {
				ConditionalVectorField ("Size", ref abstractBounds.minSize);
			}

			EditorGUILayout.Space ();

			//Both options are only relevant, when the size is variable and therefore defined by min and max values
			if (!abstractBounds.hasFixedSize) {
				abstractBounds.lerp = EditorGUILayout.Slider ("Preview lerp", abstractBounds.lerp, 0f, 1f);

				abstractBounds.keepAspectRatio = EditorGUILayout.Toggle ("Keep aspect ratio", abstractBounds.keepAspectRatio);
			}

			EditorGUILayout.Space ();
			if (!abstractBounds.IsChunk) {
				showStretch = EditorGUILayout.Foldout (showStretch, "Stretch");
				EditorGUILayout.Space ();

				if (showStretch) {
					for (int i = 0; i < 3; i++) {
						StretchInfo stretch = abstractBounds.StretchInfos [i];
						EditorGUILayout.BeginHorizontal ();
						EditorGUILayout.PrefixLabel ("Stretch on");
						EditorGUILayout.SelectableLabel (stretch.Label, GUILayout.Width (20));
						stretch.Active = EditorGUILayout.Toggle (stretch.Active, GUILayout.ExpandWidth (false));
						EditorGUILayout.SelectableLabel ("Percent", GUILayout.Width (60));
						stretch.Percent = EditorGUILayout.FloatField (stretch.Percent, GUILayout.ExpandWidth (true));
						stretch.Percent = Mathf.Clamp (stretch.Percent, 0f, 1f);
						EditorGUILayout.EndHorizontal ();
					}
				}
			}

			//Update bounds of all objects implementing the interface
			//ApplySize (children, abstractBounds);
			SceneUpdater.UpdateScene ();
		}
	}
		
	/*private void ApplySize(ITransformable[] variableObjects, AbstractBounds variableBounds){
		Vector3 lerpedSize = Vector3.Lerp (variableBounds.minSize, variableBounds.maxSize, variableBounds.lerp);
		variableBounds.Size = lerpedSize;
		variableBounds.UpdateVariableBoundsDependencies (variableObjects);
	}*/

	private void ConditionalVectorField(string label, ref Vector3 value){
		float prevLabelWidth = EditorGUIUtility.labelWidth;
		Vector3 lockedAxis = abstractBounds.LockedAxes;
		EditorGUILayout.BeginHorizontal ();
		EditorGUILayout.PrefixLabel (label);
		Vector3 tmpSize = value;
		EditorGUIUtility.labelWidth = 15;
		GUI.enabled = lockedAxis.x != 1f;
		tmpSize.x = EditorGUILayout.FloatField ("X", tmpSize.x);
		GUI.enabled = lockedAxis.y != 1f;
		tmpSize.y = EditorGUILayout.FloatField ("Y", tmpSize.y);
		GUI.enabled = lockedAxis.z != 1f;
		tmpSize.z = EditorGUILayout.FloatField ("Z", tmpSize.z);
		GUI.enabled = true;
		EditorGUIUtility.labelWidth = prevLabelWidth;
		value = tmpSize;
		EditorGUILayout.EndHorizontal ();
	}
}
