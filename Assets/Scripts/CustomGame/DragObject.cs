using UnityEngine;
using System.Collections;

public class DragObject : MonoBehaviour {

	public float grabDistance = 10.0f;
	public float holdDistance = 2.0f;
	public float throwStrength = 2.5f;
	public float tossStrength = 2.5f;
	public bool isDragging = false;

	private int dragLayer = 8;
	private int defaultLayer = 0;
	private int dragLayerMask;
	private int defaultLayerMask;
	private GameObject dragObject;
	private float objectHRotation;

	private Vector3 objectInitialRotation;
	private Vector3 prevPlayerDir;

	// Use this for initialization
	void Start () {
		dragLayerMask = 1 << dragLayer;
		defaultLayerMask = 1 << defaultLayer;
	}
	
	// Update is called once per frame
	void Update () {
		Vector3 forward = transform.TransformDirection(Vector3.forward);
		RaycastHit objectHit;

		if(isDragging && Input.GetKeyDown(KeyCode.E)){
			
			isDragging = false;
			dragObject.GetComponent<Rigidbody>().isKinematic = false;
			float hThrow = Mathf.Max(.0f, Input.GetAxis("Mouse X") - .5f);
			float vThrow = Mathf.Max(.0f, Input.GetAxis("Mouse Y") - .5f);
			
			Vector3 throwForceVector = (transform.TransformDirection(Vector3.right)*hThrow + Vector3.up * vThrow).normalized;
			dragObject.GetComponent<Rigidbody>().AddForce(throwForceVector * throwStrength, ForceMode.Impulse);

		} else if (Physics.Raycast (transform.position, forward, out objectHit, grabDistance, dragLayerMask)) {			
			if(!isDragging && Input.GetKeyDown(KeyCode.E)){
				isDragging = true;
				dragObject = objectHit.transform.gameObject;
				prevPlayerDir = transform.rotation.eulerAngles;
				dragObject.GetComponent<Rigidbody>().isKinematic = true;
			}
		}

		if(isDragging){
			float availableSpace = GetLengthToCollision(); //space between player and next obstacle			
			float colliderSize = dragObject.GetComponent<Collider>().bounds.extents.x;

			dragObject.transform.position = forward * Mathf.Min(holdDistance + colliderSize, availableSpace - colliderSize) + transform.position;

			float colliderDistance = (dragObject.transform.position - transform.position).magnitude - colliderSize;

			//rather drop the object than getting stuck in it - there may be a better option
			if (colliderDistance < 0.5f || Vector3.Angle(Vector3.down, forward.normalized) < 30){
				DropObject();
			}

			//transform.rotation.eulerAngles - prevPlayerDir;
			Vector3 newRotation = new Vector3 (.0f, transform.rotation.eulerAngles.y - prevPlayerDir.y, .0f);

			dragObject.transform.Rotate (newRotation, Space.World);
			prevPlayerDir = transform.rotation.eulerAngles;
			//dragObject.transform.Rotate(new Vector3(.0f, Input.GetAxis("Mouse ScrollWheel") * 90.0f, .0f), Space.World);
			if (Input.GetMouseButton (2)) {
				dragObject.transform.Rotate (new Vector3 (0f, Input.GetAxis ("Mouse X") * -5), Space.World);
				//edit, vertical rotation needed
				dragObject.transform.Rotate (Input.GetAxis ("Mouse Y") * 5 * transform.TransformDirection(Vector3.right).normalized);
			}
		}

		//throw the object in the direction the player is facing
		if (isDragging && Input.GetMouseButtonDown (0)) {
			DropObject();
			dragObject.GetComponent<Rigidbody>().AddForce(transform.TransformDirection(Vector3.forward)*tossStrength, ForceMode.Impulse);
		}
	}

	//reapplies physics
	private void DropObject(){
		isDragging = false;
		dragObject.GetComponent<Rigidbody>().isKinematic = false;
	}

	private float GetLengthToCollision(){
		Vector3 forward = transform.TransformDirection (Vector3.forward);
		RaycastHit obstacleHit;
		//Collider collider = dragObject.GetComponent<Collider>();


		Physics.Raycast (transform.position, forward, out obstacleHit, 50, defaultLayerMask);
		/*Ray ray = new Ray (collider.transform.position, obstacleHit.normal * -1);
		ray.origin = ray.GetPoint (10.0f);
		ray.direction = -ray.direction;
		collider.Raycast (ray, out colliderHit, 10.0f);

		Debug.Log (colliderHit.point);
		
		//Debug.DrawRay (collider.transform.position, obstacleHit.normal * -2, Color.white);
		Debug.DrawRay (transform.position, colliderHit.point, Color.white);*/
		//collider.Raycast(new Ray(
		return (obstacleHit.distance == 0) ? 10000 : obstacleHit.distance;
	}

	public bool IsDragging
	{
		get{ return isDragging; }
	}
}
