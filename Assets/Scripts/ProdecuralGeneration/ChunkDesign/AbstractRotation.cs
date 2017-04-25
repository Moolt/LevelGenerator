using UnityEngine;
using System.Collections;
using System;

public class AbstractRotation : TransformingProperty {

	public bool useX = true;
	public bool useY = true;
	public bool useZ = true;

	public int roundBy = 1;

	public override void Preview(){

	}

	public override void Generate(){
		Vector3 rotationVector = new Vector3 (Convert.ToInt32(useX), Convert.ToInt32(useY), Convert.ToInt32(useZ));
		float rotationVal = UnityEngine.Random.value * 360;
		rotationVal = Mathf.Max (0f, rotationVal - rotationVal % roundBy);
		rotationVector = rotationVector * rotationVal;
		transform.Rotate (rotationVector);
	}
}
