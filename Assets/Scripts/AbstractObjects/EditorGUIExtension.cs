using System.Collections;
using UnityEngine;
using UnityEditor;

public static class EditorGUIExtension {

	public static void RadiusDisc(Vector3 pos, float radius, Color color){
		Handles.color = new Color(color.r, color.g, color.b, 0.1f);
		Handles.DrawSolidDisc (pos, Vector3.up, radius);
		Handles.color = color;
		Handles.DrawWireDisc (pos, Vector3.up, radius);
	}

	public static void AreaRect(Vector2 area, Vector3 origin, Color color){
		Handles.color = color;
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

	public static void DrawPreviewCube(Vector3 pos, Vector3 size, Color color){		
		Gizmos.color = new Color(color.r, color.g, color.b, 0.4f);
		Gizmos.DrawCube (pos, size);
		Gizmos.color = color;
		Gizmos.DrawWireCube (pos, size);
	}

	public static bool DirectionHandle(Vector3 position, Vector3 direction, float sizeFactor, Color color){
		Handles.color = color;
		Handles.DrawDottedLine (position, position + direction * sizeFactor, 3.5f);
		return Handles.Button (position + direction * sizeFactor, Quaternion.identity, sizeFactor / 5f, sizeFactor / 5f, Handles.SphereCap);
	}
}
