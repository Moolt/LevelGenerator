using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;

[CustomEditor (typeof(AbstractValue))]
public class AbstractValueEditor : Editor {
	public List<string> supportedTypes = new List<string>{ "Color", "Single", "Vector3", "Vector2", "Int32", "Bool" };
	public List<AbstractVariableInfo> variableInfos = new List<AbstractVariableInfo>();
	public List<string> variableNames = new List<string>();
	public AbstractValue aValue;

	private SerializedObject serialTarget;
	private SerializedProperty colorList;

	void OnEnable(){
		aValue = target as AbstractValue;
	}

	public override void OnInspectorGUI(){
		if (SceneUpdater.IsActive) {
			aValue.GizmoPreviewState = (GizmoPreviewState)EditorGUILayout.EnumPopup ("Gizmo visibility", aValue.GizmoPreviewState);
			EditorGUILayout.Space ();

			aValue.component = EditorGUILayout.ObjectField ("Target", aValue.component, typeof(Component), true) as Component;
			EditorGUILayout.Space ();

			if (aValue.component != null) {
				
				serialTarget = new SerializedObject (aValue);
				colorList = serialTarget.FindProperty ("randomColors");

				//Only allow to modify components of the gameobject this abstract value is attached to
				//This will prevent bugs with the execution order
				if (IsComponentOfOtherGO (aValue.component)) {
					Debug.LogWarning ("Abstract value may only modify components of the Game Object it is attached to.");
					aValue.component = null;
				}
				
				GetComponentsVariables ();

				aValue.selectedIndex = EditorGUILayout.Popup ("Variable", aValue.selectedIndex, variableNames.ToArray ());
				aValue.showPreview = EditorGUILayout.Toggle ("Show preview", aValue.showPreview);

				if (aValue.prevComponent == null || aValue.prevComponent != aValue.component) {
					aValue.selectedIndex = 0;
					aValue.varInfo = variableInfos [aValue.selectedIndex];
				}

				if (aValue.prevSelectedIndex == -1 || aValue.prevSelectedIndex != aValue.selectedIndex) {
					aValue.varInfo = variableInfos [aValue.selectedIndex];
				}

				EditorGUILayout.Space ();

				EditorGUILayout.LabelField ("Internal Type", variableInfos [aValue.selectedIndex].ShortType.ToString ());

				if (aValue.IsSelectedOfType (typeof(int))) {
					aValue.minIntVal = EditorGUILayout.IntField ("Min value", aValue.minIntVal);
					aValue.maxIntVal = EditorGUILayout.IntField ("Max value", aValue.maxIntVal);
				} else if (aValue.IsSelectedOfType (typeof(Single))) {
					aValue.minFloatVal = EditorGUILayout.FloatField ("Min value", aValue.minFloatVal);
					aValue.maxFloatVal = EditorGUILayout.FloatField ("Max value", aValue.maxFloatVal);
				} else if (aValue.IsSelectedOfType (typeof(Vector3))) {
					aValue.minVec3Val = EditorGUILayout.Vector3Field ("Min value", aValue.minVec3Val);
					aValue.maxVec3Val = EditorGUILayout.Vector3Field ("Max value", aValue.maxVec3Val);
				}else if (aValue.IsSelectedOfType (typeof(Vector2))) {
					aValue.minVec2Val = EditorGUILayout.Vector2Field ("Min value", aValue.minVec2Val);
					aValue.maxVec2Val = EditorGUILayout.Vector2Field ("Max value", aValue.maxVec2Val);
				} else if (aValue.IsSelectedOfType (typeof(bool))) {
					
				} else if (aValue.IsSelectedOfType (typeof(Color))) {

					for (int i = 0; i < colorList.arraySize; i++) {
						SerializedProperty argbColorRef = colorList.GetArrayElementAtIndex (i);
						Color colValue = aValue.Vec4ToCol (argbColorRef.vector4Value);

						EditorGUILayout.BeginHorizontal ();

						colValue = EditorGUILayout.ColorField (colValue);
						argbColorRef.vector4Value = aValue.ColToVec4 (colValue);

						if (GUILayout.Button ("x", GUILayout.Width(20))) {
							colorList.DeleteArrayElementAtIndex (i);
						}

						EditorGUILayout.EndHorizontal ();
					}

					EditorGUILayout.Space ();

					if (GUILayout.Button ("Add color")) {
						colorList.InsertArrayElementAtIndex (colorList.arraySize);
					}

					serialTarget.ApplyModifiedProperties ();
				} else {
					EditorGUILayout.HelpBox ("Type not supported.", MessageType.Info);
				}

				if (GUILayout.Button ("Test")) {
					aValue.Generate ();
				}

				aValue.prevSelectedIndex = aValue.selectedIndex;
				aValue.prevComponent = aValue.component;
			}
		}
		SceneUpdater.UpdateScene ();
	}

	private bool IsComponentOfOtherGO(Component c){
		return c.gameObject != aValue.gameObject;
	}

	//Searches through a component, extracting all of it's fields and properties
	private void GetComponentsVariables(){
		variableNames.Clear ();
		variableInfos.Clear ();
		Type type = aValue.component.GetType ();
		BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Default| BindingFlags.DeclaredOnly | BindingFlags.NonPublic;
		FieldInfo[] fields = type.GetFields (flags);
		PropertyInfo[] properties = type.GetProperties (flags);

		foreach (FieldInfo fi in fields) {
			if (!supportedTypes.Contains (fi.FieldType.Name)) {
				continue;
			}
			AbstractVariableInfo info = new AbstractVariableInfo ();
			info.Name = fi.Name;
			info.Type = fi.FieldType.FullName + ", " + fi.FieldType.Assembly;
			info.ShortType = fi.FieldType.Name;
			info.VariableType = VariableType.FIELD;
			variableInfos.Add (info);
			variableNames.Add (ObjectNames.NicifyVariableName(fi.Name));
		}

		foreach (PropertyInfo pi in properties) {
			if (!supportedTypes.Contains (pi.PropertyType.Name)) {
				continue;
			}
			AbstractVariableInfo info = new AbstractVariableInfo ();
			info.Name = pi.Name;
			info.Type = pi.PropertyType.FullName + ", " + pi.PropertyType.Assembly;
			info.ShortType = pi.PropertyType.Name;
			info.VariableType = VariableType.PROPERTY;
			variableInfos.Add (info);
			variableNames.Add (ObjectNames.NicifyVariableName(pi.Name));
		}
	}
}
