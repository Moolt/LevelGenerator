using UnityEngine;
using System.Collections;

public static class SceneUpdater {

	public static void UpdateScene(){
		GameObject chunk = GameObject.FindGameObjectWithTag ("Chunk");
		if (chunk != null) {
			AbstractProperty[] iaa = chunk.GetComponentsInChildren<AbstractProperty> ();

			foreach (AbstractProperty iaa_ in iaa) {
				iaa_.Preview ();
			}
		} else {
			Debug.LogError ("SceneUpdater didn't find an object with the tag \"Chunk\".");
		}
	}
}
