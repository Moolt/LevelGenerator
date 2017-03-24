using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent (typeof(MeshFilter), typeof(MeshRenderer))]
public class DoorManager : DoorProperty {
	public static int doorIndex = 0; //Used for door IDs
	public List<DoorDefinition> doors = new List<DoorDefinition>(0);
	public OffsetType offsetType = OffsetType.ABSOLUTE;
	public bool previewDoors = true;
	public int minCount = 1;
	public int maxCount = 1;

	private List<DoorDefinition> randomDoors = new List<DoorDefinition> (0);
	//Used by ChunkInstantiatior to define how many doors are needed
	private int fixedAmount = -1;
	[SerializeField]
	private float globalYOffset = 0f;
	[SerializeField]
	private int globalYCornerHeight = 0;
	[SerializeField]
	private DoorDefinition draggingDoor = null; //Used by GUI, update preview (working pos) if it's not currently being dragged

	public override void Preview(){
		UpdateDoors ();
	}

	public override void Generate(){
		UpdateDoors ();
		if (fixedAmount == -1) {
			ChooseRandomDoors (minCount, maxCount);
		} else {
			ChooseRandomDoors (fixedAmount, fixedAmount);
		}
		fixedAmount = -1;
	}

	private void ChooseRandomDoors(int min, int max){
		randomDoors.Clear ();
		List<DoorDefinition> allDoors = new List<DoorDefinition> ();
		allDoors.AddRange (doors);
		int quantity = (int)(Mathf.Round (Random.value * (max - min)) + min);
		quantity = Mathf.Min (quantity, doors.Count);

		for (int i = 0; i < quantity; i++) {
			int index = (int)(Mathf.Round((allDoors.Count - 1) * Random.value));
			randomDoors.Add (allDoors [index]);
			allDoors.RemoveAt (index);
		}
	}

	private void ChooseRandomFixed(){
		randomDoors.Clear ();
		List<DoorDefinition> fixedLengthDoors = doors.OrderBy (d => Random.value).ToList();
		randomDoors.AddRange(fixedLengthDoors.GetRange (0, fixedAmount));
	}

	//Updates positions with offset, clamps values
	private void UpdateDoors(){
		doors.ForEach (door => UpdateDoor (door));
	}

	private void UpdateDoor(DoorDefinition door){
		door.Position = AbstractBounds.Corners [door.CornerIndex] + door.Offset;
		ClampPosition (door);
		RoundValues (door);

		door.RelPosition = door.Position - transform.position;

		if (door != draggingDoor) {
			AdjustWorkingPosition (door);
		}
		//The doorsize should never be larger thant the actual room
		//doorSize = Mathf.Clamp (doorSize, 1f, AbstractBounds.minSize.y);
	}

	public List<DoorDefinition> RandomDoors{
		get{
			if (Application.isEditor && SceneUpdater.IsActive && !ProceduralLevel.IsGenerating) {
				return previewDoors ? doors : new List<DoorDefinition> (0);
			}
			return randomDoors;
		}
	}

	private void RoundValues(DoorDefinition door){
		if (door.Direction == Vector3.right || door.Direction == Vector3.left) {
			door.Position.z = ChunkBoundsHelper.RoundTo (door.Position.z, DoorDefinition.GlobalSize);
		} else {
			door.Position.x = ChunkBoundsHelper.RoundTo (door.Position.x, DoorDefinition.GlobalSize);
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
			clampedPos.x = Clamp (door.Position.x, roomBottomLeft.x, roomTopRight.x, DoorDefinition.GlobalSize);
			clampedPos.y = Clamp (door.Position.y, roomBottomLeft.y, roomTopRight.y - 0.1f, DoorDefinition.GlobalSize / 2f);
			clampedPos.z = Clamp (door.Position.z, roomBottomLeft.z, roomTopRight.z, DoorDefinition.GlobalSize);
			door.Position = Vector3.Scale (clampedPos, clampFilter) + Vector3.Scale (door.Position, VectorAbs (door.Direction));
		}
	}

	public void UpdateYOffset(float val){
		doors.ForEach (d => d.Offset.y = val);
	}

	public void AdjustWorkingPosition(DoorDefinition doorDefinition){
		//UpdateDoor (doorDefinition);
		doorDefinition.WorkingPosition = doorDefinition.Position;
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

	public int FixedAmount {
		get {
			return this.fixedAmount;
		}
		set {
			fixedAmount = value;
		}
	}

	public Vector3 RequiredSpace{
		get{
			float doorSize = DoorDefinition.GlobalSize;
			float highestDoorPos = (doors.Count > 0) ? doors.OrderByDescending (d => d.Position.y).FirstOrDefault ().Position.y + doorSize / 2f : doorSize;
			return new Vector3 (doorSize, highestDoorPos, doorSize);
		}
	}

	public int GlobalYCornerHeight {
		get {
			return this.globalYCornerHeight;
		}
		set {
			globalYCornerHeight = value;
		}
	}

	public float GlobalYOffset {
		get {
			return this.globalYOffset;
		}
	}

	public DoorDefinition DraggingDoor {
		get {
			return this.draggingDoor;
		}
		set {
			draggingDoor = value;
		}
	}
}
