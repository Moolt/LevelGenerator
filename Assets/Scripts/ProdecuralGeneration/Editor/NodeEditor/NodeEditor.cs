using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class NodeEditor : EditorWindow {

	private List<BaseNode> nodes = new List<BaseNode> ();
	private bool isMakingTransition = false;
	private BaseNode selectedNode;
	private Vector2 mousePos;

	private bool clickedOnNode = false;
	private int selectedNodeIndex = -1;

	[MenuItem("Window/Node Editor")]
	static void ShowNodeEditor(){
		//NodeEditor editor = EditorWindow.GetWindow<NodeEditor> ();
	}

	void OnGUI(){
		Event e = Event.current;
		mousePos = e.mousePosition;

		selectedNodeIndex = CheckClickedOnNode ();
		clickedOnNode = selectedNodeIndex != -1;

		//Right clicked?
		if (e.button == 1 && !isMakingTransition) {
			if (e.type == EventType.MouseDown) {

				if (!clickedOnNode) {
					ShowWindowContextMenu ();
					e.Use ();
				} else {
					ShowNodeContextMenu ();
					e.Use ();
				}
			}
		} else if (e.button == 0 && e.type == EventType.MouseDown && isMakingTransition) {

			if (clickedOnNode && nodes [selectedNodeIndex] != selectedNode) {
				nodes [selectedNodeIndex].SetInput ((BaseNode)selectedNode, mousePos);
				isMakingTransition = false;
				selectedNode = null;
			}

			if (!clickedOnNode) {
				isMakingTransition = false;
				selectedNode = null;
			}
			e.Use ();

		} else if (e.button == 0 && e.type == EventType.MouseDown && !isMakingTransition) {
			if (selectedNodeIndex != -1) {
				BaseNode nodeToChange = nodes [selectedNodeIndex].ClickedOnInput (mousePos);

				if (nodeToChange != null) {
					selectedNode = nodeToChange;
					isMakingTransition = true;
				}
			}
		}

		if (isMakingTransition && selectedNode != null) {
			Rect mouseRect = new Rect (e.mousePosition.x, e.mousePosition.y, 10, 10);
			DrawNodeCurve (selectedNode.windowRect, mouseRect);
			Repaint ();
		}


		foreach (BaseNode node in nodes) {
			node.DrawCurves ();
		}

		DrawNodes();
	}

	//Iterates through all nodes and checks, whether one was clicked
	//Returns clicked nodes index or -1 if none was clicked
	private int CheckClickedOnNode(){
		for(int i = 0; i < nodes.Count; i++){
			if (nodes [i].windowRect.Contains (mousePos)) {
				return i;
			}
		}
		return -1;
	}

	private void ShowNodeContextMenu(){
		GenericMenu contextMenu = new GenericMenu ();

		contextMenu.AddItem(new GUIContent("Make Transition"), false, ContextMenuCallback, "makeTransition");
		contextMenu.AddItem(new GUIContent("Delete Node"), false, ContextMenuCallback, "deleteNode");
		contextMenu.ShowAsContext ();
	}
		
	private void ShowWindowContextMenu(){
		GenericMenu contextMenu = new GenericMenu ();

		contextMenu.AddItem(new GUIContent("Add Constant"), false, ContextMenuCallback, "constantValueNode");
		contextMenu.AddItem(new GUIContent("Add Input Node"), false, ContextMenuCallback, "inputNode");
		contextMenu.AddItem(new GUIContent("Add Output Node"), false, ContextMenuCallback, "outputNode");
		contextMenu.AddItem(new GUIContent("Add Comparison Node"), false, ContextMenuCallback, "comparison");
		contextMenu.ShowAsContext ();
	}

	private void ContextMenuCallback(object data){
		string name = data.ToString ();

		if (name == "inputNode") {
			BaseNode inputNode = new BaseNode ();
			inputNode.windowRect = new Rect (mousePos.x, mousePos.y, 200, 150);
			nodes.Add (inputNode);
		}
		if (name == "constantValueNode") {
			ConstantValueNode inputNode = new ConstantValueNode ();
			inputNode.windowRect = new Rect (mousePos.x, mousePos.y, 200, 60);
			nodes.Add (inputNode);
		}
		if (name == "deleteNode") {
			if (clickedOnNode) {
				BaseNode selectedNode = nodes [selectedNodeIndex];
				nodes.RemoveAt (selectedNodeIndex);

				foreach (BaseNode baseNode in nodes) {
					baseNode.NodeDeleted (selectedNode);
				}
			}
		}
		if (name == "makeTransition") {
			if (clickedOnNode) {
				selectedNode = nodes [selectedNodeIndex];
				isMakingTransition = true;
			}
		}
	}

	private void DrawNodes(){
		BeginWindows ();
		for(int i = 0; i < nodes.Count; i++) {
			GUI.color = nodes[i].nodeColor;
			nodes [i].windowRect = GUI.Window (i, nodes [i].windowRect, DrawNodeInstance, nodes [i].windowTitle);
			GUI.color = Color.white;
		}
		EndWindows ();
	}

	private void DrawNodeInstance(int id){
		nodes [id].DrawWindow ();
		GUI.DragWindow ();
	}

	public static void DrawNodeCurve(Rect start, Rect end){
		Vector3 startPos = new Vector3 (start.x + start.width, start.y + start.height / 2, 0);
		Vector3 endPos = new Vector3 (end.x, end.y + end.height / 2, 0);
		Vector3 startTan = startPos + Vector3.right * 50;
		Vector3 endTan = endPos + Vector3.left * 50;
		Color shadowCol = new Color (0, 0, 0, 0.06f);

		for (int i = 0; i < 3; i++) {
			Handles.DrawBezier (startPos, endPos, startTan, endTan, shadowCol, null, (i + 3) * 5);
			Handles.DrawBezier (startPos, endPos, startTan, endTan, Color.black, null, 3);
		}
	}
}
