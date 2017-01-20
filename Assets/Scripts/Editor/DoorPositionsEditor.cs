using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(DoorDefinitions))]
public class DoorPositionsEditor : Editor {
	private DoorDefinitions doorPositions;
	private SerializedObject serDoorPositions;
	private SerializedProperty serDoorList;
	private DoorDefinition selectedDoor;
	private bool newDoorCreated = false;

	void OnEnable(){
		doorPositions = target as DoorDefinitions;
	}

	void OnDisable(){
		SceneUpdater.HideGizmos = false;
		SceneUpdater.UpdateScene ();
	}

	public override void OnInspectorGUI(){
		if (SceneUpdater.IsActive) {
			serDoorPositions = new SerializedObject (doorPositions);
			serDoorList = serDoorPositions.FindProperty ("doors");
			int selectedIndex = doorPositions.doors.IndexOf (selectedDoor);
			SerializedProperty serPreviewDoors = serDoorPositions.FindProperty ("previewDoors");

			//PREVIEW DOOR toggle
			EditorGUILayout.Space ();
			string onOff = serPreviewDoors.boolValue ? "On" : "Off";
			if (GUILayout.Button ("Mesh preview: " + onOff)) {
				serPreviewDoors.boolValue = !serPreviewDoors.boolValue;
			}

			if (!SceneUpdater.HideGizmos) {
				EditorGUILayout.Space ();
				if (GUILayout.Button ("Enter edit mode")) {
					SceneUpdater.HideGizmos = true;
				}

				EditorGUILayout.Space ();
			}

			if (SceneUpdater.HideGizmos) {
				EditorGUILayout.Space ();
				SerializedProperty serDoorSize = serDoorPositions.FindProperty ("doorSize");
				serDoorSize.floatValue = EditorGUILayout.FloatField ("Door size", serDoorSize.floatValue);
				EditorGUILayout.Space ();

				for (int i = 0; i < serDoorList.arraySize; i++) {
					SerializedProperty serDoor = serDoorList.GetArrayElementAtIndex (i);
					SerializedProperty doorPos = serDoor.FindPropertyRelative ("Position");
					SerializedProperty doorOffset = serDoor.FindPropertyRelative ("Offset");
					SerializedProperty doorSize = serDoor.FindPropertyRelative ("Size");
					doorSize.vector3Value = doorPositions.doorSize * Vector3.one;

					EditorGUILayout.BeginHorizontal ();

					string labelText = doorPos.vector3Value.ToString () + " " + doorOffset.vector3Value.ToString ();
					GUIStyle labelStyle = EditorStyles.label;
					labelStyle.normal.textColor = (selectedIndex == i) ? Color.blue : Color.black;

					if (GUILayout.Button (labelText, labelStyle)) {
						selectedDoor = doorPositions.doors [i];
					}

					labelStyle.normal.textColor = Color.black;

					if (GUILayout.Button ("x", GUILayout.Width (20))) {
						serDoorList.DeleteArrayElementAtIndex (i);
						//In case the selected door has been deleted
						if (doorPositions.doors.IndexOf (selectedDoor) == i) {
							selectedDoor = null;
						}
					}
					EditorGUILayout.EndHorizontal ();
				}

				if (GUILayout.Button ("Add Door")) {
					serDoorList.InsertArrayElementAtIndex (serDoorList.arraySize);
					InitializeNewDoor (serDoorList.GetArrayElementAtIndex (serDoorList.arraySize - 1));
				}

				if (GUILayout.Button ("Clear Selection")) {
					selectedDoor = null;
				}
				if (GUILayout.Button ("Leave edit mode")) {
					SceneUpdater.HideGizmos = false;
				}
			}

			serDoorPositions.ApplyModifiedProperties ();
			SceneUpdater.UpdateScene ();
		}
	}

