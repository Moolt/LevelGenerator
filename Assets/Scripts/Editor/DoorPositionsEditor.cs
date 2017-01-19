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
				ClampPosition(door, doorPositions.AbstractBounds);
				Handles.color = (door == selectedDoor) ? Color.red : Color.blue;
				if (Handles.Button (door.Position, Quaternion.LookRotation(door.Direction), door.Extends.x, door.Extends.x, Handles.RectangleHandleCap)) {
					selectedDoor = door;
				}
			}

			if (selectedDoor != null) {
				Vector3 horHandleDir = Vector3.Cross (Vector3.up, selectedDoor.Direction);
				selectedDoor.Position = Handles.Slider (selectedDoor.Position, horHandleDir, selectedDoor.Extends.x * 1.3f, Handles.ArrowCap, 1f);
				selectedDoor.Position = Handles.Slider (selectedDoor.Position, Vector3.up, selectedDoor.Extends.y * 1.3f, Handles.ArrowCap, 1f);

				//Clamp coordinates before calculating the offset
				ClampPosition(selectedDoor, doorPositions.AbstractBounds);

				//Calculate Offset
				selectedDoor.Offset = selectedDoor.Position - doorPositions.AbstractBounds.Corners[selectedDoor.CornerIndex];
				Vector3 roomCenter = doorPositions.AbstractBounds.Center;
				float sizeFactor = HandleUtility.GetHandleSize (roomCenter);

				Vector3 newDirection = EditorGUIExtension.DirectionHandleVec (roomCenter, sizeFactor * 1.3f, selectedDoor.Direction, new Vector3(1f, 0f, 1f));
				if (newDirection != selectedDoor.Direction) {
					selectedDoor.Direction = newDirection;
					selectedDoor.CornerIndex = doorPositions.AbstractBounds.CornerIndicesByDirection (newDirection) [1];
					selectedDoor.Position = doorPositions.AbstractBounds.Corners [selectedDoor.CornerIndex];
				}

				int[] indices = doorPositions.AbstractBounds.CornerIndicesByDirection (selectedDoor.Direction);

				foreach (int i in indices) {
					if (Handles.Button (doorPositions.AbstractBounds.Corners [i], Quaternion.identity, .5f, .5f, Handles.DotHandleCap)) {
						selectedDoor.Position = doorPositions.AbstractBounds.Corners [i];
						selectedDoor.CornerIndex = i;
					}
				}
			}
		}
	}		

	//Hinders the door to be placed outside of the room
	private void ClampPosition(DoorDefinition door, AbstractBounds bounds){
		int[] cornerIndices = bounds.CornerIndicesByDirection (door.Direction);
		if (cornerIndices.Length > 0) {
			//Min and Max Points of the wall the door is facing
			//As to the order of the corners in AbstractBounds, these are not always the actual min and max values
			//The exceptions are handles by the clamp function below, which calculates min and max if they are unknown
			Vector3 roomBottomLeft = bounds.Corners [cornerIndices [0]];
			Vector3 roomTopRight = bounds.Corners [cornerIndices [cornerIndices.Length - 1]];

			//Either (1,1,0) or (0,1,1). Y Axis is always the same since we always want to clamp on the Y-Axis
			Vector3 clampFilter = VectorAbs (Vector3.Cross (door.Direction, Vector3.up) + Vector3.up);
			//Clamp on all axis. Depending on the direction the door is facing, one axis' value is going to be discarded using the clampFilter
			Vector3 clampedPos;
			clampedPos.x = Clamp (door.Position.x, roomBottomLeft.x, roomTopRight.x, door.Extends.x);
			clampedPos.y = Clamp (door.Position.y, roomBottomLeft.y, roomTopRight.y, door.Extends.y);
			clampedPos.z = Clamp (door.Position.z, roomBottomLeft.z, roomTopRight.z, door.Extends.z);
			door.Position = Vector3.Scale (clampedPos, clampFilter) + Vector3.Scale (door.Position, VectorAbs (door.Direction));
		}
	}

	//Clamp function that calculated min and max. Border is used to include the doors size into the calculation
	private float Clamp(float val, float lim1, float lim2, float border){
		float min = Mathf.Min (lim1, lim2) + border;
		float max = Mathf.Max (lim1, lim2) - border;
		return Mathf.Clamp (val, min, max);
	}

	//Makes all values of a vector positive
	private Vector3 VectorAbs(Vector3 vec){
		return new Vector3(Mathf.Abs(vec.x), Mathf.Abs(vec.y), Mathf.Abs(vec.z));
	}
}
