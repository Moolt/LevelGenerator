using System.Collections;
using UnityEngine;
using UnityEditor;


[CustomEditor (typeof(AbstractBounds))]
public class AbstractBoundsEditor : Editor {

	public override void OnInspectorGUI(){
		AbstractBounds varBounds = (AbstractBounds)target;
		ITransformable[] children = varBounds.gameObject.GetComponents<ITransformable> ();

		if (DrawDefaultInspector ()) {
			//Max size values cant be smaller that min size
			
			ClampValues (varBounds);

			//Update bounds of all objects implementing the interface
			ApplySize (children, varBounds);
		}

		if (GUILayout.Button ("Randomize Size")) {
			varBounds.RandomizeSize (children);
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
		variableBounds.Bounds = lerpedSize;
		variableBounds.UpdateVariableBoundsDependencies (variableObjects);
	}
}
