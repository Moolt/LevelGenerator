using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor (typeof(ObjectDocking))]
public class ObjectDockingEditor : Editor {

	public override void OnInspectorGUI(){
		
		ObjectDocking child = target as ObjectDocking;

		child.UpdateOffset ();

		if (DrawDefaultInspector ()) {
		}
	}

	public void OnSceneGUI(){
		ObjectDocking child = target as ObjectDocking;
		Vector3[] corners = child.AbstractBounds.Corners;

		//Drawing the Grid
		for (int j = 0; j < 3; j++) {
			for (int k = 0; k < 3; k++) {
				Handles.DrawLine (corners [k * 3 + j * 9], corners [k * 3 + 2 + j * 9]);
				Handles.DrawLine (corners [k + j * 9], corners [6 + k + j * 9]);
				Handles.DrawLine (corners [k + j * 3], corners [k + 18 + j * 3]);
			}
		}

		//Drawing the Handles
		//Also handling button klicks
		//When one handle is klicked, the corner index of the object docking component is set
		for (int i = 0; i < corners.Length; i++) {
			Handles.color = (i == child.SelectedCornerIndex) ? Color.red : Color.blue;
			bool handleResult = false;

			if (i == 4 || i % 9 == 4) {				
				handleResult |= Handles.Button (corners [i], Quaternion.identity, 1.4f, 1.6f, Handles.CubeCap);
			} else {
				handleResult |= Handles.Button (corners [i], Quaternion.identity, 1.4f, 1.6f, Handles.SphereCap);
			}

			if (handleResult) {
				child.SelectedCornerIndex = i;
			}
		}
	}
}
