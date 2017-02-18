using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
		}

		meshFilter.sharedMesh.name = "Procedural Mesh";
		mesh = meshFilter.sharedMesh;

		if (Application.isEditor) {
			mesh.MarkDynamic();
		}
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
		AbstractBoundsBinding ();
		AssignMesh ();
		UpdateCollider ();
	}

	public override void Generate(){
		Init ();
		AbstractBoundsBinding ();
		AssignMesh ();
		UpdateCollider ();
	}

	private void UpdateCollider(){
		Collider _col = GetComponent<Collider> () as Collider;
		if (_col != null && meshFilter.sharedMesh != null) {
			if (_col is MeshCollider) {
				(_col as MeshCollider).sharedMesh = meshFilter.sharedMesh;
				//(_col as MeshCollider).convex = true;
				//(_col as MeshCollider).inflateMesh = true;
			} else if (_col is BoxCollider) {
				(_col as BoxCollider).size = extends * 2f;
			}
		}
	}

	public void BreakSharedMeshLink(){
		meshFilter.sharedMesh = new Mesh ();
		mesh = meshFilter.sharedMesh;
	}

	public void AbstractBoundsBinding(){
		if (AbstractBounds != null && AbstractBounds.gameObject == transform.gameObject) {
			this.extends = AbstractBounds.Extends;
		}
	}

	public bool HasAbstractBounds{
		get{
			return GetComponent<AbstractBounds> () != null;
		}
	}
}
