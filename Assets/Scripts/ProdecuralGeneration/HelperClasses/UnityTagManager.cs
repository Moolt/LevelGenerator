using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//Code based on http://answers.unity3d.com/questions/33597/is-it-possible-to-create-a-tag-programmatically.html

public enum TagManagerType { Tag, Layer }

[InitializeOnLoad]
public class UnityTagManager : MonoBehaviour {

	static UnityTagManager(){
		SerializedObject tagManager = new SerializedObject (AssetDatabase.LoadAllAssetsAtPath ("ProjectSettings/TagManager.asset") [0]);
		SerializedProperty tagsProperties = tagManager.FindProperty ("tags");
		SerializedProperty layerProperties = tagManager.FindProperty ("layers");
		List<string> tags = new List<string> (){ "Chunk", "ChunkCopy", "ChunkInstance", "ChunkRemove", "Debug", "HallwayTemplate", "HallwayInstance", "HallwayPrototype" };
		List<string> layers = new List<string> (){ "LevelGeometry", "RoomGeometry" };
		tags.ForEach (tag => AddNew (tagsProperties, tag, TagManagerType.Tag));
		tagManager.ApplyModifiedProperties ();
		layers.ForEach (layer => AddNew (layerProperties, layer, TagManagerType.Layer));
		tagManager.ApplyModifiedProperties ();
	}

	private static void AddNew(SerializedProperty serializedProperties, string elementName, TagManagerType typeName){
		if (!DoesElementExist (serializedProperties, elementName)) {
			int index = typeName == TagManagerType.Tag ? 0 : FindFreeLayer (serializedProperties);
			if (index != -1) {
				serializedProperties.InsertArrayElementAtIndex (index);
				SerializedProperty newElement = serializedProperties.GetArrayElementAtIndex (index);
				newElement.stringValue = elementName;
				Debug.Log (typeName.ToString () + " \"" + elementName + "\" has been added.");
			} else {
				Debug.LogWarning ("Layer " + elementName + " could not be added. Please remove some unused layers and try again.");
			}
		}
	}
		
	private static bool DoesElementExist(SerializedProperty serializedProperties, string elementName){
		bool doesElementExist = false;
		for (int i = 0; i < serializedProperties.arraySize; i++) {
			SerializedProperty serializedTag = serializedProperties.GetArrayElementAtIndex (i);
			if (serializedTag.stringValue == elementName) {
				doesElementExist = true;
				break;
			}
		}
		return doesElementExist;
	}

	private static int FindFreeLayer(SerializedProperty serializedProperties){
		for (int i = 8; i < serializedProperties.arraySize; i++) {
			SerializedProperty layer = serializedProperties.GetArrayElementAtIndex (i);
			if (layer.stringValue == "") {
				return i;
			}
		}
		return -1;
	}
}
