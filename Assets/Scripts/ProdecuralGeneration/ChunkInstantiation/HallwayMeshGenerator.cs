using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class HallwaySegment{
	private float doorSize = 1f;
	private Vector2 center;
	private float height;
	//Generated
	private Dictionary<Vector2, GridPosition> adjacent;
	private HallwayMeshGenerator generator;
	private Dictionary<Vector2, float> size;
	private GridPosition gridPosition;
	private List<int> triangles;
	private Vector3[] vertices;

	public HallwaySegment (GridPosition gridPosition, HallwayMeshGenerator generator){
		this.gridPosition = gridPosition;
		this.center = gridPosition.Position;
		this.adjacent = gridPosition.AdjacentPositions;
		this.generator = generator;
		height = 2f;
		size = InitSizeDict ();
		CalculateBounds ();
	}

	public HallwaySegment(Dictionary<Vector2, float> size, Vector2 center){
		height = 2f;
		this.size = size;
		this.center = center;
	}

	private Dictionary<Vector2, float> InitSizeDict(){
		Dictionary<Vector2, float> _size = new Dictionary<Vector2, float> ();
		_size.Add (Vector2.up, doorSize);
		_size.Add (Vector2.right, doorSize);
		_size.Add (Vector2.down, doorSize);
		_size.Add (Vector2.left, doorSize);
		return _size;
	}

	private void CalculateBounds(){
		Vector2 oppositeDir = gridPosition.Direction * -1;
		//Only modify padding in the direction of the hallway segment
		//The other directions will have their default value (doorSize)
		size[gridPosition.Direction] = GetPaddingForDirection(gridPosition.Direction, false);
		size[oppositeDir] = GetPaddingForDirection (oppositeDir, true);
	}

	private float GetPaddingForDirection(Vector2 direction, bool isOpposite){		
		//The direction is pointing inside of a room. Will only happen with door positions
		if (adjacent [direction] == null && gridPosition.DoorID > -1) {
			return 0f;
		}

		//If there is no object in the direction of the current segment
		//Or if theres a segment to the left / right side of the segment
		//This will result in a squared segment
		if (IsEdge (gridPosition)) {
			//This will be executed, if there are two successive edges
			//Since edges are alway the size of doorSize * dooSize, the will be a hole between
			//Two segments because both segmets won't compensate for each others width
			//This problem is solved by adding a separate segment between the edges, filling the hole
			//Note, how a different constructor is called with a pre-calculated size and the center between both edges
			if (adjacent[direction] != null && IsEdge (adjacent [direction])) {
				Vector2 fillerCenter = gridPosition.Position + Vector2.Distance (gridPosition.Position, adjacent [direction].Position) * direction * .5f;
				float width = Vector2.Distance (gridPosition.Position, fillerCenter) - doorSize;
				Vector2 oppositeDir = direction * -1;
				Dictionary<Vector2, float> fillerSize = InitSizeDict ();
				fillerSize [direction] = width;
				fillerSize [oppositeDir] = width;
				generator.AddHallwaySegment (new HallwaySegment (fillerSize, fillerCenter));
			}
			return doorSize;
		}

		Vector2 neighbourPosition = adjacent [direction].Position;

		if (IsEdge (adjacent [direction])) {
			//If the segment in the looking direction is an edge, it will have the default size of doorSize
			//This segment has to compensate for the lack the edges length by using the whole distance between the segments minus one doorLength
			return (Vector2.Distance (gridPosition.Position, neighbourPosition) * direction - direction * doorSize).magnitude;
		} else {
			//The segment is not an edge, meaning that this is a straigth corridor. Use half the distance between the both segments as padding
			return (Vector2.Distance (gridPosition.Position, neighbourPosition) * direction * .5f).magnitude;
		}
	}

	//Edges will have squared size
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

	public override bool Equals (object obj){
		if (obj == null)
			return false;
		if (ReferenceEquals (this, obj))
			return true;
		if (obj.GetType () != typeof(HallwaySegment))
			return false;
		HallwaySegment other = (HallwaySegment)obj;
		return center == other.center;
	}

	public override int GetHashCode (){
		unchecked {
			return center.GetHashCode ();
		}
	}
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
				HallwaySegment newSegment = new HallwaySegment (gridPosition, this);
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

	public void AddHallwaySegment(HallwaySegment segment){
		if (!hallwaySegments.Contains (segment)) {
			hallwaySegments.Add (segment);
		}
	}
}
