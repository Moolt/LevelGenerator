using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DoorDefinition{
	public Vector3 Size = Vector3.one * 4f;
	public Vector3 Position = Vector3.zero;
	public Vector3 Direction = Vector3.forward;
	public int CornerIndex = 0;
	public Vector3 Offset = Vector3.zero;
}

[RequireComponent (typeof(RoomMeshGeneration), typeof(MeshFilter), typeof(MeshRenderer))]
public class DoorDefinitions : TransformingProperty {
	public List<DoorDefinition> doors = new List<DoorDefinition>(0);

	public override void Preview(){
		foreach (DoorDefinition door in doors) {
			door.Position = AbstractBounds.Corners [door.CornerIndex] + door.Offset;
		}
	}

	public override void Generate(){
	
	}
}
