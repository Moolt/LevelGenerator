using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum NodeType{ START, END, MIDDLE, SIDE }

public class RoomNode{
	protected List<RoomNode> connections;
	protected bool isCriticalPath;
	protected int doorCount = 0;
	protected RoomNode parent;
	//Used by the free tree algorithm to find the centre of the graph
	//Don't use it to acutally get the amount of children
	protected int childrenCount = -1;
	protected int depth = -1;
	protected bool marked = false;
	protected Vector3 position;
	protected NodeType nodeType;
	private RoomNode safeState;

	public static int id_ = 0;
	private int id;

	public RoomNode(bool isCriticalPath, NodeType nodeType){
		this.nodeType = nodeType;
		connections = new List<RoomNode> ();
		this.isCriticalPath = isCriticalPath;
		id = id_++;
	}

	private void CopyValues(RoomNode source, RoomNode dest){
		dest.connections = new List<RoomNode> (source.connections);
		dest.isCriticalPath = source.isCriticalPath;
		dest.doorCount = source.doorCount;
		dest.parent = source.parent;
		dest.childrenCount = source.childrenCount;
		dest.nodeType = source.nodeType;
		//dest.depth = source.depth;
		//dest.marked = source.marked;
		//Dont restore position
	}

	public void Save(){
		safeState = new RoomNode (true, NodeType.MIDDLE);
		CopyValues (this, safeState);
	}

	public void Restore(){
		if (safeState != null) {			
			CopyValues (safeState, this);
			safeState = null;
		}
	}

	public bool IsCriticalPath {
		get {
			return this.isCriticalPath;
		}
	}

	public List<RoomNode> Connections {
		get {
			return this.connections;
		}
	}

	public void AddConnection(RoomNode otherNode){
		AddConnection (otherNode, true);
	}

	public void RemoveConnection(RoomNode otherNode){
		RemoveConnection (otherNode, true);
	}

	public void AddConnection(RoomNode otherNode, bool changeParent){
		connections.Add (otherNode);
		if (changeParent) {
			otherNode.parent = this;
		}
		otherNode.IncreaseDoorCount();
	}

	public void RemoveConnection(RoomNode otherNode, bool changeParent){
		connections.Remove (otherNode);
		if (changeParent) {
			otherNode.parent = null;
		}
		otherNode.DecreaseDoorCount();
	}

	public void IncreaseDoorCount(){
		doorCount++;
	}

	public void DecreaseDoorCount(){
		doorCount--;
	}

	public int ID{
		get{ return id; }
	}

	public int DoorCount {
		get {
			return this.doorCount + connections.Count;
		}
	}

	public RoomNode Parent {
		get {
			return this.parent;
		}
		set {
			parent = value;
		}
	}

	public int ChildrenCount {
		get {
			//Init first
			if (childrenCount == -1) {
				childrenCount = connections.Count;
				childrenCount += parent != null ? 1 : 0;
			}
			return this.childrenCount;
		}
		set {
			if (value == -1) {
				childrenCount = connections.Count;
				childrenCount += parent != null ? 1 : 0;
			}
			childrenCount = value;
		}
	}

	public int Depth {
		get {
			return this.depth;
		}
		set {
			depth = value;
		}
	}

	public bool Marked {
		get {
			return this.marked;
		}
		set {
			marked = value;
		}
	}

	public Vector3 Position {
		get {
			return this.position;
		}
		set {
			position = value;
		}
	}

	public NodeType NodeType {
		get {
			return this.nodeType;
		}
	}
}