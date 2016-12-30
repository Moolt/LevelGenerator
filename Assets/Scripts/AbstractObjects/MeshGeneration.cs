using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

[RequireComponent (typeof(VariableBounds))]
[RequireComponent (typeof(MeshFilter))]
[RequireComponent (typeof(MeshRenderer))]
[DisallowMultipleComponent]
public class MeshGeneration : MeshProperty, ITransformable{

	private Vector3 roomBounds;
	[Range(1f, 5f)]
	public float textureRepeat = 1f;
	[Range(0.25f, 5f)]
	public float grid = 1f;
	public bool autoUpdate = false;
	public Material wallMaterial;
	public Material floorMaterial;
	private VariableBounds variableBounds;

	// Use this for initialization
	void Awake () {
		
	}

	public void GenerateMesh(){
		MeshRenderer meshRenderer = GetComponent<MeshRenderer> ();
		GenerateCube ();
		meshRenderer.materials = new Material[] {floorMaterial, wallMaterial, wallMaterial, wallMaterial, wallMaterial, floorMaterial};
	}

	private void GenerateCube(){
		MeshFilter filter = gameObject.GetComponent< MeshFilter >();
		Mesh mesh;

		#if UNITY_EDITOR
		Mesh meshCopy = Mesh.Instantiate(filter.sharedMesh);
		mesh = filter.sharedMesh = meshCopy;
		#else
			if (filter.mesh == null) {
			filter.mesh = new Mesh ();
			}

			mesh = filter.mesh;
		#endif


		mesh.name = "Generated Mesh";
		mesh.Clear();

		#region Vertices
		Vector3 p0 = new Vector3( -roomBounds.x * .5f,	0, roomBounds.z * .5f );
		Vector3 p1 = new Vector3( roomBounds.x * .5f, 	0, roomBounds.z * .5f );
		Vector3 p2 = new Vector3( roomBounds.x * .5f, 	0, -roomBounds.z * .5f );
		Vector3 p3 = new Vector3( -roomBounds.x * .5f,	0, -roomBounds.z * .5f );	

		Vector3 p4 = new Vector3( -roomBounds.x * .5f,	roomBounds.y,  roomBounds.z * .5f );
		Vector3 p5 = new Vector3( roomBounds.x * .5f, 	roomBounds.y,  roomBounds.z * .5f );
		Vector3 p6 = new Vector3( roomBounds.x * .5f, 	roomBounds.y,  -roomBounds.z * .5f );
		Vector3 p7 = new Vector3( -roomBounds.x * .5f,	roomBounds.y,  -roomBounds.z * .5f );

		Vector3[] vertices = new Vector3[]
		{
			// Bottom
			p0, p1, p2, p3,

			// Left
			p7, p4, p0, p3,

			// Front
			p4, p5, p1, p0,

			// Back
			p6, p7, p3, p2,

			// Right
			p5, p6, p2, p1,

			// Top
			p7, p6, p5, p4
		};
		#endregion

		#region Normales
		Vector3 up 	= Vector3.up;
		Vector3 down 	= Vector3.down;
		Vector3 front 	= Vector3.forward;
		Vector3 back 	= Vector3.back;
		Vector3 left 	= Vector3.left;
		Vector3 right 	= Vector3.right;

		Vector3[] normals = new Vector3[]
		{
			// Bottom
			down, down, down, down,

			// Left
			left, left, left, left,

			// Front
			front, front, front, front,

			// Back
			back, back, back, back,

			// Right
			right, right, right, right,

			// Top
			up, up, up, up
		};
		#endregion	

		float wUV = roomBounds.x / textureRepeat;
		float hUV = roomBounds.z / textureRepeat;

		#region Triangles
		List<int[]> triangles = new List<int[]>();
			// Bottom
		triangles.Add(new int[] {3, 1, 0, 3, 2, 1,});		

			// Left
		triangles.Add(new int[] {3 + 4 * 1, 1 + 4 * 1, 0 + 4 * 1,
			3 + 4 * 1, 2 + 4 * 1, 1 + 4 * 1});

			// Front
		triangles.Add(new int[] {3 + 4 * 2, 1 + 4 * 2, 0 + 4 * 2,
			3 + 4 * 2, 2 + 4 * 2, 1 + 4 * 2});

			// Back
		triangles.Add(new int[] {3 + 4 * 3, 1 + 4 * 3, 0 + 4 * 3,
			3 + 4 * 3, 2 + 4 * 3, 1 + 4 * 3});
		
			// Right
		triangles.Add(new int[] {3 + 4 * 4, 1 + 4 * 4, 0 + 4 * 4,
			3 + 4 * 4, 2 + 4 * 4, 1 + 4 * 4});

			// Top
		triangles.Add(new int[] {3 + 4 * 5, 1 + 4 * 5, 0 + 4 * 5,
			3 + 4 * 5, 2 + 4 * 5, 1 + 4 * 5});
		#endregion

		#region UVs
		Vector2 _00 = new Vector2( 0f, 0f );
		Vector2 _10 = new Vector2( wUV, 0f );
		Vector2 _01 = new Vector2( 0f, hUV );
		Vector2 _11 = new Vector2( wUV, hUV );

		Vector2[] uvs = new Vector2[]
		{
			// Bottom
			_11, _01, _00, _10,

			// Left
			_11, _01, _00, _10,

			// Front
			_11, _01, _00, _10,

			// Back
			_11, _01, _00, _10,

			// Right
			_11, _01, _00, _10,

			// Top
			_11, _01, _00, _10,
		};
		#endregion

		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv = uvs;
		mesh.subMeshCount = 6;

		for (int i = 0; i < mesh.subMeshCount; i++) {
			mesh.SetTriangles (triangles [i].Reverse().ToArray(), i);
		}

		mesh.RecalculateBounds();
		mesh.RecalculateNormals ();
		mesh.Optimize();
	}

	public override void Preview(){
		if (variableBounds == null) {
			variableBounds = GetComponent<VariableBounds> ();
		}
		this.roomBounds = variableBounds.Bounds;
		GenerateMesh ();
	}

	public override GameObject[] Generate(){
		this.Preview ();
		return new GameObject[]{ transform.gameObject };
	}

	public void NotifyBoundsChanged(VariableBounds newBounds){
		Preview ();
		SceneUpdater.UpdateScene ();
	}
}
