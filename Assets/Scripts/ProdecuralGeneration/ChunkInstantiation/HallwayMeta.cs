using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HallwayMeta{
	private DoorDefinition startDoor;
	private DoorDefinition endDoor;

	public HallwayMeta (DoorDefinition startDoor, DoorDefinition endDoor){
		this.startDoor = startDoor;
		this.endDoor = endDoor;
	}

	public DoorDefinition StartDoor {
		get {
			return this.startDoor;
		}
		set {
			startDoor = value;
		}
	}

	public DoorDefinition EndDoor {
		get {
			return this.endDoor;
		}
		set {
			endDoor = value;
		}
	}
}