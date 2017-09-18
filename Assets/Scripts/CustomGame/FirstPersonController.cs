using System;
using UnityEngine;
using System.Collections;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using UnityStandardAssets.Characters.FirstPerson;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour {
	
	public float walkSpeed;
	public bool airMove;
	public float runSpeed;
	public float jumpSpeed;
	public float stickToGroundForce;
	public float gravityMultiplier;
	public MouseLook mouseLook;

	private bool isWalking = false;
	private Camera mainCamera;
	private bool jumpTriggered;
	private float yRotation;
	private Vector2 input;
	private Vector3 moveDirection = Vector3.zero;
	private CharacterController characterController;
	private CollisionFlags collisionFlags;
	private bool previouslyGrounded;
	private Vector3 previousMovement;
	//private Vector3 originalCameraPosition;
	private bool jumping;
	//private DragObject dragObjectComponent;

	void Start () {
		Cursor.lockState = CursorLockMode.Locked;
		characterController = GetComponent<CharacterController> ();
		mainCamera = Camera.main;
		//originalCameraPosition = mainCamera.transform.localPosition;
		jumping = false;
		mouseLook.Init (transform, mainCamera.transform);
		//dragObjectComponent = GetComponentInChildren<DragObject> ();
	}
	
	// Update is called once per frame
	private void Update () {
		mouseLook.LookRotation (transform, mainCamera.transform);
		/*if (!(Input.GetMouseButton (2) && dragObjectComponent.IsDragging)) {
		}*/

		//jump state is read here
		if (!jumpTriggered && !jumping) {
			jumpTriggered = CrossPlatformInputManager.GetButtonDown("Jump");
		}

		//landing
		if (!previouslyGrounded && characterController.isGrounded) {
			moveDirection.y = 0f;
			jumping = false;
		}

		if (!characterController.isGrounded && !jumping && previouslyGrounded) {
			moveDirection.y = 0f;
		}

		previouslyGrounded = characterController.isGrounded;
	}

	private void FixedUpdate(){	
		float speed;
		GetControlsInput (out speed);
		// always move along the camera forward as it is the direction that it being aimed at
		Vector3 desiredMove = transform.forward * input.y + transform.right * input.x;

		RaycastHit hitInfo;
		Physics.Raycast (transform.position, Vector3.down, out hitInfo, characterController.height / 2.0f);
		//makes the character move on uneven surfaces, too
		desiredMove = Vector3.ProjectOnPlane (desiredMove, hitInfo.normal).normalized;

		moveDirection.x = desiredMove.x * speed;
		moveDirection.z = desiredMove.z * speed;

		//can the player change his direction when in mid air?
		if (!airMove) {
			if (characterController.isGrounded) {
				previousMovement.x = moveDirection.x;
				previousMovement.z = moveDirection.z;
			} else {
				moveDirection.x = previousMovement.x;
				moveDirection.z = previousMovement.z;
			}
		}

		if (characterController.isGrounded) {
			moveDirection.y = -stickToGroundForce;

			if (jumpTriggered) {
				moveDirection.y = jumpSpeed;
				jumpTriggered = false;
				jumping = true;
			}
		} else {
			moveDirection += Physics.gravity * gravityMultiplier * Time.fixedDeltaTime;
		}
		collisionFlags = characterController.Move (moveDirection * Time.fixedDeltaTime);
	}

	private void GetControlsInput(out float speed){
		float horizontal = CrossPlatformInputManager.GetAxis ("Horizontal");
		float vertical = CrossPlatformInputManager.GetAxis ("Vertical");

		isWalking = !Input.GetKey (KeyCode.LeftShift);

		//differantiate between running and walking
		speed = isWalking ? walkSpeed : runSpeed;
		input = new Vector2 (horizontal, vertical);

		if (input.sqrMagnitude > 1) {
			input.Normalize();
		}
	}

	private void OnControllerColliderHit(ControllerColliderHit hit){
		Rigidbody body = hit.collider.attachedRigidbody;

		if (collisionFlags == CollisionFlags.Above) {
			moveDirection.y = .0f;
		}

		if (collisionFlags == CollisionFlags.Below) {
			return;
		}

		if (body == null || body.isKinematic) {
			return;
		}

		//body.AddForceAtPosition (characterController.velocity * 0.1f, hit.point, ForceMode.Impulse);
	}

	public bool IsWalking {
		get {
			return previousMovement.magnitude > 0f;
		}
	}

	public bool JumpTriggered {
		get {
			return this.jumpTriggered;
		}
	}
}
