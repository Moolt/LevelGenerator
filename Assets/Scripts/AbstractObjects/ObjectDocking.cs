using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum DockingPosition { TOPLEFT, TOPRIGHT, BOTTOMLEFT, BOTTOMRIGHT, MID }
public enum CoordinateType { ABSOLUTE, PERCENTAGE }

[DisallowMultipleComponent]
public class ObjectDocking : MonoBehaviour, IAbstractAsset {

	public DockingPosition dockingPosition;
	public CoordinateType interpolationMethod;
	[HideInInspector]
	public Vector3 offset;
	private VariableBounds variableBounds;
	private Dictionary<DockingPosition, Vector3> cornerMapping;
	private float offsetRoomMagnitude;

	public void Preview(){
		variableBounds = GameObject.FindWithTag ("Chunk").GetComponent<VariableBounds> ();
		Vector3 meshBounds = variableBounds.GetBounds ();
		Vector3 newPos = new Vector3 ();
		float oldY = transform.position.y;

		CalcCorners (meshBounds);

		if (interpolationMethod == CoordinateType.ABSOLUTE) {
			this.transform.position = cornerMapping [dockingPosition] + offset;
		} else {
			this.transform.position = cornerMapping [dockingPosition] + (offset * (meshBounds.magnitude / offsetRoomMagnitude));
		}
	}

	public void CalcOffset(){
		variableBounds = GameObject.FindWithTag ("Chunk").GetComponent<VariableBounds> ();
		Vector3 meshBounds = variableBounds.GetBounds ();

		CalcCorners (meshBounds);
		offset = transform.position - cornerMapping [dockingPosition];
		offsetRoomMagnitude = meshBounds.magnitude;
	}

	private void CalcCorners(Vector3 meshBounds){
		float oldY = transform.position.y;
		cornerMapping = new Dictionary<DockingPosition, Vector3> ();
		cornerMapping.Add(DockingPosition.BOTTOMLEFT, new Vector3 (-meshBounds.x / 2f, oldY, -meshBounds.z / 2f));
		cornerMapping.Add(DockingPosition.BOTTOMRIGHT, new Vector3 (meshBounds.x / 2f, oldY, -meshBounds.z / 2f));
		cornerMapping.Add(DockingPosition.TOPLEFT, new Vector3 (-meshBounds.x / 2f, oldY, meshBounds.z / 2f));
		cornerMapping.Add(DockingPosition.TOPRIGHT, new Vector3 (meshBounds.x / 2f, oldY, meshBounds.z / 2f));
		cornerMapping.Add(DockingPosition.MID, new Vector3 (0f, oldY, 0f));
	}

	public void Generate(){
	}
}
