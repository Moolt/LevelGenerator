using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GridRow{
	[SerializeField]
	public MaskState[] data;
	public int length;

	public GridRow(int size){
		data = new MaskState[size];
		length = size;
	}

	public MaskState this[int index]{
		get{
			return data [index];
		}
		set{
			data [index] = value;
		}
	}
}