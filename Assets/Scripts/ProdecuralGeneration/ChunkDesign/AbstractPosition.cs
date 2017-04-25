using UnityEngine;
using System.Collections;

[RequireComponent (typeof(Collider))]
public class AbstractPosition : TransformingProperty {
	public Vector3 direction = Vector3.right;
	public float minValue = 5f;
	public float maxValue = 5f;

	public bool useAvailableSpace;
	public bool useRaycast;

	private Collider attachedCollider;
	private Vector3 fromVec;
	private Vector3 toVec;

	private void Logic(){
		attachedCollider = GetComponent<Collider> ();
		Vector3 center = attachedCollider.bounds.center - transform.position;

		fromVec = transform.position - Vector3.Scale (Vector3.one * minValue, direction);
		toVec = transform.position + Vector3.Scale (Vector3.one * maxValue, direction);

		if (useRaycast) {
			Vector3 minHit = RaycastPoint (transform.position - center, direction * -1);
			Vector3 maxHit = RaycastPoint (transform.position - center, direction);

			minHit += Vector3.Scale (attachedCollider.bounds.extents, direction);
			maxHit -= Vector3.Scale (attachedCollider.bounds.extents, direction);

			if (useAvailableSpace) {
				fromVec = minHit;
				toVec = maxHit;
			} else {
				fromVec = (DirMag(fromVec) < DirMag(minHit)) ? minHit : fromVec;
				toVec = (DirMag(toVec) > DirMag(maxHit)) ? maxHit : toVec;
			}
		}
	}

	public override void DrawEditorGizmos(){
		Logic ();
		Gizmos.color = Color.cyan;
		Gizmos.DrawLine (fromVec, toVec);
		Gizmos.DrawCube (fromVec, Vector3.one * 0.6f);
		Gizmos.DrawCube (toVec, Vector3.one * 0.6f);
	}

	public override void Preview(){

	}

	public override void Generate(){
		Logic ();
		Vector3 newPos = Vector3.Lerp (fromVec, toVec, Random.value);
		transform.position = newPos;
	}

	private Vector3 RaycastPoint(Vector3 origin, Vector3 dir){
		RaycastHit hit;
		Ray ray = new Ray (origin, dir);
		Physics.Raycast (ray, out hit);
		return hit.point;
	}

	private float DirMag(Vector3 vec){
		Vector3 scaled = Vector3.Scale (vec, direction);
		return scaled.x + scaled.y + scaled.z;
	}
}
