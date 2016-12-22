using System.Collections;
using UnityEngine;
using UnityEditor;

public class LevelGenerationWindow : EditorWindow {
	
	[MenuItem("Window/Level Generation")]
	public static void ShowWindow(){
		EditorWindow.GetWindow (typeof(LevelGenerationWindow));
	}
	// Use this for initialization
	void OnGUI(){		

		GUILayout.Space (20);

		if (GUILayout.Button ("Generate Chunk")) {
			GameObject chunk = GameObject.FindWithTag ("Chunk");
			IAbstractAsset[] abstractAssets = chunk.GetComponents<IAbstractAsset> ();

			foreach (IAbstractAsset comp in abstractAssets) {
				comp.Generate ();
			}
		}
	}
}
