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
		Handles.color = room.hallwayMeta.IsCriticalPath ? Color.red : Color.cyan;
		Handles.DrawLine (room.hallwayMeta.Start, room.hallwayMeta.End);
	}
}
