using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DebugRoomID : MonoBehaviour {
	public int ID;
	public HallwayMeta hallwayMeta;
	public bool isCritical = false;

	void OnDrawGizmos(){
		Gizmos.color = isCritical ? Color.red : Color.green;
		Gizmos.DrawSphere (transform.position, 2f);
		if (hallwayMeta != null) {
			//Gizmos.DrawLine (hallwayMeta.StartDoor.Position, hallwayMeta.EndDoor.Position);
		}
	}
}
