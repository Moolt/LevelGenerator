using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class VariableBounds : TransformingProperty {
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
		
	public void SetBounds(Vector3 size){
		this.size = size;
	}

	public Vector3 GetBounds(){
		return size;
	}

	public override void Preview(){
		if (adaptToParent != null) {
			SetBounds (adaptToParent.size);
			this.minSize = this.maxSize = adaptToParent.size;
		}
	}

	public override GameObject[] Generate(){
		IVariableBounds[] children = gameObject.GetComponents<IVariableBounds> ();
		RandomizeSize (children);
		return null;
	}

	public void RandomizeSize(IVariableBounds[] variableObjects){
		Vector3 randomBounds;
		if (keepAspectRatio) {
			randomBounds = Vector3.Lerp (minSize, maxSize, Random.Range (0f, 1f));
		} else {
			float randomX = Mathf.Lerp(minSize.x, maxSize.x, Random.Range(0f, 1f));
			float randomY = Mathf.Lerp(minSize.y, maxSize.y, Random.Range(0f, 1f));
			float randomZ = Mathf.Lerp(minSize.z, maxSize.z, Random.Range(0f, 1f));
			randomBounds = new Vector3 (randomX, randomY, randomZ);
		}
		SetBounds (randomBounds);
		UpdateVariableBoundsDependencies (variableObjects);
	}

	public void UpdateVariableBoundsDependencies(IVariableBounds[] variableObjects){
		foreach (IVariableBounds ivb in variableObjects) {
			ivb.NotifyBoundsChanged (this);
		}
	}
}
