using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(WildcardAsset))]
public class WildcardAssetEditor : Editor {
	private bool showAssets = true;
	private WildcardAsset wildcard;
	private SerializedObject getTarget;
	private SerializedProperty assetList;
	private int listSize;

	void OnEnable(){
		wildcard = target as WildcardAsset;
		getTarget = new SerializedObject (wildcard);
		assetList = getTarget.FindProperty ("chancesList");
	}

	public override void OnInspectorGUI(){
		if (SceneUpdater.IsActive) {
			wildcard.GizmoPreviewState = (GizmoPreviewState)EditorGUILayout.EnumPopup ("Gizmo visibility", wildcard.GizmoPreviewState);
			EditorGUILayout.Space ();

			//Update the list
			getTarget.Update ();

			showAssets = EditorGUILayout.Foldout (showAssets, "Assets");

			if (showAssets) {
				int currentSum = wildcard.SumUpChances ();

				//Show a warning, if there are null refs in the asset list
				if (NullRefsInList ()) {
					EditorGUILayout.HelpBox ("Please remove all null references from the asset list", MessageType.Warning);
				}

				//Show a warning if the chances don't sum up to 100
				if (currentSum != 100) {
					EditorGUILayout.HelpBox ("Probabilities have to sum up to a total of 100%" +
					"\nCurrent sum: " + currentSum.ToString () +
					"\nDelta: " + (100 - currentSum).ToString (),
						MessageType.Error);
				}
					
				EditorGUILayout.Space ();

				//Draw list with object picker, int picker, deletion button
				for (int i = 0; i < assetList.arraySize; i++) {
					EditorGUILayout.BeginHorizontal ();
					SerializedProperty wildcardRef = assetList.GetArrayElementAtIndex (i);
					SerializedProperty assetRef = wildcardRef.FindPropertyRelative ("Asset");
					SerializedProperty chanceRef = wildcardRef.FindPropertyRelative ("Chance");

					assetRef.objectReferenceValue = (GameObject)EditorGUILayout.ObjectField (assetRef.objectReferenceValue, typeof(GameObject), true);
					chanceRef.intValue = EditorGUILayout.IntField (chanceRef.intValue, GUILayout.Width (30));

					if (GUILayout.Button ("x", GUILayout.Width (20))) {
						assetList.DeleteArrayElementAtIndex (i);
					}

					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.Space ();
				}

				if (GUILayout.Button ("Add asset")) {
					assetList.InsertArrayElementAtIndex (assetList.arraySize);
				}
			}

			//Apply all changes to the list
			getTarget.ApplyModifiedProperties ();

			EditorGUILayout.Space ();

			//Slider to preview the objects
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.PrefixLabel ("Preview index:");
			wildcard.selectedIndex = (int)EditorGUILayout.Slider (wildcard.selectedIndex, 0, wildcard.chancesList.Count - 1);
			EditorGUILayout.EndHorizontal ();

			//Selected Asset as preview
			EditorGUILayout.LabelField ("Asset: " + wildcard.chancesList [wildcard.selectedIndex].Asset.name);

			SceneUpdater.UpdateScene ();
		}
	}
		
	//Returns true, if there are null refs in the list of assets
	private bool NullRefsInList(){
		foreach (WildcardChance chance in wildcard.chancesList) {
			if (chance.Asset == null) {
				return true;
			}
		}
		return false;
	}
}
