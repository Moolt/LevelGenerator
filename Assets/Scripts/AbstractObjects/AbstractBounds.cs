using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class AbstractBounds : TransformingProperty {
	public Vector3 minSize;
	public Vector3 maxSize;
	[HideInInspector]
	public Vector3 size;
	public bool fixedSize;
	[Range(0f, 1f)]
	public float lerp;
	public bool keepAspectRatio;
	public AbstractBounds adaptToParent = null;

	private List<Vector3> corners;

	public override void DrawEditorGizmos(){
		Gizmos.color = (fixedSize) ? Color.yellow : Color.white;
		Vector3 pos = transform.position;
		pos.y = minSize.y / 2f;
		Gizmos.DrawWireCube (pos, minSize);
		pos.y = maxSize.y / 2f;
		Gizmos.DrawWireCube (pos, maxSize);
	}
		
	public override void Preview(){
		if (adaptToParent != null) {
			size = adaptToParent.size;
			this.minSize = this.maxSize = adaptToParent.size;
		}
	}

	public override void Generate(){
		if (fixedSize) {
			size = maxSize;
		} else {
			RandomizeSize (null); //null because the children dont have to be updated
		}
		if (adaptToParent != null) {
			Size = adaptToParent.size;
		}
	}

	//Can be either used within the editor or within the generation process
	//The editor requires child components to be updated, the generation process doesn't
	public void RandomizeSize(ITransformable[] variableObjects){
		Vector3 randomBounds;
		if (keepAspectRatio) {
			randomBounds = Vector3.Lerp (minSize, maxSize, Random.Range (0f, 1f));
		} else {
			float randomX = Mathf.Lerp(minSize.x, maxSize.x, Random.Range(0f, 1f));
			float randomY = Mathf.Lerp(minSize.y, maxSize.y, Random.Range(0f, 1f));
			float randomZ = Mathf.Lerp(minSize.z, maxSize.z, Random.Range(0f, 1f));
			randomBounds = new Vector3 (randomX, randomY, randomZ);
		}
		Size = randomBounds;
		if (variableObjects != null) {			
			UpdateVariableBoundsDependencies (variableObjects);
		}
	}

	public void UpdateVariableBoundsDependencies(ITransformable[] variableObjects){
		foreach (ITransformable ivb in variableObjects) {
			ivb.NotifyBoundsChanged (this);
		}
	}

	public Vector3 Size{ 
		get { return size; } 
		set { 
			this.size = value;
		}
	}

	public Vector3 Extends{ 
		get { return size * .5f; }
	}

	public Vector3 Center{
		get{
			return transform.position + new Vector3(0f, size.y / 2f, 0f);
		}
	}

	//Calculates 27 corners which fully define the bounds
	//Used by docking component
	public Vector3[] Corners {
		get {
			//Only recalculate if the bounds changed since the last time
			corners = new List<Vector3> ();

			Vector3 point = new Vector3 (Extends.x,  Extends.y, Extends.z);

			//7  8  9 -> 16  17  18 -> 25 26 27
			//4  5  6 -> 13  14  15 -> 22 23 24
			//1  2  3 -> 10  11  12 -> 19 20 21
			for (int i = 0; i < 3; i++) {
				for (int j = 0; j < 3; j++) {
					for (int k = 0; k < 3; k++) {
						corners.Add (new Vector3 (point.x * (k - 1), point.y * i, point.z * (j - 1)) + transform.position);
					}
				}
			}

			return corners.ToArray ();
		}
	}

	public int[] CornerIndicesByDirection(Vector3 direction){
		if (direction == Vector3.right) {
			return new int[] { 8, 5, 2, 17, 14, 11, 26, 23, 20 };
		} else if (direction == Vector3.left) {
			return new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24 };
		} else if (direction == Vector3.forward) {
			return new int[] { 6, 7, 8, 15, 16, 17, 24, 25, 26 };
		} else if (direction == Vector3.back) {
			return new int[] { 2, 1, 0, 11, 10, 9, 20, 19, 18 };
		}
		return new int[0];
	}

	public override bool DelayRemoval{
		get { return true; }
	}
}
