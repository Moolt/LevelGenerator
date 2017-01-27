using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ConstantValueNode : BaseNode {

	private int value;

	public ConstantValueNode(){
		nodeColor = new Color(0.79f,0.16f,0.16f,1);
	}

	public override void DrawWindow(){
		value = EditorGUILayout.IntField ("Value", value);

		if (Event.current.type == EventType.Repaint) {
			inputNodeRect = GUILayoutUtility.GetLastRect ();
		}
	}

}
