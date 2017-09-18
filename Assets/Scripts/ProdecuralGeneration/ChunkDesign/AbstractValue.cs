using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEngine;
using System;

//Class containing information about the selected property or field
//Is needed, since it has to be serializable in order to be copied during generation time
//PropertyInfo and FieldInfo aren't serializable and they are encapsulated into this class
//Types are stored as strings, containing assembly, namespace and type, since Type itself isn't serializable
[Serializable]
public class AbstractVariableInfo{
	[SerializeField]
	private string type; //Full type, containing assembly, namespace, type
	[SerializeField]
	private string shortType; //Only type
	[SerializeField]
	private string name; //Name of the variable
	[SerializeField]
	private VariableType variableType; //Either property or field

	public string Type{ get{ return type; } set{ type = value; } }
	public string Name{ get{ return name; } set{ name = value; } }
	public string ShortType{ get{ return shortType; } set{ shortType = value; } }
	public VariableType VariableType{ get{ return variableType; } set{ variableType = value; } }
}

[Serializable]
public enum VariableType { PROPERTY, FIELD }

[Serializable]
public class AbstractValue : ValueProperty {

	public Component component; //The component the variable belongs to
	public Component prevComponent = null; //Previously selected component, used for Editor scripting
	public int selectedIndex; //The index of the element currently selected in the inspector list
	public int prevSelectedIndex = -1; //Previously selected index, used for Editor scripting
	public AbstractVariableInfo varInfo; //Object containing info about the selected field / property

	public int minIntVal, maxIntVal;
	public float minFloatVal, maxFloatVal;
	public Vector3 minVec3Val, maxVec3Val;
	public Vector2 minVec2Val, maxVec2Val;
	public List<Vector4> randomColors = new List<Vector4>(0);
	public bool showPreview = true; //Used by OnDrawGizmosSelected

	public override void Preview(){

	}

	//Chooses a random value, depending on the type of the variable and the given intervals
	//Sets the random value to the respective variable of the respective component via reflection
	//Since variables can be both fields and auto properties, the method differentiates between the two options
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

	//Returns true, if the current selected variable is of the given type
	public bool IsSelectedOfType(Type t){
		return Type.GetType(varInfo.Type) == t;
	}

    //Draw a preview Gizmo, if showPreview is enabled
    //For int and float, a disc for the min and max values is drawn
    //For vector3, a box for each min and max value is drawn
#if UNITY_EDITOR
    public override void DrawEditorGizmos(){
		if (component != null && showPreview) {
			if (IsSelectedOfType (typeof(int))) {
				EditorGUIExtension.RadiusDisc (transform.position, minIntVal, Color.red);
				EditorGUIExtension.RadiusDisc (transform.position, maxIntVal, Color.green);
			} else if (IsSelectedOfType (typeof(Single))) {
				EditorGUIExtension.RadiusDisc (transform.position, minFloatVal, Color.red);
				EditorGUIExtension.RadiusDisc (transform.position, maxFloatVal, Color.green);
			} else if (IsSelectedOfType(typeof(Vector3))){
				EditorGUIExtension.DrawPreviewCube (transform.position, minVec3Val, Color.red);
				EditorGUIExtension.DrawPreviewCube (transform.position, maxVec3Val, Color.green);
			} else if (IsSelectedOfType(typeof(Vector2))){
				EditorGUIExtension.AreaRect(minVec2Val, transform.position, Color.red);
				EditorGUIExtension.AreaRect(maxVec2Val, transform.position, Color.green);
			}
		}
	}
#endif

    //Depending on which type of variable is selected, a random value is being returned
    //For int, float and vector3 a lerp function is being used
    //For bool, true of false are being returned with a chance of 50/50
    //For color, a random color from the list is being returned
    private object GetRandomValue(){
		//Randomize the seed when in editor mode, but not durin generation process
		if (Application.isEditor && SceneUpdater.IsActive && !ProceduralLevel.IsGenerating) {
			UnityEngine.Random.InitState (System.DateTime.Now.Millisecond);			
		}

		Type varType = Type.GetType (varInfo.Type);

		if (varType == typeof(int)) {
			return UnityEngine.Random.Range (minIntVal, maxIntVal);
		}

		else if (varType == typeof(float)) {
			return UnityEngine.Random.Range (minFloatVal, maxFloatVal);
		}

		else if (varType == typeof(Vector3)) {
			return Vector3.Lerp (minVec3Val, maxVec3Val, UnityEngine.Random.value);
		}

		else if (varType == typeof(Vector2)) {
			return Vector2.Lerp (minVec2Val, maxVec2Val, UnityEngine.Random.value);
		}

		else if (varType == typeof(Color)) {
			if (randomColors.Count == 0) {
				return Color.white;
			} else {
				int randomIndex = (int)(randomColors.Count * UnityEngine.Random.value);
				return Vec4ToCol (randomColors [randomIndex]);
			}
		}

		else if (varType == typeof(bool)) {
			return UnityEngine.Random.value > 0.4999f;
		}
		return null;
	}
		
	public Color Vec4ToCol(Vector4 vec){
		return new Color (vec.x, vec.y, vec.z, vec.w);
	}

	public Vector4 ColToVec4(Color col){
		return new Vector4 (col.r, col.g, col.b, col.a);
	}
}
