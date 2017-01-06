using UnityEngine;
using System.Collections;

public static class SceneUpdater {

	private static bool isActive = true;

	public static void UpdateScene(){
		if (isActive) {
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

	public static void SetActive(bool state){
		isActive = state;
	}

	public static bool IsActive{
		get{ return isActive; }
	}
}
