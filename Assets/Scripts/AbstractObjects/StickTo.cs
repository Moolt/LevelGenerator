using UnityEngine;
using System.Collections;

public class StickTo : TransformingProperty {
	public Vector3 objectPosition;
	public Vector3 stickDirection;
	public bool updateInEditor = false;
	public float distance = 0f;
	public RaycastHit hit;

	private Vector3 objectPositionWithoutDistance;
	private Collider attachedCollider;
	private Vector3 origin;

	public override void DrawEditorGizmos (){
		Logic ();

		if (hit.collider != null && attachedCollider != null) {
			Gizmos.color = Color.cyan;
			Gizmos.DrawLine (origin, hit.point);
			Gizmos.DrawSphere (hit.point - (Vector3.Scale (Vector3.one * 0.5f, stickDirection)), 0.8f);
		}

		Gizmos.color = Color.red;
		Gizmos.DrawSphere(objectPosition , 0.8f);
		Gizmos.color = new Color(1f, 0f, 0f, .5f);
		if (MeshFound()) {
			Gizmos.DrawWireMesh (PreviewMesh.sharedMesh, objectPosition, Quaternion.identity, transform.localScale);
		}

		if (updateInEditor) {
			transform.position = objectPosition;
		}
	}

	public override void Preview(){

	}

	public override void Generate(){
		Logic ();
		transform.position = objectPosition;
	}

	private Vector3 Logic(){
		attachedCollider = (Collider) GetComponentInChildren<Collider> ();

		if (attachedCollider != null) {
			//Collider itselt only stores the center + position in world coordinates
			//Calculate the center to substract it from the final position
			//The origin is 0,0,0 - if the center is any other than the origin, the offset will be removed
			Vector3 center = attachedCollider.bounds.center - transform.position;

			origin = attachedCollider.bounds.center - Vector3.Scale (attachedCollider.bounds.size, stickDirection) * 0.40f;

			Ray ray = new Ray (origin, stickDirection);
			Physics.Raycast (ray, out hit);

			if (hit.collider != null) {
				objectPositionWithoutDistance = hit.point - Vector3.Scale (attachedCollider.bounds.size, stickDirection * 0.5f);
				objectPosition = objectPositionWithoutDistance - Vector3.Scale (Vector3.one * distance, stickDirection) - center;
				return objectPosition;
			} 
		}
		objectPosition = transform.position;
		return objectPosition;
	}

	public float MaxDistance{
		get{
			if (attachedCollider != null) {
				return (objectPositionWithoutDistance - attachedCollider.bounds.center).magnitude;
			} else {
				return 0f;
			}
		}
	}
		
	public override int ExecutionOrder{
		get { return 5; }
	}
}
