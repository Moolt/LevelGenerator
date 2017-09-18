using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[System.Serializable]
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
		return Handles.Button (position + direction * sizeFactor, Quaternion.identity, sizeFactor / 4f, sizeFactor / 4f, Handles.SphereHandleCap);
	}

	public static Vector3 DirectionHandleVec(Vector3 position, float sizeFactor, Vector3 initialDir, Vector3 axis){
		if (axis.x == 1f && EditorGUIExtension.DirectionHandle (position, Vector3.right, sizeFactor, Color.red)) {
			return Vector3.right;
		} else if (axis.y == 1f && EditorGUIExtension.DirectionHandle (position, Vector3.up, sizeFactor, Color.green)) {
			return Vector3.up;
		} else if (axis.z == 1f && EditorGUIExtension.DirectionHandle (position, Vector3.forward, sizeFactor, Color.blue)) {
			return Vector3.forward;
		} else if (axis.x == 1f && EditorGUIExtension.DirectionHandle (position, Vector3.left, sizeFactor, Color.red)) {
			return Vector3.left;
		} else if (axis.y == 1f &&EditorGUIExtension.DirectionHandle (position, Vector3.down, sizeFactor, Color.green)) {
			return Vector3.down;
		} else if (axis.z == 1f && EditorGUIExtension.DirectionHandle (position, Vector3.back, sizeFactor, Color.blue)) {
			return Vector3.back;
		}
		return initialDir;
	}
}
#endif