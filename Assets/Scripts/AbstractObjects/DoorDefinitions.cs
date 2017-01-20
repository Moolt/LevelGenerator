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
	public bool previewDoors = true;

	public override void Preview(){
		UpdateDoors ();
	}

	public override void Generate(){
		UpdateDoors ();
		randomDoors.Clear ();
		randomDoors.AddRange (doors);
	}

	//Updates positions with offset, clamps values
	private void UpdateDoors(){
		foreach (DoorDefinition door in doors) {
			door.Position = AbstractBounds.Corners [door.CornerIndex] + door.Offset;
			ClampPosition (door);
			door.RelPosition = door.Position - transform.position;
		}
	}

	public List<DoorDefinition> RandomDoors{
		get{
			if (Application.isEditor && SceneUpdater.IsActive) {
				return previewDoors ? doors : new List<DoorDefinition> (0);
			}
			return randomDoors;
		}
	}

	//Hinders the door to be placed outside of the room
	public void ClampPosition(DoorDefinition door){
		int[] cornerIndices = AbstractBounds.CornerIndicesByDirection (door.Direction);
		if (cornerIndices.Length > 0) {
			//Min and Max Points of the wall the door is facing
			//As to the order of the corners in AbstractBounds, these are not always the actual min and max values
			//The exceptions are handles by the clamp function below, which calculates min and max if they are unknown
			Vector3 roomBottomLeft = AbstractBounds.Corners [cornerIndices [0]];
			Vector3 roomTopRight = AbstractBounds.Corners [cornerIndices [cornerIndices.Length - 1]];

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
