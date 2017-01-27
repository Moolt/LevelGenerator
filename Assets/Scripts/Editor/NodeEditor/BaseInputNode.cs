using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BaseInputNode : BaseNode {

	float f = 0f;

	public BaseInputNode(){
		windowTitle = "new window";
	}

	public override void DrawWindow(){
		f= EditorGUILayout.FloatField (f);
		EditorGUILayout.LabelField (f.ToString ());
	}

	public virtual string GetResult(){
		return "None";
	}

	public override void DrawCurves(){
	}
}
