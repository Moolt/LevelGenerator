using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Used by Abstract Bounds to store which axes the AB stretches on
[System.Serializable]
public class StretchInfo{

	public StretchInfo(bool active, Vector3 direction, bool center, float percent, string label){
		this.Active = active;
		this.Direction = direction;
		this.IsCenter = center;
		this.Percent = percent;
		this.Label = label;
	}

	public bool Active;
	public Vector3 Direction;
	public bool IsCenter;
	public float Percent;
	public string Label;
}
