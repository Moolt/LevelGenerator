﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LinearArray : MultiplyingProperty {

	public Vector3 orientation = Vector3.right;
	public int duplicateCount = 1;
	public bool closeGap = true;
	public float spacing = 0f; //Space between objects
	public bool autoCount; //If set to true, as many objects as fit are created

	//Used for both GizmoPreview and position calculation, as the size of the mesh is considered
	private MeshFilter meshFilter;
	//private Vector3 offset;

	private void Preparation(){
		meshFilter = PreviewMesh;
	}

	public override void DrawEditorGizmos(){		
		Vector3[] positions = CalculatePositions ();

		transform.position = positions [0];

		if (MeshFound ()) {
			for (int i = 1; i < positions.Length; i++) {
				Gizmos.color = Color.black;
				Gizmos.DrawWireMesh (meshFilter.sharedMesh, positions [i], transform.rotation, transform.localScale);
			}
		} else {
			for (int i = 0; i < positions.Length; i++) {
				Gizmos.color = Color.black;
				Gizmos.DrawWireCube (positions [i], new Vector3 (1f, 1f, 1f));
			}
		}
	}

	public override void Preview(){
		//Not needed, as preview is handled by Gizmos
	}

	protected override Vector3[] CalculatePositions(){
		Preparation ();
		Vector3 meshFilterSize = (MeshFound()) ? meshFilter.sharedMesh.bounds.size : Vector3.one;	
		Vector3 meshSize = Vector3.Scale (meshFilterSize, transform.localScale);
		Vector3 startPosition = transform.position;
		int calculatedCount = duplicateCount;
		float calculatedSpace;
		Vector3 bounds = AbstractBounds.Size;
		//The positions the original object will stick to the variableBounds (room). Factors in the models size.
		Vector3 boundsOrigin = new Vector3 (bounds.x * -0.5f + meshSize.x / 2f, meshSize.y / 2f, bounds.z * -0.5f + meshSize.z / 2f) + AbstractBounds.transform.position;
		//The right, forward and up vectors, depending on the direction the array should be applied to
		//Vector3 orientationVector = OrientationToVec (arrayOrientation);
		//The space that is available to both the original and the copies.
		float availableSpace = (Vector3.Scale(bounds, orientation)).magnitude;
		//The models width, height or depth, depending on the orientation
		float modelWidth = Vector3.Scale (meshSize, orientation).magnitude;
		availableSpace -= modelWidth;
		//Position of the original, will be fixed on one axis and will therefore stick to a wall
		startPosition = Vector3.Scale(startPosition, (Vector3.one - orientation)) + Vector3.Scale(boundsOrigin, orientation);
		//offset = Vector3.Scale (offset, orientationVector);

		if (autoCount) {
			float actualSpacing = (closeGap) ? 0f : spacing;
			calculatedCount = (int)Mathf.Floor(availableSpace / (modelWidth + actualSpacing));
		}

		calculatedSpace = availableSpace / calculatedCount;

		Vector3[] positions = new Vector3[calculatedCount + 1];

		if (positions.Length > 0) {
			positions [0] = startPosition; //position of the original
			for (int i = 1; i < positions.Length; i++) { //positions of the duplicates w/ offset
				if (closeGap) {
					positions [i] = (startPosition + (i * modelWidth) * orientation);
				} else {
					positions [i] = (startPosition + ((i * calculatedSpace) * orientation));
				}
			}
		}

		return positions;
	}
}