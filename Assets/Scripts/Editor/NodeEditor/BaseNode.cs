using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BaseNode : ScriptableObject {

	public Color nodeColor = Color.white;
	public Rect windowRect;
	public bool hasInputs = false;
	public string windowTitle = "";

	protected BaseNode inputNode;
	protected Rect inputNodeRect;

	public virtual void DrawWindow(){
		windowTitle = EditorGUILayout.TextField ("Title", windowTitle);
		EditorGUILayout.LabelField (windowTitle);

		if (Event.current.type == EventType.Repaint) {
			inputNodeRect = GUILayoutUtility.GetLastRect ();
		}
	}

	public virtual void DrawCurves (){
		if (inputNode != null) {
			Rect rect = windowRect;
			rect.x += inputNodeRect.x;
			rect.y += inputNodeRect.y + inputNodeRect.height / 2;
			rect.width = 1;
			rect.height = 1;

			NodeEditor.DrawNodeCurve (inputNode.windowRect, rect);
		}
	}

	public virtual void SetInput(BaseNode input, Vector2 clickPos){
		clickPos.x -= windowRect.x;
		clickPos.y -= windowRect.y;

		if (inputNodeRect.Contains (clickPos)) {
			inputNode = input;
		}
	}

	public virtual void NodeDeleted(BaseNode node){
		inputNode = null;
	}

	public virtual BaseNode ClickedOnInput(Vector2 pos){
		return null;
	}
}
