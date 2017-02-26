using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class HallwaySegment{
	private Vector2 center;
	private float xMin;
	private float yMin;
	private float xMax;
	private float yMax;
	private float height;
	//Generated
	private Vector3[] vertices;
	private List<int>  triangles;
	private Square square;

	public HallwaySegment (Square square){
		this.square = square;
		//this.center = center;
		this.center = square.Position;
		xMin = yMin = -1f;
		xMax = yMax = 1f;
		xMax += center.x;
		xMin += center.x;
		yMax += center.y;
		yMin += center.y;
		height = 2f;
	}

	private void CalculateVertices(){
		vertices = new Vector3[] {
			new Vector3 (xMax, height, yMax),
			new Vector3 (xMin, height, yMax),
			new Vector3 (xMin, 0f, yMax),
			new Vector3 (xMax, 0f, yMax),
			new Vector3 (xMin, height, yMin),
			new Vector3 (xMax, height, yMin),
			new Vector3 (xMax, 0f, yMin),
			new Vector3 (xMin, 0f, yMin)
		};
	}

	public List<int> Triangles(int index){
		if (vertices == null) {
			CalculateVertices ();
		}
		int[][] _triangles = new int[][] {
			new int[] { 0, 1, 2, 3 }, //back
			new int[] { 4, 5, 6, 7 }, //front
			new int[] { 1, 0, 5, 4 }, //top
			new int[] { 3, 2, 7, 6 }, //bottom
			new int[] { 1, 4, 7, 2 }, //left
			new int[] { 5, 0, 3, 6 }, //right
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
	private List<List<Square>> hallwayPaths;
	private List<Vector3> vertices;
	private List<int> triangles;
	private Mesh mesh;

	public HallwayMeshGenerator(){
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
				HallwaySegment newSegment = new HallwaySegment (square);
				if (hallwaySegments.Contains (newSegment)) {
					//verbindung hinzufügen
				} else {
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
