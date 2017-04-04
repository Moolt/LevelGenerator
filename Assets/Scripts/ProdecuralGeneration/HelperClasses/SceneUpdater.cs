using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class SceneUpdater {

	private static bool isActive = true;
	private static bool hideGizmos = false;
	private static List<GameObject> gizmoObjects = new List<GameObject>();
	private static bool hideGizmosChanged = false;

	public static void UpdateScene(){
		if (isActive) {
			GameObject chunk = GameObject.FindGameObjectWithTag ("Chunk");
			chunk = chunk == null ? GameObject.FindGameObjectWithTag ("HallwayPrototype") : chunk;
			ChunkInstantiator generator = ChunkInstantiator.Instance;
			generator.ProcessType = ProcessType.PREVIEW;

			if (chunk != null) {
				generator.InstantiateChunk (chunk, false);

				HandleGizmoVisibility ();
			}
		}
	}

	public static void SetActive(bool state){
		isActive = state;
	}

	public static bool IsActive{
		get{ return isActive; }
	}

	public static bool HideGizmos{
		get{ return hideGizmos; }
		set{
			hideGizmosChanged = hideGizmos != value;
			hideGizmos = value;
		}
	}

	//If gizmos are hid, all active objects are set inactive and stored in a list
	//If gizmos are visibile, all objects in the list are set to visible again
	private static void HandleGizmoVisibility(){
		GameObject chunk = GameObject.FindGameObjectWithTag ("Chunk");
		if (!hideGizmos) {
			foreach (GameObject go in gizmoObjects) {
				if (go != null) {
					go.SetActive (true);
				}
			}
		} else {
			if (hideGizmosChanged) {
				gizmoObjects.Clear ();
				hideGizmosChanged = false;
			}
			foreach (Transform t in chunk.transform) {
				if (t.gameObject.activeSelf) {
					gizmoObjects.Add (t.gameObject);
					t.gameObject.SetActive (false);
				}
			}
		}
	}
}
