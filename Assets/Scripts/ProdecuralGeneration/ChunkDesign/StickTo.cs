using UnityEngine;
using System.Collections;

public class StickTo : TransformingProperty {
	public Vector3 objectPosition;
	public Vector3 stickDirection;
	public float tolerance = 1f;
	public float distance = 0f;
	public RaycastHit hit;

	private Vector3 objectPositionWithoutDistance;
	private bool applyTriggered = false;
	private Collider attachedCollider;
	private Vector3 origin;

	public override void DrawEditorGizmos (){
		CalculateParameters ();

		if (hit.collider != null && attachedCollider != null) {
			Gizmos.color = Color.cyan;
			Gizmos.DrawLine (origin, hit.point);
			Gizmos.color = Color.red;
			Gizmos.DrawSphere (hit.point - (Vector3.Scale (Vector3.one * 0.125f, stickDirection)), 0.125f);
		}

		Gizmos.color = Color.green;
		Gizmos.DrawSphere(objectPosition , 0.125f);
		Gizmos.color = Color.yellow;
		Gizmos.DrawSphere(origin , 0.125f);
		Gizmos.color = new Color(1f, 0f, 0f, .5f);
		if (MeshFound() && hit.collider != null) {
			MeshFilter[] meshes = PreviewMeshes;
			foreach (MeshFilter meshFilter in meshes) {
				Gizmos.DrawWireMesh (meshFilter.sharedMesh, objectPosition + (meshFilter.transform.position - transform.position),  transform.rotation, FindAbsoluteScale(meshFilter.transform));
			}
		}

		if (applyTriggered) {
			transform.position = objectPosition;
			applyTriggered = false;
		}
	}

	public override void Preview(){

	}

	public override void Generate(){
		CalculateParameters ();
		transform.position = objectPosition;
	}

	public void Apply(){
		applyTriggered = true;
	}

	private Vector3 CalculateParameters(){
		attachedCollider = (Collider) GetComponentInChildren<Collider> ();

		if (attachedCollider != null) {
			//Collider itselt only stores the center + position in world coordinates
			//Calculate the center to substract it from the final position
			//The origin is 0,0,0 - if the center is any other than the origin, the offset will be removed
			Vector3 center = attachedCollider.bounds.center - transform.position;

			origin = attachedCollider.bounds.center - Vector3.Scale (attachedCollider.bounds.size, stickDirection) * (-0.48f + tolerance);

			Ray ray = new Ray (origin, stickDirection);
			Physics.Raycast (ray, out hit);

			if (hit.collider != null) {
				objectPositionWithoutDistance = hit.point - Vector3.Scale (attachedCollider.bounds.size, stickDirection * 0.5f);
				objectPosition = objectPositionWithoutDistance - Vector3.Scale (Vector3.one * distance, stickDirection) - center;
				//objectRotation = (applyDirection) ? hit.normal : transform.eulerAngles;

				if (attachedCollider == hit.collider) {
					objectPosition = transform.position;
				}

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
		
	public override float ExecutionOrder{
		get { return 5; }
	}
}
