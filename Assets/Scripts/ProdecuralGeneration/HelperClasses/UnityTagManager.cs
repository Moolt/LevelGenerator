using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//Code based on http://answers.unity3d.com/questions/33597/is-it-possible-to-create-a-tag-programmatically.html

[InitializeOnLoad]
public class UnityTagManager : MonoBehaviour {

	static UnityTagManager(){
		SerializedObject tagManager = new SerializedObject (AssetDatabase.LoadAllAssetsAtPath ("ProjectSettings/TagManager.asset") [0]);
		SerializedProperty tagsProperties = tagManager.FindProperty ("tags");
		List<string> tags = new List<string> (){ "Chunk", "ChunkCopy", "ChunkInstance", "ChunkRemove", "Debug" };
		tags.ForEach (tag => AddTag (tagsProperties, tag));
		tagManager.ApplyModifiedProperties ();
	}

	private static void AddTag(SerializedProperty tagsProperties, string tagName){
		if (!DoesTagExist (tagsProperties, tagName)) {
			tagsProperties.InsertArrayElementAtIndex (0);
			SerializedProperty newTag = tagsProperties.GetArrayElementAtIndex (0);
			newTag.stringValue = tagName;
			Debug.Log ("Tag \"" + tagName + "\" has been added.");
		}
	}

	private static bool DoesTagExist(SerializedProperty tagsProperties, string tagName){
		bool doesTagExist = false;
		for (int i = 0; i < tagsProperties.arraySize; i++) {
			SerializedProperty serializedTag = tagsProperties.GetArrayElementAtIndex (i);
			if (serializedTag.stringValue == tagName) {
				doesTagExist = true;
				break;
			}
		}
		return doesTagExist;
	}
}
