using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class ObjectDocking : TransformingProperty {

	public OffsetType offsetType;
	public float offsetRoomMagnitude;
	public Vector3 offsetRoomMagnitudeVec;
	public int cornerIndex;
	public Vector3 offset;

	public override void Preview(){
		DockingLogic ();
	}

	public override void Generate(){		
		DockingLogic ();
	}

	private void DockingLogic(){
		Vector3[] corners = ParentsAbstractBounds.Corners;

		if (corners.Length > 0) {
			Vector3 corner = corners [cornerIndex];
			float currentSizeMagnitude = ParentsAbstractBounds.Size.magnitude;

			if (offsetType == OffsetType.ABSOLUTE) {
				this.transform.position = corner + offset;
			} else if (offsetType == OffsetType.RELATIVE) {
				this.transform.position = corner + (Vector3.Scale (offset, RelativeOffsetFactor ()));
			} else if (offsetType == OffsetType.RELATIVEUNIFORM) {
				this.transform.position = corner + offset * (ParentsAbstractBounds.Size.magnitude / offsetRoomMagnitude);
			}
		}
	}

	//Called from the EditorScript when the object has been modified / moved
	public void UpdateOffset(){
		if (ParentsAbstractBounds != null) {
			Vector3[] corners = ParentsAbstractBounds.Corners;
			offset = transform.position - corners [cornerIndex];
			offsetRoomMagnitude = ParentsAbstractBounds.Size.magnitude;
			offsetRoomMagnitudeVec = ParentsAbstractBounds.Size;
		}
	}

	private Vector3 RelativeOffsetFactor(){
		Vector3 offsetFactor = Vector3.zero;
		offsetFactor.x = ParentsAbstractBounds.Size.x / offsetRoomMagnitudeVec.x;
		offsetFactor.y = ParentsAbstractBounds.Size.y / offsetRoomMagnitudeVec.y;
		offsetFactor.z = ParentsAbstractBounds.Size.z / offsetRoomMagnitudeVec.z;
		return offsetFactor;
	}

	//Used by Object Arrays to change docking position
	public void AddToOffset(Vector3 additionalOffset){
		offset += additionalOffset;
	}

	//Setter used by editor script, handle
	public int SelectedCornerIndex {
		get {
			return cornerIndex;
		}
		set{
			offset = Vector3.zero;
			cornerIndex = value;
			SceneUpdater.UpdateScene ();
		}
	}
}
