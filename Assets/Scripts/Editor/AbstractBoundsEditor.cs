using System.Collections;
using UnityEngine;
using UnityEditor;


[CustomEditor (typeof(AbstractBounds))]
public class AbstractBoundsEditor : Editor {
	private AbstractBounds abstractBounds;

	void OnEnable(){
		abstractBounds = target as AbstractBounds;
	}

	public override void OnInspectorGUI(){
		if (SceneUpdater.IsActive) {
			abstractBounds.GizmoPreviewState = (GizmoPreviewState)EditorGUILayout.EnumPopup ("Gizmo visibility", abstractBounds.GizmoPreviewState);
			EditorGUILayout.Space ();

			ITransformable[] children = abstractBounds.gameObject.GetComponents<ITransformable> ();

			if (DrawDefaultInspector ()) {
				//Max size values cant be smaller that min size
			
				ClampValues (abstractBounds);

				//Update bounds of all objects implementing the interface
				ApplySize (children, abstractBounds);
			}

			if (GUILayout.Button ("Randomize Size")) {
				abstractBounds.RandomizeSize (children);
			}
			SceneUpdater.UpdateScene ();
		}
	}

	private void ClampValues(AbstractBounds varBounds){
		if (varBounds.fixedSize) {
			varBounds.minSize = varBounds.maxSize;
		}
		ClampVectorMin (ref varBounds.minSize, Vector3.zero);
		ClampVectorMin (ref varBounds.maxSize, varBounds.minSize);
	}

	private void ClampVectorMin(ref Vector3 input, Vector3 minValue){
		input.x = Mathf.Max (input.x, minValue.x);
		input.y = Mathf.Max (input.y, minValue.y);
		input.z = Mathf.Max (input.z, minValue.z);
	}
		
	private void ApplySize(ITransformable[] variableObjects, AbstractBounds variableBounds){
		Vector3 lerpedSize = Vector3.Lerp (variableBounds.minSize, variableBounds.maxSize, variableBounds.lerp);
		variableBounds.Size = lerpedSize;
		variableBounds.UpdateVariableBoundsDependencies (variableObjects);
	}
}
