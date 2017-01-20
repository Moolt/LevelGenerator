using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorGridElement{
	public Rect Rect { get; set; }
	public bool IsInsideDoor { get; set; }
}

public class RoomMeshData{
	private AbstractBounds abstractBounds;
	private List<DoorDefinition> doors;
	private List<float> xCoordinates = new List<float> ();
	private List<float> yCoordinates = new List<float> ();

	//Dirty flag for Vertice Calculation
	private bool dirty = true;

	public RoomMeshData(AbstractBounds abstractBounds){
		this.abstractBounds = abstractBounds;
	}

	//Extends
	private float width = 1f;
	private float height = 1f;
	private float length = 1f;

	public List<Vector3> Vertices {
		get;
		set;
	}

	public List<int> Triangles {
		get;
		set;
	}

	public Vector3 Extends{
		set{
			width = value.x;
			height = value.y * 2f;
			length = value.z;
			dirty = true;
		}
		get{
			return new Vector3 (width, height, length);
		}
	}

	public Vector3 Size{
		get{
			return new Vector3 (width * 2f, height, length * 2f);
		}
	}

	public void ConstructRoom(){
		Vertices = new List<Vector3> ();
		Triangles = new List<int> ();

		CalculateWallMesh (Vector3.forward);
		CalculateWallMesh (Vector3.back);
		CalculateWallMesh (Vector3.right);
		CalculateWallMesh (Vector3.left);
		BuildPlaneMesh (0, 6, 8, 2);
		BuildPlaneMesh (20, 26, 24, 18);
	}

	public void CalculateWallMesh(Vector3 direction){
		if (doors != null) {
			Rect[] doorRects = GetDoorsAsRect (direction);
			Rect wallRect = GetWallRect (direction);

			BuildCoordinateGrid (doorRects, wallRect);
			DoorGridElement[] gridElements = PopulateGrid (doorRects);
			BuildWallMesh(gridElements, direction);
		}
	}

	private void BuildPlaneMesh(params int[] cornerIndices){
		Vertices.AddRange (abstractBounds.RelativeCorners (cornerIndices[0], cornerIndices[1], cornerIndices[2], cornerIndices[3]));
		int length = Vertices.Count;
		Triangles.AddRange (new int[]{ length - 4, length - 3, length - 1 });
		Triangles.AddRange (new int[]{ length - 1, length - 3, length - 2 });
	}

	private void BuildWallMesh(DoorGridElement[] gridElements, Vector3 direction){
		//Back Vector is used to transform a 2DVec back to a 3DVec
		//It doesn't matter which point on the plane / wall it actually is, as long as it stores the 
		//X or Y coordinate that is missing from the 2DVec. Subtract position to get coordinate in object space
		Vector3 backVec = abstractBounds.FindCorner (0, direction) - abstractBounds.transform.position;
		foreach (DoorGridElement gridElement in gridElements) {
			if (!gridElement.IsInsideDoor) {
				Rect elementRect = gridElement.Rect;
				Vector3 x1y1 = Vec2ToVec3 (elementRect.position, backVec, direction);
				Vector3 x1y2 = Vec2ToVec3 (new Vector2 (elementRect.position.x, elementRect.position.y + elementRect.height), backVec, direction);
				Vector3 x2y2 = Vec2ToVec3 (elementRect.position + elementRect.size, backVec, direction);
				Vector3 x2y1 = Vec2ToVec3 (new Vector2 (elementRect.position.x + elementRect.width, elementRect.position.y), backVec, direction);
				Vertices.AddRange (new Vector3[]{ x1y1, x1y2, x2y2, x2y1 });
				int length = Vertices.Count;

				//Walls should face inward. Triangle order therefore depends on the walls direction
				if (direction == Vector3.forward || direction == Vector3.left) {
					Triangles.AddRange (new int[]{ length - 4, length - 3, length - 1 });
					Triangles.AddRange (new int[]{ length - 1, length - 3, length - 2 });
				} else {
					Triangles.AddRange (new int[]{ length - 1, length - 2, length - 4 });
					Triangles.AddRange (new int[]{ length - 4, length - 2, length - 3 });
				}
			}
		}
	}

	private DoorGridElement[] PopulateGrid(Rect[] doorRects){
		List<DoorGridElement> gridElements = new List<DoorGridElement> ();
		//Used to merge grid cells if they have no door between then
		//Has to be true for the first iteration to avoid out of index problems
		//Is reset for every row, could be expanded for both axis
		bool lastElementWasDoor = true;

		for (int y = 0; y < yCoordinates.Count - 1; y++) {
			for (int x = 0; x < xCoordinates.Count - 1; x++) {
				//Build the grid cell
				DoorGridElement gridElement = new DoorGridElement ();
				float width = Mathf.Max (xCoordinates [x], xCoordinates [x + 1]) - Mathf.Min (xCoordinates [x], xCoordinates [x + 1]);
				float height = Mathf.Max (yCoordinates [y], yCoordinates [y + 1]) - Mathf.Min (yCoordinates [y], yCoordinates [y + 1]);
				gridElement.Rect = new Rect (xCoordinates [x], yCoordinates [y], width, height);
				gridElement.IsInsideDoor = HasIntersectionWithDoor (doorRects, gridElement.Rect);

				//Only adds the new element if the last element was inside a door and therefore theres nothing to merge
				//Or of the new grid cell itself is inside a door
				if (lastElementWasDoor || gridElement.IsInsideDoor) {
					gridElements.Add (gridElement);
				} else {
					//Expand the width of the last grid cell by the width of the new one, preventing unneeded vertices
					Rect oldRect = gridElements [gridElements.Count - 1].Rect;
					Rect extendedRect = new Rect (oldRect.x, oldRect.y, oldRect.width + gridElement.Rect.width, oldRect.height);
					gridElements [gridElements.Count - 1].Rect = extendedRect;
				}

				lastElementWasDoor = gridElement.IsInsideDoor;
			}
			lastElementWasDoor = true;
		}
		return gridElements.ToArray ();
	}

