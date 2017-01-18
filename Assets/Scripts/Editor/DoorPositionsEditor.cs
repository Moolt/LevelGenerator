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

	void OnEnable(){
		doorPositions = target as DoorDefinitions;
		serDoorPositions = new SerializedObject (doorPositions);
		serDoorList = serDoorPositions.FindProperty ("doors");
	}

	public override void OnInspectorGUI(){
		if (SceneUpdater.IsActive) {

			for (int i = 0; i < serDoorList.arraySize; i++) {
				SerializedProperty serDoor = serDoorList.GetArrayElementAtIndex (i);
				SerializedProperty doorPos = serDoor.FindPropertyRelative ("Position");

				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField (doorPos.vector3Value.ToString());

				if(GUILayout.Button ("x", GUILayout.Width(20))){
					serDoorList.DeleteArrayElementAtIndex (i);
				}
				EditorGUILayout.EndHorizontal ();
			}

			if(GUILayout.Button ("Add Door")){
				serDoorList.InsertArrayElementAtIndex (serDoorList.arraySize);
			}

			serDoorPositions.ApplyModifiedProperties ();
			SceneUpdater.UpdateScene ();
		}
	}

	public void OnSceneGUI(){
		if (SceneUpdater.IsActive) {

			foreach (DoorDefinition door in doorPositions.doors) {
				Handles.color = (door == selectedDoor) ? Color.red : Color.blue;
				if (Handles.Button (door.Position, Quaternion.LookRotation(door.Direction), door.Size.magnitude, door.Size.magnitude, Handles.RectangleHandleCap)) {
					selectedDoor = door;
				}
			}

			if (selectedDoor != null) {
				selectedDoor.Position = Handles.Slider (selectedDoor.Position, SwapVector(selectedDoor.Direction), selectedDoor.Size.x * 2f, Handles.ArrowCap, 1f);
				selectedDoor.Position = Handles.Slider (selectedDoor.Position, Vector3.up, selectedDoor.Size.y * 2f, Handles.ArrowCap, 1f);
				//selectedDoor.Position = Handles.FreeMoveHandle (selectedDoor.Position, Quaternion.identity, 3f, Vector3.one, Handles.SphereCap);
				//Calculate Offset
				selectedDoor.Offset = selectedDoor.Position - doorPositions.AbstractBounds.Corners[selectedDoor.CornerIndex];

				float sizeFactor = HandleUtility.GetHandleSize (Vector3.zero);

				Vector3 newDirection = EditorGUIExtension.DirectionHandleVec (Vector3.zero, sizeFactor * 1.3f, selectedDoor.Direction, new Vector3(1f, 0f, 1f));
				if (newDirection != selectedDoor.Direction) {
					selectedDoor.Direction = newDirection;
					selectedDoor.CornerIndex = doorPositions.AbstractBounds.CornerIndicesByDirection (newDirection) [4];
					selectedDoor.Position = doorPositions.AbstractBounds.Corners [selectedDoor.CornerIndex];
				}

				int[] indices = doorPositions.AbstractBounds.CornerIndicesByDirection (selectedDoor.Direction);

				foreach (int i in indices) {
					if (Handles.Button (doorPositions.AbstractBounds.Corners [i], Quaternion.identity, .5f, .5f, Handles.DotHandleCap)) {
						selectedDoor.Position = doorPositions.AbstractBounds.Corners [i];
						selectedDoor.CornerIndex = i;
					}
				}
				//RoomFace roomFace = doorPositions.AbstractBounds.GetFace (selectedDoor.Direction);
				//selectedDoor.Position = roomFace.To;
			}
		}
	}

	private Vector3 SwapVector(Vector3 input){
		float tmp = input.x;
		Vector3 result = new Vector3 (input.z, 0f, tmp);
		return result;
	}
}
