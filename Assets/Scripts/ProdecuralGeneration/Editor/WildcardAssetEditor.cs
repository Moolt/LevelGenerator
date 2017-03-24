using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;
using System.Linq;

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
		wildcard.UpdateChildren ();
	}

	public override void OnInspectorGUI(){
		if (SceneUpdater.IsActive) {
			wildcard.GizmoPreviewState = (GizmoPreviewState)EditorGUILayout.EnumPopup ("Gizmo visibility", wildcard.GizmoPreviewState);
			EditorGUILayout.Space ();

			wildcard.wildcardTarget = (WildcardTarget)EditorGUILayout.EnumPopup ("Target", wildcard.wildcardTarget);

			if (wildcard.wildcardTarget == WildcardTarget.CHILDREN) {
				SerializedProperty children = getTarget.FindProperty ("children");

				EditorGUILayout.Space ();
				for (int i = 0; i < children.arraySize; i++) {
					SerializedProperty child = children.GetArrayElementAtIndex (i);
					SerializedProperty childName = child.FindPropertyRelative ("name");
					SerializedProperty childChance = child.FindPropertyRelative ("chance");
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (childName.stringValue);
					childChance.intValue = EditorGUILayout.IntField (childChance.intValue);
					EditorGUILayout.EndHorizontal ();
				}
				EditorGUILayout.Space ();
				if(GUILayout.Button ("Update")){
					wildcard.UpdateChildren();
				}
				getTarget.ApplyModifiedProperties ();
			} else {

				//Update the list
				getTarget.Update ();

				showAssets = EditorGUILayout.Foldout (showAssets, "Assets");

				if (showAssets) {
					int currentSum = wildcard.SumUpChances ();

					//Show a warning, if there are null refs in the asset list
					if (NullRefsInList ()) {
						EditorGUILayout.HelpBox ("Null references will result in an empty object.", MessageType.Warning);
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
				wildcard.selectedIndex = Mathf.Clamp (wildcard.selectedIndex, 0, Mathf.Max (0, wildcard.chancesList.Count - 1));
				if (wildcard.chancesList.Count > 0) {
					GameObject asset = wildcard.chancesList [wildcard.selectedIndex].Asset;
					string previewName = asset != null ? asset.name : "null";
					EditorGUILayout.LabelField ("Asset: " + previewName);
				} else {
					EditorGUILayout.LabelField ("Nothing to preview");
				}
			}
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
