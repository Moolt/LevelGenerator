using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof(StickTo))]
public class StickToEditor : Editor {
	private StickTo stickTo;
	private Vector3 roomCenter;

	void OnEnable(){
		stickTo = target as StickTo;
		roomCenter = (stickTo.AbstractBounds == null) ? Vector3.zero : stickTo.AbstractBounds.Center;
	}

	public override void OnInspectorGUI(){
		if (SceneUpdater.IsActive) {

			if (!ColliderFound ()) {
				EditorGUILayout.HelpBox ("StickTo needs a collider on this or any child Game Objects in order to work.\n" +
				"If you are using a Wildcard Property, please ensure the objects you are instantiating have a Collider attached to them.", MessageType.Info);
			}

			EditorGUILayout.LabelField ("Direction", stickTo.stickDirection.ToString ());
			EditorGUILayout.LabelField ("Sticks to", SticksTo());
			EditorGUILayout.Space ();
			stickTo.updateInEditor = EditorGUILayout.Toggle ("Update in Editor", stickTo.updateInEditor);
			stickTo.distance = Mathf.Min (stickTo.MaxDistance, stickTo.distance);
			stickTo.distance = EditorGUILayout.Slider ("Distance", stickTo.distance, 0f, stickTo.MaxDistance);
			stickTo.distance = Mathf.Max (0f, stickTo.distance);
		}
	}

	public void OnSceneGUI(){		
		float sizeFactor = HandleUtility.GetHandleSize (roomCenter);

		if (EditorGUIExtension.DirectionHandle (roomCenter, Vector3.right, sizeFactor, Color.red)) {
			stickTo.stickDirection = Vector3.right;
		} else if (EditorGUIExtension.DirectionHandle (roomCenter, Vector3.up, sizeFactor, Color.green)) {
			stickTo.stickDirection = Vector3.up;
		} else if (EditorGUIExtension.DirectionHandle (roomCenter, Vector3.forward, sizeFactor, Color.blue)) {
			stickTo.stickDirection = Vector3.forward;
		} else if (EditorGUIExtension.DirectionHandle (roomCenter, Vector3.left, sizeFactor, Color.red)) {
			stickTo.stickDirection = Vector3.left;
		} else if (EditorGUIExtension.DirectionHandle (roomCenter, Vector3.down, sizeFactor, Color.green)) {
			stickTo.stickDirection = Vector3.down;
		} else if (EditorGUIExtension.DirectionHandle (roomCenter, Vector3.back, sizeFactor, Color.blue)) {
			stickTo.stickDirection = Vector3.back;
		}
		SceneUpdater.UpdateScene ();
	}

	private string SticksTo(){
		return (stickTo.hit.collider != null) ? stickTo.hit.collider.name : "Nothing";
	}

	private bool ColliderFound(){
		Collider attachedCollider = stickTo.GetComponentInChildren<Collider> ();
		return attachedCollider != null;
	}
}