	private void InitializeNewDoor(SerializedProperty doorDefinition){
		SerializedProperty doorSize = doorDefinition.FindPropertyRelative ("Size");
		SerializedProperty doorCornerIndex = doorDefinition.FindPropertyRelative ("CornerIndex");
		SerializedProperty doorPosition = doorDefinition.FindPropertyRelative ("Position");
		SerializedProperty doorOffset = doorDefinition.FindPropertyRelative ("Offset");
		SerializedProperty doorDirection = doorDefinition.FindPropertyRelative ("Direction");
		doorSize.vector3Value = Vector3.one * doorPositions.doorSize;
		doorCornerIndex.intValue = doorPositions.AbstractBounds.CornerIndicesByDirection (Vector3.forward) [1]; //Middle of the room
		doorPosition.vector3Value = doorPositions.AbstractBounds.Corners [doorCornerIndex.intValue];
		doorOffset.vector3Value = Vector3.zero;
		doorDirection.vector3Value = Vector3.forward;
		newDoorCreated = true;
	}

	public void OnSceneGUI(){
		if (SceneUpdater.IsActive) {
			Repaint ();

			if (doorPositions.doors.Count == 0) {
				selectedDoor = null;
			}

			if (newDoorCreated) {
				selectedDoor = doorPositions.doors [doorPositions.doors.Count - 1];
				newDoorCreated = false;
			}

			foreach (DoorDefinition door in doorPositions.doors) {
				Handles.color = (door == selectedDoor) ? Color.red : Color.blue;
				if (Handles.Button (door.Position, Quaternion.LookRotation(door.Direction), door.Extends.x, door.Extends.x, Handles.RectangleHandleCap)) {
					selectedDoor = door;
				}
			}

			if (selectedDoor != null) {
				Vector3 horHandleDir = Vector3.Cross (Vector3.up, selectedDoor.Direction);
				float sliderSize = HandleUtility.GetHandleSize (selectedDoor.Position);
				selectedDoor.Position = Handles.Slider (selectedDoor.Position, horHandleDir, selectedDoor.Extends.x * sliderSize, Handles.ArrowCap, 1f);
				selectedDoor.Position = Handles.Slider (selectedDoor.Position, Vector3.up, selectedDoor.Extends.y * sliderSize, Handles.ArrowCap, 1f);

				//Eventhough the position is already being clamped in the preview function of doorsPositions,
				//This clamp is important since several frames may pass before the next call of preview
				doorPositions.ClampPosition (selectedDoor);

				//Calculate Offset
				selectedDoor.Offset = selectedDoor.Position - doorPositions.AbstractBounds.Corners[selectedDoor.CornerIndex];
				Vector3 roomCenter = doorPositions.AbstractBounds.Center;
				float directionHandleSize = HandleUtility.GetHandleSize (roomCenter) * 0.8f;

				Vector3 newDirection = EditorGUIExtension.DirectionHandleVec (roomCenter, directionHandleSize, selectedDoor.Direction, new Vector3(1f, 0f, 1f));
				if (newDirection != selectedDoor.Direction) {
					selectedDoor.Direction = newDirection;
					selectedDoor.CornerIndex = doorPositions.AbstractBounds.CornerIndicesByDirection (newDirection) [1];
					selectedDoor.Position = doorPositions.AbstractBounds.Corners [selectedDoor.CornerIndex];
				}

				int[] indices = doorPositions.AbstractBounds.CornerIndicesByDirection (selectedDoor.Direction);

				foreach (int i in indices) {
					float dockHandleSize = HandleUtility.GetHandleSize (doorPositions.AbstractBounds.Corners[i]) * 0.1f;
					if (Handles.Button (doorPositions.AbstractBounds.Corners [i], Quaternion.identity, dockHandleSize, dockHandleSize, Handles.DotHandleCap)) {
						selectedDoor.Position = doorPositions.AbstractBounds.Corners [i];
						selectedDoor.CornerIndex = i;
					}
				}
			}
		}
	}
}
