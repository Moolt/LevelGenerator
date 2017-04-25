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
			Vector3 randomScale = new Vector3 (Random.Range (minSize.x, maxSize.x), 
				Random.Range (minSize.y, maxSize.y), 
				Random.Range (minSize.z, maxSize.z));
			transform.localScale =  transform.localScale + randomScale - Vector3.one;
		}
	}

	public override void DrawEditorGizmos(){
		MeshFilter meshfilter = PreviewMesh;

		if (meshfilter != null) {
			Gizmos.color = Color.red;
			Gizmos.DrawWireMesh (meshfilter.sharedMesh, transform.position, transform.rotation, transform.localScale + GetIntervalLimit(Interval.MIN) - Vector3.one);
			Gizmos.color = Color.green;
			Gizmos.DrawWireMesh (meshfilter.sharedMesh, transform.position, transform.rotation, transform.localScale + GetIntervalLimit(Interval.MAX) - Vector3.one);
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
