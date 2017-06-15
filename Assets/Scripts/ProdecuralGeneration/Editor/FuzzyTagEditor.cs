using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System.IO;
using System;

public class NewAttribute{
	public static int _id;
	public int id;
	public string name;

	public NewAttribute(string name){
		id = _id++;
		this.name = name;
	}
}

public class FuzzyTagEditor : EditorWindow {

	private FuzzyAttributes attributes;
	private string[] attributeNames;
	private int selectedIndex = 0;

	private bool isCreatingNew = false;

	public string newName = "New Attribute";
	public List<NewAttribute> newAttributes = new List<NewAttribute>();

	[MenuItem("Window/Dynamic Tag Editor")]
	public static void ShowWindow(){
		EditorWindow.GetWindow (typeof(FuzzyTagEditor));
	}

	void OnEnable(){
		Load ();
		//CreateXML ();
	}

	void OnDestroy(){
		Save ();
	}

	private void Save(){
		string serialized = JsonUtility.ToJson (attributes, true);
		string path = Application.dataPath + "/Resources/ProceduralGeneration/DynamicTags.json";
		File.WriteAllText (path, serialized);
	}

	private void CreateXML(){

		TextAsset textAsset = Resources.Load ("FuzzyAttributes") as TextAsset;
		FuzzyAttributes oldAttr = JsonUtility.FromJson<FuzzyAttributes> (textAsset.text);

		FuzzyAttributes attributes = new FuzzyAttributes ();
		attributes.attributes = new FuzzyAttribute[oldAttr.attributes.Length];

		for (int i = 0; i < attributes.attributes.Length; i++) {
			attributes.attributes [i] = new FuzzyAttribute ();
			attributes.attributes [i].name = oldAttr.attributes [i].name;
			attributes.attributes [i].descriptors = new FuzzyDescriptor[oldAttr.attributes [i].attributes.Length];

			for(int j = 0; j < attributes.attributes[i].descriptors.Length; j++){
				attributes.attributes [i].descriptors [j] = new FuzzyDescriptor ();
				attributes.attributes [i].descriptors [j].name = oldAttr.attributes [i].attributes [j];
				attributes.attributes [i].descriptors [j].value = UnityEngine.Random.value;
			}
		}

		string serialized = JsonUtility.ToJson (attributes, true);
		string path = Application.dataPath + "/Resources/newjson.json";
		Debug.Log (path);
		File.WriteAllText (path, serialized);
	}

	private void Load(){
		TextAsset textAsset = Resources.Load ("ProceduralGeneration/DynamicTags") as TextAsset;
		attributes = JsonUtility.FromJson<FuzzyAttributes> (textAsset.text);
		UpdateNameList ();
	}

	private void UpdateNameList(){
		attributeNames = attributes.attributes.ToList ().Select (a => a.name).ToArray();
	}

	// Use this for initialization
	void OnGUI(){

		EditorGUILayout.Space();

		selectedIndex = EditorGUILayout.Popup (selectedIndex, attributeNames);

		FuzzyAttribute selectedAttribute = attributes.attributes [selectedIndex];

		foreach (FuzzyDescriptor descr in selectedAttribute.descriptors) {
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField (descr.name);
			descr.value = EditorGUILayout.Slider (descr.value, 0f, 1f);
			EditorGUILayout.EndHorizontal ();
		}
		
		EditorGUILayout.Space ();
		EditorGUILayout.BeginHorizontal ();
		if (GUILayout.Button ("Remove")) {
			RemoveAttribute (selectedAttribute);
			selectedIndex = 0;
		}

		if (GUILayout.Button("Update Order")) {
			UpdateOrder (selectedAttribute);
			//Save();
			Save ();
		}

		if (GUILayout.Button ("Default Values")) {
			SetDefaultValues (selectedAttribute);
		}

		if (GUILayout.Button ("Save Changes")) {
			Save ();
		}

		EditorGUILayout.EndHorizontal ();

		EditorGUILayout.Space ();
		Separator ();
		if (!isCreatingNew && GUILayout.Button ("Create New")) {
			//CreateXML ();
			isCreatingNew = true;
		}

		if (isCreatingNew) {
			List<NewAttribute> removalList = new List<NewAttribute> ();
			newName = EditorGUILayout.TextField ("Name", newName);

			for (int i = 0; i < newAttributes.Count; i++) {
				EditorGUILayout.BeginHorizontal ();
				newAttributes[i].name = EditorGUILayout.TextField (newAttributes[i].name);
				if (GUILayout.Button ("x", GUILayout.Width(20))) {
					removalList.Add (newAttributes [i]);
				}
				EditorGUILayout.EndHorizontal();
			}

			newAttributes.RemoveAll (s => removalList.Contains (s));

			if (GUILayout.Button ("Add")) {
				newAttributes.Add (new NewAttribute("Empty" + NewAttribute._id.ToString()));
			}

			EditorGUILayout.BeginHorizontal ();

			if (GUILayout.Button ("Cancel")) {
				isCreatingNew = false;
				newName = "New Attribute";
				InitNewAttribute ();
			}

			if (GUILayout.Button ("Apply")) {
				CreateNewFuzzyAttributes ();
				InitNewAttribute ();
				Save ();
				//selectedIndex = attributes.attributes.Length - 1;
			}

			EditorGUILayout.EndHorizontal ();
		}
	}

	private void CreateNewFuzzyAttributes(){
		List<FuzzyAttribute> completeList = attributes.attributes.ToList ();
		FuzzyAttribute newFuzzyAttr = new FuzzyAttribute ();
		newFuzzyAttr.name = newName;
		newFuzzyAttr.descriptors = new FuzzyDescriptor[newAttributes.Count];
		for (int i = 0; i < newAttributes.Count; i++) {
			newFuzzyAttr.descriptors [i] = new FuzzyDescriptor ();
			newFuzzyAttr.descriptors [i].name = newAttributes [i].name;
			newFuzzyAttr.descriptors [i].value = (1f / (newAttributes.Count)) * (i + 1);
		}
		completeList.Add (newFuzzyAttr);
		attributes.attributes = completeList.ToArray ();
		//Save ();
		//Load ();
		UpdateNameList();
	}

	private void UpdateOrder(FuzzyAttribute selectedAttribute){
		selectedAttribute.descriptors = selectedAttribute.descriptors.ToList ().OrderBy (d => d.value).ToArray ();
	}

	private void SetDefaultValues(FuzzyAttribute selectedAttribute){
		UpdateOrder (selectedAttribute);

		for(int i = 0; i < selectedAttribute.descriptors.Length; i++){
			selectedAttribute.descriptors[i].value = (1f / (selectedAttribute.descriptors.Length)) * (i + 1);
		}
	}

	private void InitNewAttribute(){
		newName = "New Attribute";
		newAttributes = new List<NewAttribute> ();
		isCreatingNew = false;
	}

	private void RemoveAttribute(FuzzyAttribute removeAttribute){
		List<FuzzyAttribute> completeList = attributes.attributes.ToList ();
		completeList.Remove (removeAttribute);
		attributes.attributes = completeList.ToArray ();
		UpdateNameList ();
	}

	private void Separator(){
		GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
	}
}
