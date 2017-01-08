using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum Direction { XAXIS, YAXIS, ZAXIS }

[DisallowMultipleComponent]
public class LinearArray : InstantiatingProperty {

	public int duplicateCount = 1;
	public bool autoCount;
	public float spacing = 0f;
	public Direction arrayOrientation;

	private MeshFilter meshFilter;
	private AbstractBounds abstractBounds;
	private Vector3 offset;

	private void Preparation(){
		meshFilter = PreviewMesh;

		//AbstractBounds parentBounds = gameObject.GetComponentInParent<AbstractBounds> ();
		abstractBounds = gameObject.GetComponentInParent<AbstractBounds> ();
		offset = abstractBounds.transform.position;
	}

	void OnDrawGizmos(){		

		Vector3[] positions = CalculatePositions (offset);

		transform.position = positions [0];

		if (meshFilter != null) {
			for (int i = 1; i < positions.Length; i++) {
				Gizmos.color = new Color32(0, 0, 0, 50);
				Gizmos.DrawMesh (meshFilter.sharedMesh, positions[i], transform.rotation, transform.localScale);
				Gizmos.color = new Color32(0, 0, 0, 200);
				Gizmos.DrawWireMesh (meshFilter.sharedMesh, positions[i], transform.rotation, transform.localScale);
			}
		}
	}

	public override void Preview(){
		//Not needed, as preview is handled by Gizmos
	}

	public override void Generate(){		
		Vector3[] copyPositions = CalculatePositions (offset);

		transform.position = copyPositions [0];

		for (int i = 1; i < copyPositions.Length; i++) {
			GameObject copy = GameObject.Instantiate (gameObject);
			//Array needs to be removed, since the copies are not under control of the Generator yet to handle removal
			DestroyImmediate(copy.GetComponent<LinearArray> ());
			copy.transform.position = copyPositions [i];
			copy.transform.SetParent (gameObject.transform.parent);
			GeneratedObjects.Add (copy);
		}
	}

	private void HandleDockingOffset(ICollection<GameObject> copies, Vector3 origPos){
		if (gameObject.GetComponent<ObjectDocking> () != null) {			
			foreach(GameObject copy in copies){
				ObjectDocking docking = copy.GetComponent<ObjectDocking> ();
				Vector3 delta = copy.transform.position - origPos;
				docking.AddToOffset (delta);
			}
		}
	}

	private Vector3[] CalculatePositions(Vector3 offset){
		Preparation ();
		Vector3 meshSize = Vector3.Scale (meshFilter.sharedMesh.bounds.size, transform.localScale);
		Vector3 startPosition = transform.position;
		int calculatedCount = duplicateCount;
		float calculatedSpace;
		Vector3 bounds = abstractBounds.Bounds;
		//The positions the original object will stick to the variableBounds (room). Factors in the models size.
		Vector3 boundsOrigin = new Vector3 (bounds.x * -0.5f + meshSize.x / 2f, 0f, bounds.z * -0.5f + meshSize.z / 2f);
		//The right, forward and up vectors, depending on the direction the array should be applied to
		Vector3 orientationVector = OrientationToVec (arrayOrientation);
		//The space that is available to both the original and the copies.
		float availableSpace = (Vector3.Scale(bounds, orientationVector)).magnitude;
		//The models width, height or depth, depending on the orientation
		float modelWidth = Vector3.Scale (meshSize, orientationVector).magnitude;
		availableSpace -= modelWidth;
		//Position of the original, will be fixed on one axis and will therefore stick to a wall
		startPosition = Vector3.Scale(startPosition, (Vector3.one - orientationVector)) + Vector3.Scale(boundsOrigin, orientationVector);
		offset = Vector3.Scale (offset, orientationVector);

		if (autoCount) {
			calculatedCount = (int)Mathf.Floor(availableSpace / (modelWidth + spacing));
		}

		calculatedSpace = availableSpace / calculatedCount;

		Vector3[] positions = new Vector3[calculatedCount + 1];

		positions [0] = startPosition + offset; //position of the original
		for (int i = 1; i < positions.Length; i++) { //positions of the duplicates w/ offset
			positions [i] = (startPosition + ((i * calculatedSpace) * orientationVector)) + offset;
		}

		return positions;
	}

	public Vector3 OrientationToVec(Direction dir){
		switch (dir) {
		case Direction.XAXIS:
			return Vector3.right;
		case Direction.YAXIS:
			return Vector3.up;
		case Direction.ZAXIS:
			return Vector3.forward;
		}
		return Vector3.forward;
	}
}
