using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(DebugRoomID))]
public class DebugRoomIDEditor : Editor {

	void OnSceneGUI(){
		GUIStyle style = new GUIStyle ();
		style.fontSize = 25;
		DebugRoomID room = target as DebugRoomID;
		Handles.Label(room.transform.position, room.ID.ToString(), style);
		if (room.hallwayMeta == null)
			return;
		//Handles.DrawLine (room.hallwayMeta.StartDoor.Position, room.hallwayMeta.EndDoor.Position);
	}
}
