using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class VariableBounds : TransformingProperty, IObjectBounds {
	public Vector3 minSize;
	public Vector3 maxSize;
	[HideInInspector]
	public Vector3 size;
	public bool fixedSize;
	[Range(0f, 1f)]
	public float lerp;
	public bool keepAspectRatio;
	public VariableBounds adaptToParent = null;

	public void OnDrawGizmosSelected(){
		Gizmos.color = (fixedSize) ? Color.yellow : Color.white;
		Vector3 pos = transform.position;
		pos.y = minSize.y / 2f;
		Gizmos.DrawWireCube (pos, minSize);
		pos.y = maxSize.y / 2f;
		Gizmos.DrawWireCube (pos, maxSize);
	}
		
	public Vector3 Bounds{ 
		get { return size; } 
		set { this.size = value; } 
	}
		
	public override void Preview(){		
		if (adaptToParent != null) {
			Bounds = adaptToParent.size;
			this.minSize = this.maxSize = adaptToParent.size;
		}
	}

	public override GameObject[] Generate(){
		if (fixedSize) {
			size = maxSize;
		} else {
			RandomizeSize (null); //null because the children dont have to be updated
		}
		if (adaptToParent != null) {
			Bounds = adaptToParent.size;
		}
		return null;
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
		Bounds = randomBounds;
		if (variableObjects != null) {			
			UpdateVariableBoundsDependencies (variableObjects);
		}
	}

	public void UpdateVariableBoundsDependencies(ITransformable[] variableObjects){
		foreach (ITransformable ivb in variableObjects) {
			ivb.NotifyBoundsChanged (this);
		}
	}
}
