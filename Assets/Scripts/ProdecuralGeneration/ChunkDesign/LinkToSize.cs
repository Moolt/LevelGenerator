using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SizeComparison{ SMALLER, LARGER }

public class LinkToSize : ConditionalProperty {

	public float width = 1f;
	public float height = 1f;
	public float depth = 1f;
	public SizeComparison deleteIfChunkIs;

	public override void Generate(){
		SetMeshVisibility (IsConditionTrue);
		if (IsConditionTrue) {
			Remove ();
		}
	}

	public override void Preview(){
		SetMeshVisibility (IsConditionTrue);
	}

	public override void DrawEditorGizmos (){
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube (Vector3.up * (height / 2f), new Vector3 (width, height, depth));
		SceneUpdater.UpdateScene ();
	}

	private void SetMeshVisibility(bool visibility){
		foreach (MeshFilter mf in PreviewMeshes) {
			MeshRenderer mr = mf.gameObject.GetComponent<MeshRenderer> ();
			if (mr != null) {
				mr.enabled = !visibility;
			}
		}
	}

	private bool IsConditionTrue{
		get{
			Vector3 p = ParentsAbstractBounds.Size;
			return (p.x > width && p.y > height && p.z > depth) && deleteIfChunkIs == SizeComparison.LARGER ||
				(p.x < width && p.y < height && p.z < depth) && deleteIfChunkIs == SizeComparison.SMALLER;
		}
	}
}

