using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DoorDefinition{
	public Vector3 Size;
	public Vector3 Position;
	public Vector3 Direction;
	public int CornerIndex;
	public Vector3 Offset;

	public Vector3 Extends{
		get{
			return Size * 0.5f;
		}
	}
}

[RequireComponent (typeof(RoomMeshGeneration), typeof(MeshFilter), typeof(MeshRenderer))]
public class DoorDefinitions : TransformingProperty {
	public float doorSize;
	public List<DoorDefinition> doors = new List<DoorDefinition>(0);
	public OffsetType offsetType = OffsetType.ABSOLUTE;

	public override void Preview(){
		foreach (DoorDefinition door in doors) {
			door.Position = AbstractBounds.Corners [door.CornerIndex] + door.Offset;
		}
	}

	public override void Generate(){
	
	}
}
