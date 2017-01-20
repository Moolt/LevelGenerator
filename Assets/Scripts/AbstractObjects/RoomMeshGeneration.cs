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

	//MeshInfo
	private Vector3[] vertices;

	public Vector3[] Vertices{
		get{
			if (dirty) {
				vertices = new Vector3[] {
					new Vector3 (-width, height, length),
					new Vector3 (width, height, length),
					new Vector3 (width, 0f, length),
					new Vector3 (-width, 0f, length),
					new Vector3 (width, height, -length),
					new Vector3 (-width, height, -length),
					new Vector3 (-width, 0f, -length),
					new Vector3 (width, 0f, -length)
				};
				dirty = false;
			}
			return vertices;
		}
	}

	public List<Vector3> NewVertices {
		get;
		set;
	}

	public List<int> NewTriangles {
		get;
		set;
	}

	private int[][] tris = new int[][] {
		new int[] { 0, 1, 2, 3 },
		new int[] { 1, 4, 7, 2 },
		new int[] { 4, 5, 6, 7 },
		new int[] { 5, 0, 3, 6 },
		new int[] { 3, 2, 7, 6 },
		new int[] { 1, 0, 5, 4 }
	};

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

	public int[][] Triangles{
		get{
			return tris;
		}
	}

	public Vector3[] FaceVertices(int direction){
		Vector3[] faceVertices = new Vector3[4];

		for (int i = 0; i < faceVertices.Length; i++) {
			faceVertices [i] = Vertices [Triangles [direction] [i]];
		}
		return faceVertices;
	}

	public void ConstructRoom(){
		NewVertices = new List<Vector3> ();
		NewTriangles = new List<int> ();

		PrepareCoordinateGrid (Vector3.forward);
		PrepareCoordinateGrid (Vector3.back);
		PrepareCoordinateGrid (Vector3.right);
		PrepareCoordinateGrid (Vector3.left);
	}

	public void PrepareCoordinateGrid(Vector3 direction){
		if (doors != null) {
			Rect[] doorRects = GetDoorsAsRect (direction);
			Rect wallRect = GetWallRect (direction);

			BuildCoordinateGrid (doorRects, wallRect);
			DoorGridElement[] gridElements = PopulateGrid (doorRects);
			BuildWallMesh(gridElements, direction);
			//Vector3 recreatedOrigin = Vec2ToVec3 (_2DOrigin, bottomLeftOrigin, direction);
			//Vector3 recreatedSize = Vec2ToVec3 (_2DSize, Extends * 2f, direction);
		}
	}

	private void BuildWallMesh(DoorGridElement[] gridElements, Vector3 direction){
		Vector3 backVec = abstractBounds.FindCorner (0, direction);
		foreach (DoorGridElement gridElement in gridElements) {
			if (!gridElement.IsInsideDoor) {
				Rect elementRect = gridElement.Rect;
				Vector3 x1y1 = Vec2ToVec3 (elementRect.position, backVec, direction);
				Vector3 x1y2 = Vec2ToVec3 (new Vector2 (elementRect.position.x, elementRect.position.y + elementRect.height), backVec, direction);
				Vector3 x2y2 = Vec2ToVec3 (elementRect.position + elementRect.size, backVec, direction);
				Vector3 x2y1 = Vec2ToVec3 (new Vector2 (elementRect.position.x + elementRect.width, elementRect.position.y), backVec, direction);
				NewVertices.AddRange (new Vector3[]{ x1y1, x1y2, x2y2, x2y1 });
				int length = NewVertices.Count;
				NewTriangles.AddRange (new int[]{ length - 4, length - 3, length - 1 });
				NewTriangles.AddRange (new int[]{ length - 1, length - 3, length - 2 });
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
			if (doorRect.Overlaps (gridElementRect)) {
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

		mesh.vertices = meshData.NewVertices.ToArray ();
		mesh.triangles = meshData.NewTriangles.ToArray ();

		/*vertices = new List<Vector3> ();
		triangles = new List<int> ();

		for (int i = 0; i < 6; i++) {
			MakeFace (i);
		}

		mesh.vertices = vertices.ToArray ();
		mesh.triangles = triangles.ToArray ();*/
		mesh.RecalculateNormals ();
	}

	void MakeFace(int direction){
		vertices.AddRange (meshData.FaceVertices(direction));

		triangles.Add (vertices.Count - 4);
		triangles.Add (vertices.Count - 4 + 1);
		triangles.Add (vertices.Count - 4 + 2);
		triangles.Add (vertices.Count - 4);
		triangles.Add (vertices.Count - 4 + 2);
		triangles.Add (vertices.Count - 4 + 3);
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
			doors.AddRange (doorDefinitions.doors);
			meshData.Doors = doors;
		}
	}
}