	private bool HasIntersectionWithDoor(Rect[] doorRects, Rect gridElementRect){
		foreach (Rect doorRect in doorRects) {			
			if (doorRect.Contains (gridElementRect.center)) {
				return true;
			}
		}
		return false;
	}

	private void BuildCoordinateGrid(Rect[] doorRects, Rect wallRect){
		xCoordinates.Clear ();
		yCoordinates.Clear ();

		foreach (Rect doorRect in doorRects) {
			AddRectCoords (doorRect);
		}

		AddRectCoords (wallRect);
		xCoordinates.Sort ();
		yCoordinates.Sort ();
	}

	private void AddRectCoords(Rect rect){
		AddCoord (xCoordinates, rect.xMin);
		AddCoord (xCoordinates, rect.xMax);
		AddCoord (yCoordinates, rect.yMin);
		AddCoord (yCoordinates, rect.yMax);
	}

	private void AddCoord(List<float> coords, float val){
		if (!coords.Contains (val)) {
			coords.Add (val);
		}
	}

	private Rect GetWallRect(Vector3 direction){
		Vector3 origin = new Vector3 (-Extends.x, 0f, -Extends.z);
		//Vector3 bottomLeftOrigin = abstractBounds.FindCorner (0, direction);
		Vector2 _2DOrigin = Vec3ToVec2 (origin, direction);
		Vector2 _2DSize = Vec3ToVec2 (Size, direction);
		return new Rect (_2DOrigin, _2DSize);
	}

	private Rect[] GetDoorsAsRect(Vector3 direction){
		List<Rect> doorRects = new List<Rect> ();
		foreach (DoorDefinition door in doors) {
			if (door.Direction == direction) {
				Vector2 pos2D = Vec3ToVec2 (door.RelPosition, direction);
				Vector2 size2D = Vec3ToVec2 (door.Size, direction);
				pos2D -= size2D * .5f;
				doorRects.Add (new Rect (pos2D, size2D));
			}
		}
		return doorRects.ToArray();
	}

	//Assuming that the direction is never along the y axis
	private Vector2 Vec3ToVec2(Vector3 input, Vector3 direction){
		Vector3 normal = Vector3.Cross (direction, Vector3.up);
		Vector2 output = Vector2.zero;
		output.y = input.y;
		//The axis which the direction points to should be eliminated by the scale with the normal
		//The remaining value is the x value
		output.x += input.x * Mathf.Abs(normal.x);
		output.x += input.z * Mathf.Abs(normal.z);
		return output;
	}

	private Vector3 Vec2ToVec3(Vector3 input, Vector3 original, Vector3 direction){
		Vector3 normal = Vector3.Cross (direction, Vector3.up);
		normal = Vector3.Scale (normal, normal);
		Vector3 output = Vector3.zero;
		output.y = input.y;
		//Static axis
		output += Vector3.Scale (original, Vector3.Scale(direction, direction));
		//Map x axis to its original axis
		output += Vector3.Scale (Vector3.one * input.x, normal);
		return output;
	}

	public List<DoorDefinition> Doors {
		get {
			return this.doors;
		}
		set {
			doors = value;
		}
	}
}

[RequireComponent (typeof(MeshFilter), typeof(MeshRenderer))]
[RequireComponent (typeof(AbstractBounds), typeof(DoorDefinitions))]
public class RoomMeshGeneration : MeshProperty {
	private List<DoorDefinition> doors = new List<DoorDefinition>(0);
	private AbstractBounds abstractBounds;
	private Vector3 roomExtends;

	private RoomMeshData meshData;
	private List<Vector3> vertices;
	private List<int> triangles;

	private MeshFilter meshFilter;
	private Mesh mesh;

	void Init(){
		abstractBounds = GetComponent<AbstractBounds> ();
		meshFilter = GetComponent<MeshFilter> ();
		meshData = new RoomMeshData (abstractBounds);
		mesh = meshFilter.sharedMesh;
		roomExtends = abstractBounds.Extends;
	}

	public override void Preview(){
		Init ();
		ObtainDoors ();
		UpdateMesh ();
		UpdateMeshCollider ();
	}

	private void UpdateMesh(){
		mesh.Clear ();

		meshData.Extends = roomExtends;
		meshData.ConstructRoom ();

		mesh.vertices = meshData.Vertices.ToArray ();
		mesh.triangles = meshData.Triangles.ToArray ();

		mesh.RecalculateNormals ();
	}

	public override void Generate(){
		Init ();
		ObtainDoors ();
		UpdateMesh ();
		UpdateMeshCollider ();
	}

	private void UpdateMeshCollider(){
		MeshCollider meshCollider = GetComponent<MeshCollider> () as MeshCollider;
		if (meshCollider != null) {
			meshCollider.sharedMesh = meshFilter.sharedMesh;
		}
	}
	
	private void ObtainDoors(){
		DoorDefinitions doorDefinitions = GetComponent<DoorDefinitions> () as DoorDefinitions;
		if (doorDefinitions != null) {
			doors.Clear ();
			doors.AddRange (doorDefinitions.RandomDoors);
			meshData.Doors = doors;
		}
	}
}
