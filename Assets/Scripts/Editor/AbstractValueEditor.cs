using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

[CustomEditor (typeof(AbstractValue))]
public class AbstractValueEditor : Editor {
	public AbstractValue aValue;
	public List<AbstractVariableInfo> variableInfos = new List<AbstractVariableInfo>();
	public List<string> variableNames = new List<string>();
	public int prevSelectedIndex = -1;
	public Component prevComponent = null;

	void OnEnable(){
		aValue = target as AbstractValue;
	}

	public override void OnInspectorGUI(){
		if (SceneUpdater.IsActive) {
			aValue.component = EditorGUILayout.ObjectField ("Target", aValue.component, typeof(Component), true) as Component;

			EditorGUILayout.Space ();

			if (aValue.component != null) {
				
				GetComponentsVariables ();

				aValue.selectedIndex = EditorGUILayout.Popup ("Variable", aValue.selectedIndex, variableNames.ToArray ());

				if (prevComponent == null || prevComponent != aValue.component) {
					aValue.selectedIndex = 0;
					aValue.varInfo = variableInfos [aValue.selectedIndex];
				}

				if (prevSelectedIndex == -1 || prevSelectedIndex != aValue.selectedIndex) {
					aValue.varInfo = variableInfos [aValue.selectedIndex];
				}

				EditorGUILayout.Space ();

				EditorGUILayout.LabelField ("Internal Type", variableInfos [aValue.selectedIndex].ShortType.ToString ());

				if (IsSelectedOfType (typeof(int))) {
					aValue.minIntVal = EditorGUILayout.IntField ("Min value", aValue.minIntVal);
					aValue.maxIntVal = EditorGUILayout.IntField ("Max value", aValue.maxIntVal);
				} else if (IsSelectedOfType (typeof(Single))) {
					aValue.minFloatVal = EditorGUILayout.FloatField ("Min value", aValue.minFloatVal);
					aValue.maxFloatVal = EditorGUILayout.FloatField ("Max value", aValue.maxFloatVal);
				} else if (IsSelectedOfType (typeof(Vector3))) {
					aValue.minVecVal = EditorGUILayout.Vector3Field ("Min value", aValue.minVecVal);
					aValue.maxVecVal = EditorGUILayout.Vector3Field ("Max value", aValue.maxVecVal);
				} else if (IsSelectedOfType (typeof(bool))) {

				} else {
					EditorGUILayout.HelpBox ("Type not supported.", MessageType.Info);
				}

				if (GUILayout.Button ("prev")) {
					aValue.Generate ();
				}

				prevSelectedIndex = aValue.selectedIndex;
				prevComponent = aValue.component;
			}				
		}
	}

	private bool IsSelectedOfType(Type t){
		Type tt = Type.GetType (variableInfos [aValue.selectedIndex].Type);
		return Type.GetType(variableInfos[aValue.selectedIndex].Type) == t;
	}

	private void GetComponentsVariables(){
		variableNames.Clear ();
		variableInfos.Clear ();
		Type type = aValue.component.GetType ();
		BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Default| BindingFlags.DeclaredOnly | BindingFlags.NonPublic;
		FieldInfo[] fields = type.GetFields (flags);
		PropertyInfo[] properties = type.GetProperties (flags);

		foreach (FieldInfo fi in fields) {
			AbstractVariableInfo info = new AbstractVariableInfo ();
			info.Name = fi.Name;
			info.Type = fi.FieldType.FullName + ", " + fi.FieldType.Assembly;
			info.ShortType = fi.FieldType.Name;
			info.VariableType = VariableType.FIELD;
			variableInfos.Add (info);
			variableNames.Add (ObjectNames.NicifyVariableName(fi.Name));
		}

		foreach (PropertyInfo pi in properties) {
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
