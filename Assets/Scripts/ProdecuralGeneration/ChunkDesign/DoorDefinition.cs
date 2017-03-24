using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DoorDefinition{
	public static float GlobalSize = 2.3f;
	public int ID = 0; //For identification
	//public Vector3 Size; //Unused, use GlobalSize instead
	public Vector3 Position; //Acutal position
	public Vector3 WorkingPosition; //For previewing
	public Vector3 RelPosition; //Relative position in room
	public Vector3 Direction; //Opposite direction of the wall it's attached to
	public int CornerIndex; //Docking Corner Index
	public Vector3 Offset; //Distance from Corner
	public string Name; //Only used for GUI yet

	public Vector3 Extends{
		get{
			return Vector3.one * GlobalSize * 0.5f;
		}
	}
}