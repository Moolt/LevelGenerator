using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomMeshData{

	//Extends
	private float width = 1f;
	private float height = 1f;
	private float length = 1f;

	//MeshInfo
	private Vector3[] vertices;
	private int[][] tris = new int[][] {
		new int[] { 0, 1, 2, 3 },
		new int[] { 1, 4, 7, 2 },
		new int[] { 4, 5, 6, 7 },
		new int[] { 5, 0, 3, 6 },
		new int[] { 3, 2, 7, 6 },
		new int[] { 1, 0, 5, 4 }
	};

	//Dirty flag for Vertice Calculation
	private bool dirty = true;

	public Vector3 Extends{
		set{
			width = value.x;
			height = value.y * 2f;
			length = value.z;
			dirty = true;
		}
	}

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
}

[RequireComponent (typeof(MeshFilter), typeof(MeshRenderer), typeof(AbstractBounds))]
public class RoomMeshGeneration : MeshProperty {
	private AbstractBounds abstractBounds;
	private Vector3 roomExtends;

	private RoomMeshData meshData;
	private List<Vector3> vertices;
	private List<int> triangles;

	private MeshFilter meshFilter;
	private Mesh mesh;

	void Preparation(){
		meshData = new RoomMeshData ();
		abstractBounds = GetComponent<AbstractBounds> ();
		meshFilter = GetComponent<MeshFilter> ();
		mesh = meshFilter.sharedMesh;
		roomExtends = abstractBounds.Extends;
	}

	public override void Preview(){
		Preparation ();
		UpdateMesh ();
	}

	private void UpdateMesh(){
		mesh.Clear ();

		meshData.Extends = roomExtends;
		vertices = new List<Vector3> ();
		triangles = new List<int> ();

		for (int i = 0; i < 6; i++) {
			MakeFace (i);
		}

		mesh.vertices = vertices.ToArray ();
		mesh.triangles = triangles.ToArray ();
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
		Preparation ();
		UpdateMesh ();
	}
}
