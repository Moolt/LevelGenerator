using UnityEngine;
using System.Collections;
using System;
using System.Reflection;

[Serializable]
public class AbstractVariableInfo{
	[SerializeField]
	private string type;
	[SerializeField]
	private string shortType;
	[SerializeField]
	private string name;
	[SerializeField]
	private VariableType variableType;

	public string Type{ get{ return type; } set{ type = value; } }
	public string Name{ get{ return name; } set{ name = value; } }
	public string ShortType{ get{ return shortType; } set{ shortType = value; } }
	public VariableType VariableType{ get{ return variableType; } set{ variableType = value; } }
}

[Serializable]
public enum VariableType { PROPERTY, FIELD }

[Serializable]
public class AbstractValue : ValueProperty {

	public Component component;
	public int selectedIndex;
	public AbstractVariableInfo varInfo;

	public int minIntVal, maxIntVal;
	public float minFloatVal, maxFloatVal;
	public Vector3 minVecVal, maxVecVal;

	public override void Preview(){

	}

	public override void Generate(){
		Type componentType = component.GetType ();
		object value = GetRandomValue ();

		if (value != null) {
			if (varInfo.VariableType == VariableType.FIELD) {
				FieldInfo fieldInfo = componentType.GetField (varInfo.Name);
				fieldInfo.SetValue (component, value);
			} else {
				PropertyInfo propertyInfo = componentType.GetProperty (varInfo.Name);
				propertyInfo.SetValue (component, value, null);
			}
		}
	}

	private object GetRandomValue(){
		UnityEngine.Random.InitState (System.DateTime.Now.Millisecond);
		Type varType = Type.GetType (varInfo.Type);

		if (varType == typeof(int)) {
			return UnityEngine.Random.Range (minIntVal, maxIntVal);
			//return (int)Mathf.Lerp((int)varInterval.MinValue, (int)varInterval.MaxValue, UnityEngine.Random.value);
		}

		else if (varType == typeof(float)) {
			return UnityEngine.Random.Range (minFloatVal, maxFloatVal);
		}

		else if (varType == typeof(Vector3)) {
			return Vector3.Lerp (minVecVal, maxVecVal, UnityEngine.Random.value);
		}

		else if (varType == typeof(bool)) {
			return UnityEngine.Random.value > 0.4999f;
		}
		return null;
	}

	public void ResetValues(){
		minIntVal = maxIntVal = 0;
		minFloatVal = maxFloatVal = 1f;
		minVecVal = new Vector3 ();
		maxVecVal = new Vector3 ();
	}
}
