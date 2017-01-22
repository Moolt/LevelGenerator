using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MeshShape{ CUBE, RAMP, TRIANGULAR, CYLINDER }

public class ProceduralMeshData{
	private MeshShape meshShape = MeshShape.RAMP;
	private Vector3 extends;
	private float tiling;
	private int iterations = 5;

	public Vector3[] Vertices{ get; set; }
	public int[] Triangles { get; set; }
	public Vector2[] UVs { get; set; }

	private Vector3[] RawVertices{
		get{
			Vector3 e = extends;

			if (meshShape == MeshShape.RAMP) {
				return new Vector3[] {
					new Vector3 (e.x, e.y, e.z),
					new Vector3 (-e.x, e.y, e.z),
					new Vector3 (-e.x, -e.y, e.z),
					new Vector3 (e.x, -e.y, e.z),
					new Vector3 (-e.x, -e.y, -e.z),
					new Vector3 (e.x, -e.y, -e.z)
				};
			}

			if (meshShape == MeshShape.CUBE) {
				return new Vector3[] {
					new Vector3 (e.x, e.y, e.z),
					new Vector3 (-e.x, e.y, e.z),
					new Vector3 (-e.x, -e.y, e.z),
					new Vector3 (e.x, -e.y, e.z),
					new Vector3 (-e.x, e.y, -e.z),
					new Vector3 (e.x, e.y, -e.z),
					new Vector3 (e.x, -e.y, -e.z),
					new Vector3 (-e.x, -e.y, -e.z)
				};
			}

			if (meshShape == MeshShape.TRIANGULAR) {
				return new Vector3[] {
					new Vector3 (e.x, e.y, 0f),
					new Vector3 (-e.x, e.y, 0f),
					new Vector3 (-e.x, -e.y, e.z),
					new Vector3 (e.x, -e.y, e.z),
					new Vector3 (e.x, -e.y, -e.z),
					new Vector3 (-e.x, -e.y, -e.z),
					new Vector3 (-e.x, -e.y, 0f),
					new Vector3 (e.x, -e.y, 0f),
					new Vector3 (e.x, -e.y, -e.z)
				};
			}

			if (meshShape == MeshShape.CYLINDER) {
				List<Vector3> cylinderVerts = new List<Vector3> ();

				for (int i = 0; i < iterations; i++) {
					float x = extends.x * Mathf.Cos (i * ((Mathf.PI * 2 ) / iterations));
					float z = extends.z * Mathf.Sin (i * ((Mathf.PI * 2 ) / iterations));
					cylinderVerts.Add(new Vector3(x, -extends.y, z));
					cylinderVerts.Add(new Vector3(x, extends.y, z));
				}

				//Caps
				cylinderVerts.Add (new Vector3 (0f, -extends.y, 0f));
				cylinderVerts.Add (new Vector3 (0f, extends.y, 0f));

				return cylinderVerts.ToArray ();
			}

			return new Vector3[0];
		}
	}

	private int[][] RawTriangles{
		get{
			if (meshShape == MeshShape.RAMP) {
				return new int[][] {
					new int[] { 0, 1, 2, 3 }, //back
					new int[] { 1, 0, 5, 4 }, //slope
					new int[] { 3, 2, 4, 5 }, //bottom
					new int[] { 0, 3, 5 }, //right tri
					new int[] { 4, 2, 1 }, //left tri
				};
			}

			if (meshShape == MeshShape.CUBE) {
				return new int[][] {
					new int[] { 0, 1, 2, 3 }, //back
					new int[] { 4, 5, 6, 7 }, //front
					new int[] { 1, 0, 5, 4 }, //top
					new int[] { 3, 2, 7, 6 }, //bottom
					new int[] { 1, 4, 7, 2 }, //left
					new int[] { 5, 0, 3, 6 }, //right
				};
			}

			if (meshShape == MeshShape.TRIANGULAR) {
				return new int[][] {
					new int[] { 0, 1, 2, 3 }, //back
					new int[] { 1, 0, 4, 5}, //front
					new int[] { 3, 2, 5, 4 }, //bottom
					new int[] { 6, 2, 1 }, //left, back
					new int[] { 5, 6, 1 }, //left, front
					new int[] { 3, 7, 0 }, //right, back
					new int[] { 4, 0, 7 }, //right, front
				};
			}

			if (meshShape == MeshShape.CYLINDER) {
				int[][] cylinderQuads = new int[iterations * 3][];
				int topCenter = iterations * 2 - 1;
				int botCenter = iterations * 2 - 2;

				for (int i = 0; i < iterations - 1; i++) {
					int j = i * 2;
					cylinderQuads [i] = new int[] { j + 1, j + 3, j + 2, j + 0 };
				}

				//Last one is a bit tricky, since it has to connect to the first one
				//Iterations -2, since the top and bottom caps have to be compensated for
				cylinderQuads [iterations - 1] = new int[] { (iterations * 2 - 2) + 1, 1, 0, (iterations * 2 - 2) + 0 };

				for (int i = 0; i < iterations - 1; i++) {
					int j = i * 2;
					cylinderQuads [iterations + i] = new int[] { j + 3, j + 1, topCenter };
				}

				cylinderQuads [iterations * 2 - 1] = new int[] { (iterations - 3) * 2 + 3, 1, topCenter };

				for (int i = 0; i < iterations - 1; i++) {
					int j = i * 2;
					cylinderQuads [iterations * 2 + i] = new int[] { j + 1, j + 3, botCenter };
				}

				cylinderQuads [iterations * 3 - 1] = new int[] { 3, (iterations - 3) * 2 + 1, topCenter };
					
				return cylinderQuads;
			}

			return new int[0][];
		}
	}

