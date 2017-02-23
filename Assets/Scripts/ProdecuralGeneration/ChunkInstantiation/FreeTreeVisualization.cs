using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FreeTreeVisualization{
	private List<RoomNode> modifiedNodes;
	private RoomNode rootnode;
	private float d = 20f;
	private int nodeAmount;

	public FreeTreeVisualization (float d, int nodeAmount, RoomNode rootnode){
		this.d = d;
		this.nodeAmount = nodeAmount;
		this.rootnode = rootnode;
		this.modifiedNodes = new List<RoomNode> ();
	}

	public float r(float p){
		return  2f * Mathf.Acos (p / (p + d));
	}

	public void FreeTree(){
		RoomNode[] centres = TreeCentre ();

		if (centres.Length == 1) {
			DrawSubTree (centres [0], 0f, 0f, 2f * Mathf.PI);
		} else {
			DrawSubTree (centres [0], d / 2f, (3f * Mathf.PI) / 2f, (5f / 2f) * Mathf.PI);
			DrawSubTree (centres [1], d / 2f, Mathf.PI / 2f, (3f * Mathf.PI) / 2f);
		}
		RestoreTree ();
	}

	public void DrawSubTree(RoomNode v, float p, float a1, float a2){
		float s, a;
		//POLARKOORDINATEN
		Vector3 cartesian = Vector3.zero;
		Vector2 polar = new Vector2 (p, (a1 + a2) / 2f);
		cartesian.x = polar.x * Mathf.Cos (polar.y);
		cartesian.z = polar.x * Mathf.Sin (polar.y);
		v.Position = cartesian;

		if (r (p) < (a2 - a1)) {
			s = r (p) / w (v);
			a = (a1 + a2 - r (p)) / 2f;
		} else {
			s = (a2 - a1) / w (v);
			a = a1;
		}

		foreach (RoomNode u in v.Connections) {
			DrawSubTree (u, p + d, a, a + s * w (u));
			a = a + s * w (u);
		}
	}

	public int w(RoomNode v){		
		int leafCount = FindActualLeaves (v);
		int leafID = v.ID;
		return leafCount;
	}

	public RoomNode[] TreeCentre(){
		Queue<RoomNode> leaves = new Queue<RoomNode> ();
		int depth = 0;
		FindLeaves (rootnode, leaves, depth++);

		for(int i = 0; i < nodeAmount; i++) {
			if (leaves.Count == 0) {
				FindLeaves (rootnode, leaves, depth++);
			}

			RoomNode leaf = leaves.Dequeue ();
			leaf.Marked = true;

			//The index at which only two nodes remain
			//If their depth is the same, they were added at the same step and
			//Are therefore both the middle
			if (i == nodeAmount - 2) {
				//If there is nothing left in the queue, the next leaf has a different depth than
				//The current one, meaning, that this is our center leaf. If there still is a leaf
				//In the queue, then both leaves have the same depth and are both part of the center.
				if (leaves.Count == 0) {
					FindLeaves (rootnode, leaves, depth++);
				} else {
					leaves.Enqueue (leaf);
				}
				break;
			}

			if (leaf.Parent != null) {
				leaf.Parent.ChildrenCount -= 1;
			}

			foreach (RoomNode child in leaf.Connections) {
				child.ChildrenCount -= child.Marked ? 0 : 1;
			}
		}
		//Reset all the Connection values changed during this algorithm
		//This is necessary for the free tree algorithm
		ResetLeaves(rootnode);
		//Root nodes may have changed
		RebuildTreeFromCentre (leaves.ToArray());
		return leaves.ToArray ();
	}

	//Finds leaves everywhere in the graph, assuming it is UNDIRECTIONAL
	private void FindLeaves(RoomNode node, Queue<RoomNode> queue, int depth){
		if (node.ChildrenCount < 2 && !node.Marked) {
			node.Depth = depth;
			queue.Enqueue (node);
		}

		foreach (RoomNode subNode in node.Connections) {
			FindLeaves (subNode, queue, depth);
		}
	}

	//Assuming the graph is DIRECTIONAL, so only nodes with no children will be detected as leaves
	private int FindActualLeaves(RoomNode node){
		Queue<RoomNode> leaves = new Queue<RoomNode> ();
		if (node.Connections.Count == 0) {
			return 1;
		} else {
			FindActualLeaves (node, leaves);
		}
		return leaves.Count;
	}

	private void FindActualLeaves(RoomNode node, Queue<RoomNode> queue){
		if (node.Connections.Count == 0) {
			queue.Enqueue (node);
		}

		foreach (RoomNode subNode in node.Connections) {
			FindActualLeaves (subNode, queue);
		}
	}

	private void ResetLeaves(RoomNode node){
		node.Marked = false;
		node.ChildrenCount = -1; //Force recalculate, see RoomNode class

		foreach (RoomNode subNode in node.Connections) {
			ResetLeaves (subNode);
		}
	}

	public void ApplyNewCenter(RoomNode center){
		Queue<RoomNode> nodeQueue = new Queue<RoomNode> ();

		if (center.Parent != null) {
			center.AddConnection (center.Parent, false);
			center.Parent = null;
			nodeQueue.Enqueue (center);
		}

		while (nodeQueue.Count > 0) {
			RoomNode parent = nodeQueue.Dequeue ();
			foreach (RoomNode child in parent.Connections) {
				if (child.Parent != parent) {
					if (child.Parent != null) {
						child.AddConnection (child.Parent, false);
						nodeQueue.Enqueue (child);						
					}
					child.RemoveConnection (parent, false);
					child.Parent = parent;
				}
			}
		}
	}

	public void RebuildTreeFromCentre(RoomNode[] centres){
		SaveTree (rootnode);
		if (centres.Length == 2) {
			if (centres [0].Parent == centres [1]) {
				centres [1].RemoveConnection (centres [0], false);
				centres [0].Parent = null;
			} else {
				centres [0].RemoveConnection (centres [1], false);
				centres [1].Parent = null;
			}
		}

		foreach (RoomNode center in centres) {
			ApplyNewCenter (center);
		}
	}

	public void RestoreTree(){
		foreach (RoomNode subNode in modifiedNodes) {
			subNode.Restore ();
		}
	}

	private void SaveTree(RoomNode node){
		node.Save ();
		modifiedNodes.Add (node);
		foreach (RoomNode subNode in node.Connections) {
			SaveTree (subNode);
		}
	}
}