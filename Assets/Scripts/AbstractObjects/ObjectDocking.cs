using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum CoordinateType { ABSOLUTE, PERCENTAGE }

[DisallowMultipleComponent]
public class ObjectDocking : TransformingProperty {

	public CoordinateType interpolationMethod;

	[HideInInspector]
	public float offsetRoomMagnitude;
	[HideInInspector]
	public int cornerIndex;
	[HideInInspector]
	public Vector3 offset;

	public override void Preview(){
		DockingLogic ();
	}

	public override GameObject[] Generate(){
		DockingLogic ();
		return null;
	}

	private void DockingLogic(){
		Vector3[] corners = AbstractBounds.Corners;

		Vector3 corner = corners [cornerIndex];
		float currentSizeMagnitude = AbstractBounds.Bounds.magnitude;

		if (interpolationMethod == CoordinateType.ABSOLUTE) {
			this.transform.position = corners [cornerIndex] + offset;
		} else {
			this.transform.position = corners [cornerIndex]+ (offset * (currentSizeMagnitude / offsetRoomMagnitude));
		}
	}

	//Called from the EditorScript when the object has been modified / moved
	public void UpdateOffset(){
		Vector3[] corners = AbstractBounds.Corners;
		offset = transform.position - corners [cornerIndex];
		offsetRoomMagnitude = AbstractBounds.Bounds.magnitude;
	}

	public int SelectedCornerIndex {
		get {
			return cornerIndex;
		}
		set{
			SceneUpdater.UpdateScene ();
			offset = Vector3.zero;
			cornerIndex = value;
		}
	}
}
