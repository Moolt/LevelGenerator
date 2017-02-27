using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class HallwaySegmentFace{
	private Vector3 direction;
	private Vector3[] vertices;
	private List<int> triangles;

	public HallwaySegmentFace (Vector3 direction){
		this.direction = direction;
	}		

	public Vector3[] Vertices {
		get {
			return this.vertices;
		}
	}

	public List<int> Triangles {
		get {
			return new int[] { 0, 1, 2, 0, 2, 3 }.ToList();
		}
	}
}

public class HallwaySegment{
	private Vector2 center;
	private float doorSize = 1f;
	private float height;
	//Generated
	private Vector3[] vertices;
	private List<int> triangles;
	private Square square;
	private GridPosition gridPosition;
	private Dictionary<Vector2, GridPosition> adjacent;
	private Dictionary<Vector2, float> size;

	public HallwaySegment (Square square, GridPosition gridPosition){
		this.gridPosition = gridPosition;
		this.square = square;
		this.center = gridPosition.Position;
		this.adjacent = gridPosition.AdjacentPositions;
		height = 2f;
		InitSizeDict ();
		CalculateBounds ();
	}

	private void InitSizeDict(){
		size = new Dictionary<Vector2, float> ();
		size.Add (Vector2.up, doorSize);
		size.Add (Vector2.right, doorSize);
		size.Add (Vector2.down, doorSize);
		size.Add (Vector2.left, doorSize);
	}

	private void CalculateBounds(){
		Vector2 oppositeDir = gridPosition.Direction * -1;
		//Only modify padding in the direction of the hallway segment
		//The other directions will have their default value (doorSize)
		size[gridPosition.Direction] = GetPaddingForDirection(gridPosition.Direction, false);
		size[oppositeDir] = GetPaddingForDirection (oppositeDir, true);
	}

	private float GetPaddingForDirection(Vector2 direction, bool isOpposite){
		if (adjacent [direction] == null) {
			//The direction is pointing inside of a room.
			if (/*isOpposite &&*/ gridPosition.DoorID > -1) {
				return 0f;
			}
			//The direction is not pointing to any path
			return doorSize;
		}

		if (IsEdge (gridPosition)) {
			return doorSize;
		}

		Vector2 neighbourPosition = adjacent [direction].Position;

		if (adjacent [direction].Direction == gridPosition.Direction) {
			//Continuous path in the same direction
			if (IsEdge (adjacent [direction])) {
				return (Vector2.Distance (gridPosition.Position, neighbourPosition) * direction - direction * doorSize).magnitude;
			} else {
				return (Vector2.Distance (gridPosition.Position, neighbourPosition) * direction * .5f).magnitude;
			}
		} else {
			//Previous pos and this will form an edge
			return (Vector2.Distance (gridPosition.Position, neighbourPosition) * direction - direction * doorSize).magnitude;
		}
	}

	private bool IsEdge(GridPosition gridPosition){
		bool isEdge = gridPosition.AdjacentPositions [gridPosition.Direction] == null;
		if (IsHorizontal (gridPosition.Direction)) {
			isEdge |= gridPosition.AdjacentPositions [Vector2.up] != null || gridPosition.AdjacentPositions [Vector2.down] != null;
		} else {
			isEdge |= gridPosition.AdjacentPositions [Vector2.left] != null || gridPosition.AdjacentPositions [Vector2.right] != null;
		}
		return isEdge;
	}

	private bool IsHorizontal(Vector2 direction){
		return direction == Vector2.left || direction == Vector2.right;
	}

	private void CalculateVertices(){
		vertices = new Vector3[] {
			new Vector3 (size[Vector2.right] + center.x, height, size[Vector2.up] + center.y),
			new Vector3 (-size[Vector2.left] + center.x, height, size[Vector2.up] + center.y),
			new Vector3 (-size[Vector2.left] + center.x, 0f, size[Vector2.up] + center.y),
			new Vector3 (size[Vector2.right] + center.x, 0f, size[Vector2.up] + center.y),
			new Vector3 (-size[Vector2.left] + center.x, height, -size[Vector2.down] + center.y),
			new Vector3 (size[Vector2.right] + center.x, height, -size[Vector2.down] + center.y),
			new Vector3 (size[Vector2.right] + center.x, 0f, -size[Vector2.down] + center.y),
			new Vector3 (-size[Vector2.left] + center.x, 0f, -size[Vector2.down] + center.y)
		};
	}

	public List<int> Triangles(int index){
		if (vertices == null) {
			CalculateVertices ();
		}
		int[][] _triangles = new int[][] {
			new int[] { 0, 1, 2}, //back
			new int[] { 0, 2, 3}, //back
			new int[] { 4, 5, 6}, //front
			new int[] { 4,6,7 }, //front
			new int[] { 1, 0, 5 }, //top
			new int[] { 1, 5, 4 }, //top
			new int[] { 3, 2, 7}, //bottom
			new int[] { 3, 7, 6 }, //bottom
			new int[] { 1, 4, 7}, //left
			new int[] { 1, 7, 2 }, //left
			new int[] { 5, 0, 3}, //right
			new int[] { 5, 3, 6 } //right
		};
		triangles = new List<int> ();
		for (int i = 0; i < _triangles.GetLength (0); i++) {
			foreach(int j in _triangles[i]) {
				triangles.Add (j + index);
			}
		}
		return triangles;
	}

	public Vector3[] Vertices{
		get{
			CalculateVertices ();
			return vertices;
		}
	}

	/*public List<int> Triangles{
		get{
			CalculateTriangles ();
			return triangles;
		}
	}*/
}

public class HallwayMeshGenerator {

	private HashSet<HallwaySegment> hallwaySegments;
	private AStarGrid grid;
	private List<List<Square>> hallwayPaths;
	private List<Vector3> vertices;
	private List<int> triangles;
	private Mesh mesh;

	public HallwayMeshGenerator(AStarGrid grid){
		this.grid = grid;
		this.hallwayPaths = new List<List<Square>> ();
		this.hallwaySegments = new HashSet<HallwaySegment> ();
		this.vertices = new List<Vector3> ();
		this.triangles = new List<int> ();
	}

	public Mesh GenerateMesh(){
		mesh = new Mesh ();

		PrepareHallwaySegments ();

		int currentVertexIndex = 0;
		foreach (HallwaySegment segment in hallwaySegments) {
			vertices.AddRange (segment.Vertices);
			List<int> hwTriangles = segment.Triangles (currentVertexIndex);
			currentVertexIndex = hwTriangles.Max () + 1;
			triangles.AddRange (hwTriangles);
		}
		mesh.vertices = vertices.ToArray ();
		mesh.triangles = triangles.ToArray ();
		mesh.RecalculateNormals ();
		mesh.RecalculateBounds ();
		return mesh;
	}

	private void PrepareHallwaySegments(){
		foreach (List<Square> squares in hallwayPaths) {
			foreach (Square square in squares) {
				GridPosition gridPosition = grid.Grid [square.GridX, square.GridY];
				HallwaySegment newSegment = new HallwaySegment (square, gridPosition);
				if (!hallwaySegments.Contains (newSegment)) {
					hallwaySegments.Add (newSegment);
				}
			}
		}
	}

	//Create four vertices each square

	private List<Vector3> ComputeVertices(List<Square> squares){
		/*List<Vector3>
		foreach (Square square in squares) {
			
		}*/
		return null;
	}

	public void AddPath(List<Square> path){
		hallwayPaths.Add (path);
	}

	public Mesh Mesh{
		get{ 
			GenerateMesh ();
			return mesh; 
		}
	}
}
