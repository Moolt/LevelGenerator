using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor (typeof(ObjectDocking))]
public class ObjectDockingEditor : Editor {
	private ObjectDocking child;

	void OnEnable(){
		child = target as ObjectDocking;
	}

	public override void OnInspectorGUI(){
		if (SceneUpdater.IsActive) {
			child.UpdateOffset ();
			child.offsetType = (OffsetType)EditorGUILayout.EnumPopup ("Interpolation method", child.offsetType);
			child.offset = EditorGUILayout.Vector3Field ("Offset", child.offset);
			//SceneUpdater.UpdateScene ();
		}
	}

	public void OnSceneGUI(){
		if (SceneUpdater.IsActive) {
			ObjectDocking child = target as ObjectDocking;
			child.UpdateOffset ();

			if (child.ParentsAbstractBounds == null) {
				return;
			}

			Vector3[] corners = child.ParentsAbstractBounds.Corners;

			//Drawing the Grid
			Handles.color = Color.white;
			for (int j = 0; j < 3; j++) {
				for (int k = 0; k < 3; k++) {
					Handles.DrawDottedLine (corners [k * 3 + j * 9], corners [k * 3 + 2 + j * 9], 2f);
					Handles.DrawDottedLine (corners [k + j * 9], corners [6 + k + j * 9], 2f);
					Handles.DrawDottedLine (corners [k + j * 3], corners [k + 18 + j * 3], 2f);
				}
			}

			//Drawing the Handles
			//Also handling button klicks
			//When one handle is klicked, the corner index of the object docking component is set
			for (int i = 0; i < corners.Length; i++) {
				Handles.color = (i == child.SelectedCornerIndex) ? Color.red : Color.blue;
				float handleSize = HandleUtility.GetHandleSize (corners [i]) * 0.1f;

				if (Handles.Button (corners [i], Quaternion.identity, handleSize, handleSize, Handles.DotHandleCap)) {
					//Logic for moving etc. is inside the property itself
					child.SelectedCornerIndex = i;
				}
			}
		}
	}
}
