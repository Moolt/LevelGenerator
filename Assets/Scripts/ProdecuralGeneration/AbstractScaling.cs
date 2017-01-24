using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class AbstractScaling : TransformingProperty {

	public Vector3 minSize;
	public Vector3 maxSize;
	public bool uniformScaling;
	public float uniformMinSize;
	public float uniformMaxSize;
	public int maxVal = 5;

	public override void Preview(){
		//Handled by OnDrawGizmosSelected
	}

	public override void Generate(){
		if (uniformScaling) {
			float uniformRndScale = uniformMinSize + (uniformMaxSize - uniformMinSize) * Random.value;
			transform.localScale = transform.localScale + Vector3.one * uniformRndScale - Vector3.one;
		} else {
			transform.localScale =  transform.localScale + Vector3.Lerp (minSize, maxSize, Random.value) - Vector3.one;
		}
	}

	public override void DrawEditorGizmos(){
		MeshFilter meshfilter = PreviewMesh;

		if (meshfilter != null) {
			Gizmos.color = Color.red;
			Gizmos.DrawWireMesh (meshfilter.sharedMesh, transform.position, transform.rotation, GetIntervalLimit(Interval.MIN));
			Gizmos.color = Color.green;
			Gizmos.DrawWireMesh (meshfilter.sharedMesh, transform.position, transform.rotation, GetIntervalLimit(Interval.MAX));
		}
	}		

	private Vector3 GetIntervalLimit(Interval inv){
		if (uniformScaling) {
			float uniformVal = (inv == Interval.MIN) ? uniformMinSize : uniformMaxSize;
			return Vector3.one * uniformVal;
		} else {
			return (inv == Interval.MIN) ? minSize : maxSize;
		}
	}
}
