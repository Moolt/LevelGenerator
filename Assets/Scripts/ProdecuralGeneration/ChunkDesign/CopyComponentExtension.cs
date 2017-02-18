using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEngine;
using System;

[System.Serializable]
public static class CopyComponentExtension{

	private static Dictionary<string, string> exceptions = new Dictionary<string, string>
	{
		{"material", "sharedMaterial"},
		{"materials", "sharedMaterials"},
		{"mesh", "sharedMesh"}
	};

	public static T GetCopyOf<T>(this Component comp, T other) where T : Component
	{
		Type type = comp.GetType(); //type of the copy
		if (type != other.GetType()) return null; // type mis-match
		BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Default  | BindingFlags.FlattenHierarchy;
		PropertyInfo[] pinfos = type.GetProperties(flags);

		//Handle variables
		foreach (var pinfo in pinfos) {
			if (pinfo.CanWrite) {
				try {
					//Ignore obsolete variables to avoid editor warnings
					if(HasAnnotation<ObsoleteAttribute>(pinfo) || HasAnnotation<NotSupportedException>(pinfo) || HasAnnotation<System.ComponentModel.EditorBrowsableAttribute>(pinfo)){
						continue;
					}
					if(exceptions.ContainsKey(pinfo.Name)){
						PropertyInfo exInfo = type.GetProperty(exceptions[pinfo.Name]);
						exInfo.SetValue(comp, exInfo.GetValue(other, null), null);
					} else {
						pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
					}
				}
				catch (System.Exception e) { Debug.Log (e.Message); } 
			}
		}

		//Handle properties
		FieldInfo[] finfos = type.GetFields(flags);

		foreach (var finfo in finfos) {
			finfo.SetValue(comp, finfo.GetValue(other));
		}

		return comp as T;
	}

	public static bool HasAnnotation<T> (PropertyInfo pinfo){
		object[] attr = pinfo.GetCustomAttributes(typeof(T), true);

		return attr.Length > 0;
	}
}