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
				sArray.duplicateCount = EditorGUILayout.IntField ("Duplicate Count", sArray.duplicateCount);
			}

			sArray.spacing = EditorGUILayout.FloatField ("Spacing", sArray.spacing);
			sArray.spacing = Mathf.Max (0f, sArray.spacing);

			sArray.duplicateCount = Mathf.Max (0, sArray.duplicateCount);
		}
		SceneUpdater.UpdateScene ();
	}

	public void OnSceneGUI(){		
		if(sArray.areaType == AreaType.SPHERICAL){
			RadiusDisc (sArray.radius, Color.yellow);
		}

		if(sArray.areaType == AreaType.RECT){
			AreaRect (sArray.rectArea, sArray.transform.position, Color.yellow);
		}

		if (sArray.areaType == AreaType.ABSTRACTBOUNDS) {
			Vector3 bounds = sArray.ParentsAbstractBounds.Bounds;
			Vector3 origin = sArray.ParentsAbstractBounds.transform.position;
			Vector2 rect = new Vector2 (bounds.x, bounds.z);
			AreaRect (rect, origin, Color.yellow);
		}
		//float spacingRadius = sArray.spacing + sArray.PreviewMesh.sharedMesh.bounds.extents.magnitude;
		//RadiusDisc (spacingRadius, Color.black);
	}

	private void RadiusDisc(float radius, Color color){
		Handles.color = new Color(color.r, color.g, color.b, 0.1f);
		Handles.DrawSolidDisc (sArray.transform.position, Vector3.up, radius);
		Handles.color = color;
		Handles.DrawWireDisc (sArray.transform.position, Vector3.up, radius);
	}

	private void AreaRect(Vector2 area, Vector3 origin, Color color){
		Color faceColor = new Color(color.r, color.g, color.b, 0.1f);
		Vector2 rect = area / 2f;

		Vector3[] verts = new Vector3[] { 
			new Vector3 (origin.x - rect.x, origin.y, origin.z - rect.y),
			new Vector3 (origin.x + rect.x, origin.y, origin.z - rect.y),
			new Vector3 (origin.x + rect.x, origin.y, origin.z + rect.y),
			new Vector3 (origin.x - rect.x, origin.y, origin.z + rect.y)
		};

		Handles.DrawSolidRectangleWithOutline(verts, faceColor, color);
	}
}
