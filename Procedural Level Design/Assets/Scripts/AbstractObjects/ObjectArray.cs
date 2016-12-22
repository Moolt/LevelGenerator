using UnityEngine;
using System.Collections;

public enum Direction { HORIZONTAL, VERTICAL, HEIGTH }

[DisallowMultipleComponent]
public class ObjectArray : MonoBehaviour, IAbstractAsset {

	[Range(1, 10)]
	public int duplicateCount = 1;
	public bool autoCount;
	public bool margin = false;
	public Direction arrayOrientation;

	private MeshFilter meshFilter;
	private VariableBounds variableBounds;
	private Vector3 offset;

	private void Preparation(){
		if (meshFilter == null) {
			meshFilter = GetComponent<MeshFilter> ();
		}

		GameObject searchObject = gameObject;
		while (searchObject.GetComponent<VariableBounds> () == null) {
			searchObject = searchObject.transform.parent.gameObject;
		}
		variableBounds = searchObject.GetComponent<VariableBounds> ();
		offset = searchObject.transform.position;
	}

	void OnDrawGizmos(){
		Preparation ();
		Vector3 meshSize = Vector3.Scale (meshFilter.sharedMesh.bounds.size, transform.localScale);
		Vector3[] positions = CalculatePositions (meshSize, offset);

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

	public void Preview(){
		
	}

	public void Generate(){
		/*Preparation ();
		Vector3 meshSize = Vector3.Scale (meshFilter.sharedMesh.bounds.size, transform.localScale);
		Vector3[] positions = CalculatePositions (meshSize, offset);

		transform.position = positions [0];

		if (meshFilter != null) {
			for (int i = 0; i < positions.Length; i++) {
				GameObject copy = GameObject.Instantiate (gameObject);
				copy.transform.position = positions [i];
			}
		}*/
	}

	private Vector3[] CalculatePositions(Vector3 modelSize, Vector3 offset){
		Vector3 startPosition = transform.position;
		int calculatedCount = duplicateCount;
		float calculatedSpace;
		Preparation ();
		Vector3 bounds = variableBounds.GetBounds ();
		//The positions the original object will stick to the variableBounds (room). Factors in the models size.
		Vector3 boundsOrigin = new Vector3 (bounds.x * -0.5f + modelSize.x / 2f, 0f, bounds.z * -0.5f + modelSize.z / 2f);
		//The right, forward and up vectors, depending on the direction the array should be applied to
		Vector3 orientationVector = OrientationToVec (arrayOrientation);
		//The space that is available to both the original and the copies.
		float availableSpace = (Vector3.Scale(bounds, orientationVector)).magnitude;
		//The models width, height or depth, depending on the orientation
		float modelWidth = Vector3.Scale (modelSize, orientationVector).magnitude;
		availableSpace -= modelWidth;
		//Position of the original, will be fixed on one axis and will therefore stick to a wall
		startPosition = Vector3.Scale(startPosition, (Vector3.one - orientationVector)) + Vector3.Scale(boundsOrigin, orientationVector);
		offset = Vector3.Scale (offset, orientationVector);

		if (autoCount) {
			calculatedCount = (int)Mathf.Floor(availableSpace / modelWidth);
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
		case Direction.HORIZONTAL:
			return Vector3.right;
		case Direction.VERTICAL:
			return Vector3.forward;
		case Direction.HEIGTH:
			return Vector3.up;
		}
		return Vector3.forward;
	}
}
