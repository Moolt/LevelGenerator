using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DoorDefinition{
	public Vector3 Size;
	public Vector3 Position;
	public Vector3 RelPosition;
	public Vector3 Direction;
	public int CornerIndex;
	public Vector3 Offset;

	public Vector3 Extends{
		get{
			return Size * 0.5f;
		}
	}
}

[RequireComponent (typeof(MeshFilter), typeof(MeshRenderer))]
public class DoorDefinitions : DoorProperty {
	private List<DoorDefinition> randomDoors = new List<DoorDefinition> (0);
	public List<DoorDefinition> doors = new List<DoorDefinition>(0);
	public OffsetType offsetType = OffsetType.ABSOLUTE;
	public float doorSize;

	public override void Preview(){
		foreach (DoorDefinition door in doors) {
			door.Position = AbstractBounds.Corners [door.CornerIndex] + door.Offset;
			door.RelPosition = door.Position - transform.position;
		}
	}

	public override void Generate(){
		randomDoors.Clear ();
		randomDoors.AddRange (doors);
	}

	public List<DoorDefinition> RandomDoors{
		get{ return randomDoors; }
	}
}
