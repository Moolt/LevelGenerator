using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class HallwaySegment{
	private static Dictionary<Vector2, int[]> directionTrianglesMapping;
	private float doorSize = 1f;
	//Center of the segment = Center of the gridPosition
	private Vector2 center;
	//Hallways hight, should be 2 times the width
	private float height;
	//Generated
	//Stores all adjacent gridPositions for this segment
	//Key is the direction as Vector2, null if there is no gridPos
	private Dictionary<Vector2, GridPosition> adjacent;
	//The size of the segment in the direction stored as Vector2.
	//Always positive values, used for mesh generation
	private Dictionary<Vector2, float> size;
	//If two doors are opposing each other, they will have the same path in the grid
	//Since their positions will differ, the grid in AStarGrid is adjusted to door positions
	//One door will have to compensate for the resulting offset
	private Dictionary<Vector2, float> offset;
	//Reference to the hallwayGenerator to add filler segments, if neccessary
	private HallwayMeshGenerator generator;
	private GridPosition gridPosition;
	//The results of the generation process are the triangles and vertices
	private List<int>[] triangles;
	private List<Vector3> vertices;
	private List<Vector2> uvs;

	private HallwaySegment(){
		InitOffsetDict ();
		height = 2f;
	}

	public HallwaySegment (GridPosition gridPosition, HallwayMeshGenerator generator): this(){
		InitTriangleMapping ();
		this.gridPosition = gridPosition;
		this.center = gridPosition.Position;
		this.adjacent = gridPosition.AdjacentPositions;
		this.generator = generator;
		size = CreateSizeDict ();
		CalculateBounds ();
	}

	public HallwaySegment(Dictionary<Vector2, float> size, Dictionary<Vector2, GridPosition> adjacent, Vector2 center): this(){
		this.adjacent = adjacent;
		this.center = center;
		this.size = size;
	}

	private Dictionary<Vector2, float> CreateSizeDict(){
		Dictionary<Vector2, float> _size = new Dictionary<Vector2, float> ();
		_size.Add (Vector2.up, doorSize);
		_size.Add (Vector2.right, doorSize);
		_size.Add (Vector2.down, doorSize);
		_size.Add (Vector2.left, doorSize);
		return _size;
	}

	private void InitOffsetDict(){
		offset = new Dictionary<Vector2, float> ();
		offset.Add (Vector2.up, 0f);
		offset.Add (Vector2.right, 0f);
		offset.Add (Vector2.down, 0f);
		offset.Add (Vector2.left, 0f);
	}

	private void InitTriangleMapping(){
		if (directionTrianglesMapping == null) {
			directionTrianglesMapping = new Dictionary<Vector2, int[]> ();
			directionTrianglesMapping.Add (Vector2.up, new int[] { 2, 1, 0, 0, 3, 2 });
			directionTrianglesMapping.Add (Vector2.right, new int[] { 3, 0, 5, 5, 6, 3 });
			directionTrianglesMapping.Add (Vector2.down, new int[] { 6, 5, 4, 4, 7, 6 });
			directionTrianglesMapping.Add (Vector2.left, new int[] { 7, 4, 1, 1, 2, 7 });
		}
	}

	private void CalculateBounds(){
		Vector2 oppositeDir = gridPosition.Direction * -1;
		//Only modify padding in the direction of the hallway segment
		//The other directions will have their default value (doorSize)
		size[gridPosition.Direction] = GetPaddingForDirection(gridPosition.Direction, false);
		size[oppositeDir] = GetPaddingForDirection (oppositeDir, true);
		HandleOffset ();
		//Add an epmty gridPosition in the opposite direction, if the segment is a door
		//This will stop triangles to be created at the beginning of a hallway
		if (gridPosition.IsDoor) {
			gridPosition.AddAdjacent (oppositeDir, new GridPosition (0, 0));
		}
	}

	private float GetPaddingForDirection(Vector2 direction, bool isOpposite){		
		//The direction is pointing inside of a room. Will only happen with door positions
		if (adjacent [direction] == null && gridPosition.IsDoor) {
			return 0f;
		}
		//If this segment and a door segment form a straigth passage, give the space to the door
		//The door may need the space if it has an offset
		if (adjacent [direction] != null && adjacent[direction].IsDoor && !IsEdge(gridPosition)) {
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
				CreateFiller (direction);
			}
			return doorSize;
		}

		Vector2 neighbourPosition = adjacent [direction].Position;
		float neighbourDistance = Vector2.Distance (Vector2.Scale (gridPosition.Position, direction), Vector2.Scale (neighbourPosition, direction));

		if (IsEdge (adjacent [direction])) {
			//If the segment in the looking direction is an edge, it will have the default size of doorSize
			//This segment has to compensate for the lack the edges length by using the whole distance between the segments minus one doorLength
			return (neighbourDistance * direction - direction * doorSize).magnitude;
		} else {
			if (gridPosition.IsDoor) {
				return (neighbourDistance * direction).magnitude;
			} else {
				//The segment is not an edge, meaning that this is a straigth corridor. Use half the distance between the both segments as padding
				return (neighbourDistance * direction * .5f).magnitude;
			}
		}
	}

	private void HandleOffset(){
		//The segment in direction of the door should never be null, but just to be sure...
		if (gridPosition.IsDoor && adjacent[gridPosition.Direction] != null) {
			Vector2 distance = adjacent [gridPosition.Direction].Position - gridPosition.Position;
			//If direction.x is 0f, we want the vertical offset, else the horizontal
			offset[gridPosition.Direction] = gridPosition.Direction.x != 0f ? distance.y : distance.x;
		}
	}

	//Creates a filler in direction of this segment
	//Will only be executed if this segment and the segment in the given direction are edges
	private void CreateFiller(Vector2 direction){
		//The position between the both segments
		Vector2 fillerCenter = gridPosition.Position + Vector2.Distance (gridPosition.Position, adjacent [direction].Position) * direction * .5f;
		//Actually half the full width.
		float width = Vector2.Distance (gridPosition.Position, fillerCenter) - doorSize;
		Vector2 oppositeDir = direction * -1;
		//A new adjacent dictionary has to be created for the filler, since it doesn't originate from
		//An actual GridPosition instance.
		Dictionary<Vector2, GridPosition> fillerAdjacent = new Dictionary<Vector2, GridPosition>();
		fillerAdjacent.Add (direction, adjacent [direction]);
		fillerAdjacent.Add (oppositeDir, gridPosition);
		//New size Dictionary. Default values to the sides, width for back and forth
		Dictionary<Vector2, float> fillerSize = CreateSizeDict ();
		fillerSize [direction] = width;
		fillerSize [oppositeDir] = width;
		//New instance is added to the hallway. The called function will stop duplicates from being added.
		generator.AddHallwaySegment (new HallwaySegment (fillerSize, fillerAdjacent, fillerCenter));
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

	public void BuildMesh(int index){
		Vector3[] _vertices = CalculateVertices ();
		CalculateTriangles ();
		BuildVertexList (_vertices);
		CalculateUVs ();
		OffsetVertexIndices (index);
	}

	private Vector3[] CalculateVertices(){
		return new Vector3[] {
			new Vector3 (size[Vector2.right] + center.x + offset[Vector2.up], height, size[Vector2.up] + center.y + offset[Vector2.right]),
			new Vector3 (-size[Vector2.left] + center.x + offset[Vector2.up], height, size[Vector2.up] + center.y + offset[Vector2.left]),
			new Vector3 (-size[Vector2.left] + center.x + offset[Vector2.up], 0f, size[Vector2.up] + center.y + offset[Vector2.left]),
			new Vector3 (size[Vector2.right] + center.x + offset[Vector2.up], 0f, size[Vector2.up] + center.y + offset[Vector2.right]),
			new Vector3 (-size[Vector2.left] + center.x + offset[Vector2.down], height, -size[Vector2.down] + center.y + offset[Vector2.left]),
			new Vector3 (size[Vector2.right] + center.x + offset[Vector2.down], height, -size[Vector2.down] + center.y + offset[Vector2.right]),
			new Vector3 (size[Vector2.right] + center.x + offset[Vector2.down], 0f, -size[Vector2.down] + center.y + offset[Vector2.right]),
			new Vector3 (-size[Vector2.left] + center.x + offset[Vector2.down], 0f, -size[Vector2.down] + center.y + offset[Vector2.left])
		};
	}

	private void OffsetVertexIndices(int index){
		for (int i = 0; i < 2; i++) {
			for (int j = 0; j < triangles[i].Count; j++) {
				triangles[i][j] += index;
			}
		}
	}

	private Vector2[] FaceUVs(params Vector3[] vertices){		
		int verticeCount = vertices.Length;
		List<Vector2> faceUVs = new List<Vector2> ();

		Vector3 tangent = (vertices [0] - vertices [1]).normalized;
		Vector3 biTangent = (vertices [1] - vertices [2]).normalized;

		for (int i = 0; i < verticeCount; i++) {
			Vector2 uv = new Vector2 ();
			uv.x = vertices [i].x * tangent.x + vertices [i].y * tangent.y + vertices [i].z * tangent.z;
			uv.y = vertices [i].x * biTangent.x + vertices [i].y * biTangent.y + vertices [i].z * biTangent.z;
			uv *= 1f;
			faceUVs.Add (uv);
		}

		return faceUVs.ToArray();
	}

	private void BuildVertexList(Vector3[] _vertices){
		vertices = new List<Vector3> ();
		for (int i = 0; i < 2; i++) {
			for (int j = 0; j < triangles [i].Count; j += 3) {
				vertices.Add (_vertices [triangles [i] [j]]);
				vertices.Add (_vertices [triangles [i] [j + 1]]);
				vertices.Add (_vertices [triangles [i] [j + 2]]);
				triangles [i] [j] = vertices.Count - 3;
				triangles [i] [j + 1] = vertices.Count - 2;
				triangles [i] [j + 2] = vertices.Count - 1;
			}
		}
	}

	private void CalculateUVs(){
		uvs = new List<Vector2> ();
		for (int i = 0; i < 2; i++) {
			for (int j = 0; j < triangles [i].Count; j += 3) {
				uvs.AddRange (FaceUVs (
					vertices [triangles [i] [j]], 
					vertices [triangles [i] [j + 1]],
					vertices [triangles [i] [j + 2]])
				);
			}
		}
	}

	private void CalculateTriangles(){
		triangles = new List<int>[2];
		triangles[0] = new List<int> ();
		triangles[0].AddRange (new int[] { 5, 0, 1, 1, 4, 5 });
		triangles[0].AddRange (new int[] { 7, 2, 3, 3, 6, 7 });

		triangles[1] = new List<int> ();
		foreach (KeyValuePair<Vector2, int[]> tris in directionTrianglesMapping) {
			if (!adjacent.ContainsKey (tris.Key) || adjacent [tris.Key] == null) {
				triangles [1].AddRange (tris.Value);
			}
		}
	}

	public List<int>[] Triangles(){
		return triangles;
	}

	public List<Vector2> UVs {
		get {
			return this.uvs;
		}
	}

	public List<Vector3> Vertices{
		get{
			return vertices;
		}
	}

	//Hallway Segments are equal, if their positions are equal
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
	private List<int>[] triangles;
	private List<Vector2> uvs;
	private Mesh mesh;

	public HallwayMeshGenerator(AStarGrid grid){
		this.grid = grid;
		this.hallwayPaths = new List<List<Square>> ();
		this.hallwaySegments = new HashSet<HallwaySegment> ();
		this.vertices = new List<Vector3> ();
		this.triangles = new List<int>[2];
		this.triangles [0] = new List<int> ();
		this.triangles [1] = new List<int> ();
		this.uvs = new List<Vector2> ();
	}

	public Mesh GenerateMesh(){
		mesh = new Mesh ();

		PrepareHallwaySegments ();

		int currentVertexIndex = 0;
		foreach (HallwaySegment segment in hallwaySegments) {
			segment.BuildMesh (currentVertexIndex);
			vertices.AddRange (segment.Vertices);
			List<int>[] hwTriangles = segment.Triangles ();
			currentVertexIndex = Mathf.Max (hwTriangles [0].Max (), hwTriangles [1].Max ()) + 1;
			triangles[0].AddRange (hwTriangles[0]);
			triangles[1].AddRange (hwTriangles[1]);
			uvs.AddRange (segment.UVs);
		}
		mesh.vertices = vertices.ToArray ();
		mesh.subMeshCount = triangles.Length;
		mesh.SetTriangles (triangles [0], 0);
		mesh.SetTriangles (triangles [1], 1);
		mesh.uv = uvs.ToArray();
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