	private Vector3[] FaceVertices(int index){
		int verticeCount = RawTriangles [index].Length;
		Vector3[] faces = new Vector3[verticeCount];

		for (int i = 0; i < verticeCount; i++) {
			faces [i] = RawVertices [RawTriangles [index] [i]];
		}
		return faces;
	}

	private Vector2[] FaceUVs(int index){
		Vector3[] vertices = FaceVertices (index);
		int verticeCount = vertices.Length;
		List<Vector2> faceUVs = new List<Vector2> ();

		Vector3 tangent = (vertices [0] - vertices [1]).normalized;
		Vector3 biTangent = (vertices [1] - vertices [2]).normalized;

		for (int i = 0; i < verticeCount; i++) {
			Vector2 uv = new Vector2 ();
			uv.x = vertices [i].x * tangent.x + vertices [i].y * tangent.y + vertices [i].z * tangent.z;
			uv.y = vertices [i].x * biTangent.x + vertices [i].y * biTangent.y + vertices [i].z * biTangent.z;
			faceUVs.Add (uv);
		}

		return faceUVs.ToArray();
	}

	private Vector2[] TriangleUVs(Vector3[] triangle){
		Vector2[] triUVs = new Vector2[3];

		Vector3 tangent = (triangle [0] - triangle [1]).normalized;
		Vector3 biTangent = (triangle [1] - triangle [2]).normalized;

		for (int i = 0; i < 3; i++) {
			Vector2 uv = new Vector2 ();
			uv.x = triangle [i].x * tangent.x + triangle [i].y * tangent.y + triangle [i].z * tangent.z;
			uv.y = triangle [i].x * biTangent.x + triangle [i].y * biTangent.y + triangle [i].z * biTangent.z;
			uv *= tiling;
			triUVs [i] = uv;
		}
		return triUVs;
	}

	public void BuildMesh(){
		List<Vector3> verticeList = new List<Vector3> ();
		List<Vector2> uvList = new List<Vector2> ();
		List<int> triangleList = new List<int> ();

		for (int i = 0; i < RawTriangles.Length; i++) {
			Vector3[] faceVertices = FaceVertices (i);
			Vector2[] faceUVs = FaceUVs (i);

			verticeList.AddRange (faceVertices);
			uvList.AddRange (faceUVs);

			int absCount = verticeList.Count;
			int faceVertCount = faceVertices.Length;

			triangleList.Add (absCount - faceVertCount + 0);
			triangleList.Add (absCount - faceVertCount + 1);
			triangleList.Add (absCount - faceVertCount + 2);
			if (faceVertCount == 4) {
				triangleList.Add (absCount - faceVertCount + 0);
				triangleList.Add (absCount - faceVertCount + 2);
				triangleList.Add (absCount - faceVertCount + 3);
			}
		}
		Triangles = triangleList.ToArray ();
		Vertices = verticeList.ToArray ();
		UVs = uvList.ToArray ();
	}

	public MeshShape MeshShape {
		get {
			return this.meshShape;
		}
		set {
			meshShape = value;
		}
	}

	public Vector3 Extends {
		get {
			return this.extends;
		}
		set {
			extends = value;
		}
	}

	public float Tiling {
		get {
			return this.tiling;
		}
		set {
			tiling = value;
		}
	}

	public int Iterations {
		get {
			return this.iterations;
		}
		set {
			iterations = value;
		}
	}
}

[RequireComponent (typeof(MeshFilter), typeof(MeshRenderer))]
public class AbstractMesh : MeshProperty {
	public MeshShape meshShape = MeshShape.CUBE;
	public Vector3 extends = Vector3.one;
	public Material material;
	public float tiling = 1f;
	public int iterations = 4;

	private ProceduralMeshData meshData = new ProceduralMeshData ();
	private MeshRenderer meshRenderer;
	private MeshFilter meshFilter;
	private Mesh mesh;

	private void Init(){
		meshFilter = GetComponent<MeshFilter> ();
		meshRenderer = GetComponent<MeshRenderer> ();

		if (meshFilter.sharedMesh == null) {
			meshFilter.sharedMesh = new Mesh ();
			meshFilter.sharedMesh.name = "Procedural Mesh";
		}

		mesh = meshFilter.sharedMesh;
	}

	private void AssignMesh(){
		mesh.Clear ();
		meshData.Extends = extends;
		meshData.Tiling = tiling;
		meshData.MeshShape = meshShape;
		meshData.Iterations = iterations;
		meshData.BuildMesh ();
		mesh.vertices = meshData.Vertices;
		mesh.triangles = meshData.Triangles;
		mesh.uv = meshData.UVs;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds ();

		meshRenderer.material = material;
	}

	public override void Preview(){
		Init ();
		AssignMesh ();
		UpdateMeshCollider ();
	}

	private void UpdateMeshCollider(){
		MeshCollider meshCollider = GetComponent<MeshCollider> () as MeshCollider;
		if (meshCollider != null) {
			meshCollider.sharedMesh = meshFilter.sharedMesh;
		}
	}

	public override void Generate(){
	}
}
