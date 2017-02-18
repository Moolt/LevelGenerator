using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DoorDefinition{
	public int ID = 0;
	public Vector3 Size;
	public Vector3 Position;
	public Vector3 RelPosition;
	public Vector3 Direction;
	public int CornerIndex;
	public Vector3 Offset;
	public string Name;

	public Vector3 Extends{
		get{
			return Size * 0.5f;
		}
	}
}