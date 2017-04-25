using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof(ScatteredArray))]
public class ScatteredArrayEditor : Editor {
	private ScatteredArray sArray;

	void OnEnable(){
		sArray = target as ScatteredArray;
	}

	public override void OnInspectorGUI(){
		if (SceneUpdater.IsActive) {
			sArray.GizmoPreviewState = (GizmoPreviewState)EditorGUILayout.EnumPopup ("Gizmo visibility", sArray.GizmoPreviewState);
			EditorGUILayout.Space ();

			if (sArray.hasOverflow && !sArray.autoFill) {
				EditorGUILayout.HelpBox ("Overflow detected. Please reduce the amount of copies.", MessageType.Error);
			}

			sArray.editorSeed = EditorGUILayout.IntField ("Editor seed", sArray.editorSeed);

			EditorGUILayout.Space ();

			sArray.areaType = (AreaType) EditorGUILayout.EnumPopup ("Type of area" , sArray.areaType);

			if (sArray.areaType == AreaType.SPHERICAL) {
				sArray.radius = EditorGUILayout.FloatField ("Radius", sArray.radius);
				sArray.radius = Mathf.Max (0f, sArray.radius);
			} 

			if (sArray.areaType == AreaType.RECT) {
				sArray.rectArea = EditorGUILayout.Vector2Field ("Area size", sArray.rectArea); 
			}

			EditorGUILayout.Space ();

			sArray.autoFill = EditorGUILayout.Toggle ("Auto fill", sArray.autoFill);
			if (sArray.autoFill) {
				//Prevent overlapping has to be true in order to cause an overflow that stops cultivating the radius
				sArray.preventOverlapping = true;
			} else {
				sArray.preventOverlapping = EditorGUILayout.Toggle ("Prevent overlapping", sArray.preventOverlapping);
				//Duplicate count is variable and cannot be set
				sArray.duplicateCount = EditorGUILayout.IntField ("Duplicates", sArray.duplicateCount, GUILayout.ExpandWidth(false));
			}

			if (sArray.preventOverlapping || sArray.autoFill) {
				sArray.spacing = EditorGUILayout.FloatField ("Spacing", sArray.spacing);
				sArray.spacing = Mathf.Max (0f, sArray.spacing);
			}

			sArray.duplicateCount = Mathf.Max (0, sArray.duplicateCount);
		}
		SceneUpdater.UpdateScene ();
	}

	public void OnSceneGUI(){

		if(sArray.areaType == AreaType.SPHERICAL){
			EditorGUIExtension.RadiusDisc (sArray.transform.position, sArray.radius, Color.yellow);
		}

		if(sArray.areaType == AreaType.RECT){
			EditorGUIExtension.AreaRect (sArray.rectArea, sArray.transform.position, Color.yellow);
		}

		if (sArray.areaType == AreaType.ABSTRACTBOUNDS) {
			Vector3 bounds = sArray.AbstractBounds.Size;
			Vector3 origin = sArray.AbstractBounds.transform.position;
			Vector2 rect = new Vector2 (bounds.x, bounds.z);
			EditorGUIExtension.AreaRect (rect, origin, Color.yellow);
		}
		//float spacingRadius = sArray.spacing + sArray.PreviewMesh.sharedMesh.bounds.extents.magnitude;
		//RadiusDisc (spacingRadius, Color.black);
	}
}
