using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof(StickTo))]
public class StickToEditor : Editor {
	private StickTo stickTo;

	void OnEnable(){
		stickTo = target as StickTo;
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
			stickTo.distance = Mathf.Min (stickTo.MaxDistance, stickTo.distance);
			stickTo.distance = EditorGUILayout.Slider ("Distance", stickTo.distance, 0f, stickTo.MaxDistance);
			stickTo.tolerance = EditorGUILayout.Slider ("Tolerance", stickTo.tolerance, 0.05f, 0.95f);
			stickTo.distance = Mathf.Max (0f, stickTo.distance);
			EditorGUILayout.Space ();
			EditorGUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Apply Position")) {
				stickTo.Apply ();
			}
			EditorGUILayout.EndHorizontal ();
			SceneUpdater.UpdateScene ();
		}
	}

	public void OnSceneGUI(){
		if (SceneUpdater.IsActive) {
			float sizeFactor = HandleUtility.GetHandleSize (stickTo.transform.position) * .7f;
			stickTo.stickDirection = EditorGUIExtension.DirectionHandleVec (stickTo.transform.position, sizeFactor, stickTo.stickDirection, Vector3.one);
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
