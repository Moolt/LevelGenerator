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
			stickTo.GizmoPreviewState = (GizmoPreviewState)EditorGUILayout.EnumPopup ("Gizmo visibility", stickTo.GizmoPreviewState);
			EditorGUILayout.Space ();

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
		if (SceneUpdater.IsActive) {
			float sizeFactor = HandleUtility.GetHandleSize (roomCenter);
			stickTo.stickDirection = EditorGUIExtension.DirectionHandleVec (roomCenter, sizeFactor, stickTo.stickDirection, Vector3.one);
		}
	}

	private string SticksTo(){
		return (stickTo.hit.collider != null) ? stickTo.hit.collider.name : "Nothing";
	}

	private bool ColliderFound(){
		Collider attachedCollider = stickTo.GetComponentInChildren<Collider> ();
		return attachedCollider != null;
	}
}
