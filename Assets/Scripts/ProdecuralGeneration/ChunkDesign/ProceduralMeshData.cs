using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ProceduralMeshData{
	private MeshShape meshShape = MeshShape.RAMP;
	//Cylinder iterations
	private int iterations = 5;
	private Vector3 extends;
	private float tiling;
	//Terrain Properties
	private float terrainCellSize = 0.5f;
	private float terrainScale = 1f;
	private float terrainElevation = 1f;
	private bool zeroEdges = true;

	private bool isDirty = true;
	private Vector3[] _rawVertices = new Vector3[0];
	private int[][] _rawTriangles = new int[0][];
	private bool verticesCalculated = false;
	private bool trianglesCalculated = false;

	public Vector3[] Vertices{ get; set; }
	public int[] Triangles { get; set; }
	public Vector2[] UVs { get; set; }

	private Vector3[] RawVertices{
		get{
			Vector3 e = extends;

			if (verticesCalculated) {
				return _rawVertices;
			}

			if (meshShape == MeshShape.RAMP) {
				_rawVertices = new Vector3[] {
					new Vector3 (e.x, e.y, e.z),
					new Vector3 (-e.x, e.y, e.z),
					new Vector3 (-e.x, -e.y, e.z),
					new Vector3 (e.x, -e.y, e.z),
					new Vector3 (-e.x, -e.y, -e.z),
					new Vector3 (e.x, -e.y, -e.z)
				};
			}

			if (meshShape == MeshShape.CUBE) {
				_rawVertices = new Vector3[] {
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
				_rawVertices = new Vector3[] {
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

				for (int i = 0; i < iterations + 1; i++) {
					float x = extends.x * Mathf.Cos (i * ((Mathf.PI * 2 ) / iterations));
					float z = extends.z * Mathf.Sin (i * ((Mathf.PI * 2 ) / iterations));
					cylinderVerts.Add(new Vector3(x, -extends.y, z));
					cylinderVerts.Add(new Vector3(x, extends.y, z));
				}

				//Caps
				cylinderVerts.Add (new Vector3 (0f, -extends.y, 0f));
				cylinderVerts.Add (new Vector3 (0f, extends.y, 0f));

				_rawVertices = cylinderVerts.ToArray ();
			}

			if (meshShape == MeshShape.PLANE) {
				_rawVertices = new Vector3[] {
					new Vector3 (-e.x, -e.y, -e.z),
					new Vector3 (e.x, -e.y, -e.z),
					new Vector3 (e.x, -e.y, e.z),
					new Vector3 (-e.x, -e.y, e.z)
				};
			}

			if (meshShape == MeshShape.TERRAIN) {
				List<Vector3> terrainVerts = new List<Vector3> ();
				int xIterations = (int)Mathf.Round ((e.x * 2f) / terrainCellSize);
				int yIterations = (int)Mathf.Round ((e.z * 2f) / terrainCellSize);
				float terrainSeed = ChunkInstantiator.Instance.ProcessType != ProcessType.PREVIEW ? Random.value * 100 : 1f;
				for (int x = 0; x < xIterations; x++) {
					for (int y = 0; y < yIterations; y++) {
						terrainVerts.Add (TerrainPosition (terrainSeed, x, y));
						terrainVerts.Add (TerrainPosition (terrainSeed, x + 1, y));
						terrainVerts.Add (TerrainPosition (terrainSeed, x + 1, y + 1));
						terrainVerts.Add (TerrainPosition (terrainSeed, x, y + 1));
					}
				}
				_rawVertices = terrainVerts.ToArray ();
			}

			verticesCalculated = true;
			return _rawVertices;
		}
	}

	private int[][] RawTriangles{
		get{

			if (trianglesCalculated) {
				return _rawTriangles;
			}

			if (meshShape == MeshShape.RAMP) {
				_rawTriangles = new int[][] {
					new int[] { 0, 1, 2, 3 }, //back
					new int[] { 1, 0, 5, 4 }, //slope
					new int[] { 3, 2, 4, 5 }, //bottom
					new int[] { 0, 3, 5 }, //right tri
					new int[] { 4, 2, 1 }, //left tri
				};
			}

			if (meshShape == MeshShape.CUBE) {
				_rawTriangles = new int[][] {
					new int[] { 0, 1, 2, 3 }, //back
					new int[] { 4, 5, 6, 7 }, //front
					new int[] { 1, 0, 5, 4 }, //top
					new int[] { 3, 2, 7, 6 }, //bottom
					new int[] { 1, 4, 7, 2 }, //left
					new int[] { 5, 0, 3, 6 }, //right
				};
			}

			if (meshShape == MeshShape.TRIANGULAR) {
				_rawTriangles = new int[][] {
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
				List<int[]> cylinderQuads = new List<int[]>();
				int topCenter = RawVertices.Length - 1;
				int botCenter = RawVertices.Length - 2;

				for (int i = 0; i < iterations; i++) {
					int j = i * 2;
					cylinderQuads.Add (new int[] { j + 1, j + 3, j + 2, j + 0 });
					cylinderQuads.Add (new int[] { j + 3, j + 1, topCenter });
					cylinderQuads.Add (new int[] { j + 0, j + 2, botCenter });
				}

				_rawTriangles = cylinderQuads.ToArray();
			}

			if (meshShape == MeshShape.PLANE) {
				_rawTriangles = new int[][] {
					new int[] { 1, 0, 3, 2 } //top
				};
			}

			if (meshShape == MeshShape.TERRAIN) {
				Vector3 e = extends;
				List<int[]> terrainQuads = new List<int[]> ();
				int xIterations = (int)Mathf.Round ((e.x * 2f) / terrainCellSize);
				int yIterations = (int)Mathf.Round ((e.z * 2f) / terrainCellSize);
				int quadCount = xIterations * yIterations;

				for (int i = 0; i < quadCount; i++) {
					int m = i * 4;
					terrainQuads.Add (new int[] { 1 + m, 0 + m, 3 + m, 2 + m});
				}
				_rawTriangles = terrainQuads.ToArray ();
			}

			trianglesCalculated = true;
			return _rawTriangles;
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
			uv *= tiling;
			faceUVs.Add (uv);
		}

		return faceUVs.ToArray();
	}

	private Vector2[] TerrainUVs(int index){
		Vector3[] vertices = FaceVertices (index);
		return new Vector2[] {
			new Vector2 (vertices [0].x, vertices [0].z) * tiling,
			new Vector2 (vertices [1].x, vertices [1].z) * tiling,
			new Vector2 (vertices [2].x, vertices [2].z) * tiling,
			new Vector2 (vertices [3].x, vertices [3].z) * tiling
		};
	} 

	public void BuildMesh(){
		if (isDirty) {
			List<Vector3> verticeList = new List<Vector3> ();
			List<Vector2> uvList = new List<Vector2> ();
			List<int> triangleList = new List<int> ();

			for (int i = 0; i < RawTriangles.Length; i++) {
				Vector3[] faceVertices = FaceVertices (i);
				Vector2[] faceUVs = IsTerrain() ? TerrainUVs(i) : FaceUVs (i);

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
			isDirty = false;
			verticesCalculated = false;
			trianglesCalculated = false;
		}
	}

	private Vector3 TerrainPosition(float seed, int x, int y){
		Vector3 e = extends;
		float xPosition = terrainCellSize * x;
		float yPosition = terrainCellSize * y;
		float height = Mathf.PerlinNoise (seed + xPosition / terrainScale, seed + yPosition / terrainScale);
		height *= terrainElevation;

		return new Vector3 (- e.x + xPosition, height, -e.z + yPosition);
	}

	private bool IsTerrain(){
		return meshShape == MeshShape.TERRAIN;
	}

	public MeshShape MeshShape {
		get {
			return this.meshShape;
		}
		set {
			if (value != meshShape) {
				meshShape = value;
				isDirty = true;
			}
		}
	}

	public Vector3 Extends {
		get {
			return this.extends;
		}
		set {
			if (value != extends) {
				extends = value;
				isDirty = true;
			}
		}
	}

	public float Tiling {
		get {
			return this.tiling;
		}
		set {
			if (value != tiling) {
				tiling = value;
				isDirty = true;
			}
		}
	}

	public int Iterations {
		get {
			return this.iterations;
		}
		set {
			if (value != iterations) {
				iterations = value;
				isDirty = true;
			}
		}
	}

	public float TerrainCellSize {
		get {
			return this.terrainCellSize;
		}
		set {
			if (value != terrainCellSize) {
				terrainCellSize = value;
				isDirty = true;
			}
		}
	}

	public float TerrainScale {
		get {
			return this.terrainScale;
		}
		set {
			if (value != terrainScale) {
				terrainScale = value;
				isDirty = true;
			}
		}
	}

	public float TerrainElevation {
		get {
			return this.terrainElevation;
		}
		set {
			if (value != terrainElevation) {
				terrainElevation = value;
				isDirty = true;
			}
		}
	}

	public bool ZeroEdges {
		get {
			return this.zeroEdges;
		}
		set {
			if (value != zeroEdges) {
				zeroEdges = value;
				isDirty = true;
			}
		}
	}
}