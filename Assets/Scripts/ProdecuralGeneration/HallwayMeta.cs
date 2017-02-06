using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HallwayMeta{
	public Vector3 Start;
	public Vector3 End;
	public float Size;
	public GameObject StartChunk;
	public GameObject EndChunk;
	public bool IsCriticalPath;

	public HallwayMeta (Vector3 start, Vector3 end, GameObject startChunk, GameObject endChunk, bool isCriticalPath){
		this.Start = start;
		this.End = end;
		this.StartChunk = startChunk;
		this.EndChunk = endChunk;
		this.IsCriticalPath = isCriticalPath;
	}
}