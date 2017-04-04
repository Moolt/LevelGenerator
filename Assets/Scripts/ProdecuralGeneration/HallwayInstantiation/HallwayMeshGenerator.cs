using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class HallwaySegment{
	private static float hallwayTiling = 1f; //Texture tiling
	private static Dictionary<Vector2, int[]> directionTrianglesMapping;
	private float doorSize;
	//Center of the segment = Center of the gridPosition
	private Vector2 center;
	//Hallways hight, should be 2 times the width since the lower y value is always 0
	private float height;
	//Generated
	//Stores all adjacent gridPositions for this segment
	//Key is the direction as Vector2, null if there is no gridPos
	private Dictionary<Vector2, GridPosition> adjacent;
	//The size of the segment in the direction stored as Vector2.
	//Always positive values, used for mesh generation
	private Dictionary<Vector2, float> size;
	private GridPosition gridPosition;
	//The results of the generation process are the triangles and vertices
	private List<int>[] triangles;
	private List<Vector3> vertices;
	private List<Vector2> uvs;

	private HallwaySegment(){
		doorSize = DoorDefinition.GlobalSize * .5f;
		height = doorSize * 2f;
	}

	public HallwaySegment (GridPosition gridPosition): this(){
		InitTriangleMapping ();
		this.gridPosition = gridPosition;
		this.center = gridPosition.Position;
		this.adjacent = gridPosition.AdjacentPositions;
		size = CreateSizeDict ();
		CalculateBounds ();
	}

	private Dictionary<Vector2, float> CreateSizeDict(){
		Dictionary<Vector2, float> _size = new Dictionary<Vector2, float> ();
		_size.Add (Vector2.up, doorSize);
		_size.Add (Vector2.right, doorSize);
		_size.Add (Vector2.down, doorSize);
		_size.Add (Vector2.left, doorSize);
		return _size;
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
		//HandleInsideEdge(adjacent[gridPosition.Direction]);
		Vector2 oppositeDir = gridPosition.Direction * -1;
		//Only modify padding in the direction of the hallway segment
		//The other directions will have their default value (doorSize)
		size[gridPosition.Direction] = GetPaddingForDirection(gridPosition.Direction);
		size[oppositeDir] = GetPaddingForDirection (oppositeDir);
		//Add an epmty gridPosition in the opposite direction, if the segment is a door
		//This will stop triangles to be created at the beginning of a hallway
		if (gridPosition.IsDoor) {
			gridPosition.AddAdjacent (oppositeDir, new GridPosition (0, 0));
		}
	}

	private float GetPaddingForDirection(Vector2 direction){		
		//The direction is pointing inside of a room. Will only happen with door positions
		if (adjacent [direction] == null && gridPosition.IsDoor) {
			return 0f;
		}
		return doorSize;
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

	private void OffsetVertexIndices(int index){
		for (int i = 0; i < triangles.Length; i++) {
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
			uv *= hallwayTiling;
			faceUVs.Add (uv);
		}

		return faceUVs.ToArray();
	}

	private void BuildVertexList(Vector3[] _vertices){
		vertices = new List<Vector3> ();
		for (int i = 0; i < triangles.Length; i++) {
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
		for (int i = 0; i < triangles.Length; i++) {
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
		triangles = new List<int>[3];
		triangles[0] = new List<int> ();
		triangles[0].AddRange (new int[] { 5, 0, 1, 1, 4, 5 });
		triangles[1] = new List<int> ();
		triangles[1].AddRange (new int[] { 7, 2, 3, 3, 6, 7 });

		triangles[2] = new List<int> ();
		foreach (KeyValuePair<Vector2, int[]> tris in directionTrianglesMapping) {
			if (!adjacent.ContainsKey (tris.Key) || adjacent [tris.Key] == null) {
				triangles [2].AddRange (tris.Value);
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

	public static float HallwayTiling {
		get {
			return hallwayTiling;
		}
		set {
			hallwayTiling = value;
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

	public HallwayMeshGenerator(AStarGrid grid, float hallwayTiling){
		HallwaySegment.HallwayTiling = hallwayTiling;
		this.grid = grid;
		this.hallwayPaths = new List<List<Square>> ();
		this.hallwaySegments = new HashSet<HallwaySegment> ();
		this.vertices = new List<Vector3> ();
		this.triangles = new List<int>[3];
		//Init triangles with empty lists
		for (int i = 0; i < triangles.Length; i++) {
			this.triangles [i] = new List<int> ();
		}
		this.uvs = new List<Vector2> ();
	}

	public Mesh GenerateMesh(bool searchForAdjacents){
		mesh = new Mesh ();

		PrepareHallwaySegments (searchForAdjacents);

		int currentVertexIndex = 0;
		foreach (HallwaySegment segment in hallwaySegments) {
			segment.BuildMesh (currentVertexIndex);
			vertices.AddRange (segment.Vertices);
			List<int>[] hwTriangles = segment.Triangles ();

			for(int i = 0; i < triangles.Length; i++){
				if (hwTriangles [i] != null && hwTriangles[i].Count > 0) {
					currentVertexIndex = Mathf.Max (hwTriangles [i].Max (), currentVertexIndex);
				}
			}
			currentVertexIndex += 1;

			for(int i = 0; i < triangles.Length; i++){
				triangles[i].AddRange (hwTriangles[i]);
			}
			uvs.AddRange (segment.UVs);
		}
		mesh.vertices = vertices.ToArray ();
		mesh.subMeshCount = triangles.Length;
		for(int i = 0; i < triangles.Length; i++){
			mesh.SetTriangles (triangles [i], i);
		}
		mesh.uv = uvs.ToArray();
		mesh.RecalculateNormals ();
		mesh.RecalculateBounds ();
		return mesh;
	}

	private void PrepareHallwaySegments(bool searchForAdjacents){
		foreach (List<Square> squares in hallwayPaths) {
			foreach (Square square in squares) {
				GridPosition gridPosition = grid.Grid [square.GridX, square.GridY];
				if (searchForAdjacents) {
					SearchForIndirectAdjacents (gridPosition);
				}
				HallwaySegment newSegment = new HallwaySegment (gridPosition);
				if (!hallwaySegments.Contains (newSegment)) {
					hallwaySegments.Add (newSegment);
				}
			}
		}
	}

	//Actual adjascents, meaning adjascent positions on the same path, were computed before when creating the paths in the astar algorithm
	//Here the positions of _all_ paths are compared
	private void SearchForIndirectAdjacents(GridPosition gridPosition){
		Vector2[] directions = new Vector2[]{ Vector2.up, Vector2.right, Vector2.down, Vector2.left };
		int[] indices = new int[]{ 0, 1, 1, 0, 0, -1, -1, 0 };
		for (int i = 0; i < 4; i++) {
			GridPosition adjacent = grid.Grid [gridPosition.i + indices [i * 2], gridPosition.j + indices [i * 2 + 1]];
			if (adjacent.IsPartOfPath && gridPosition.AdjacentPositions[directions[i]] == null && Vector2.Distance (gridPosition.Position, adjacent.Position) <= DoorDefinition.GlobalSize * 2f) {
				gridPosition.AddAdjacent (directions [i], adjacent);
			}
		}
	}

	public void AddPath(List<Square> path){
		hallwayPaths.Add (path);
	}

	public Mesh Mesh{
		get{ 
			GenerateMesh (true);
			return mesh; 
		}
	}
}
