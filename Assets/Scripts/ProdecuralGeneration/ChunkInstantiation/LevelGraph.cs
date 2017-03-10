using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

//Generates a graph that is then used by the FreeTreeVisualization to position the nodes and then by Procedural level to instantiate rooms
public class LevelGraph{
	private List<RoomNode> rootnodes;
	private RoomNode rootnode;
	private int roomsCount;
	private int nodesCreated;
	private int critPathLength;
	private int maxDoors;
	private float distribution;

	public LevelGraph(){
		rootnodes = new List<RoomNode> ();
	}

	public void GenerateGraph (int roomsCount, int critPathLength, int maxDoors, float distribution){
		RoomNode.id_ = 0;
		rootnodes.Clear ();
		nodesCreated = 0;

		this.roomsCount = roomsCount;
		this.critPathLength = critPathLength;
		this.maxDoors = maxDoors;
		this.distribution = distribution;

		CreateCriticalPath ();
		ShuffleRootnodes ();
		CreateSideRooms ();
	}

	private void CreateCriticalPath(){
		RoomNode prevNode = new RoomNode (true, NodeType.START);
		rootnode = prevNode;
		rootnodes.Add (rootnode);
		nodesCreated++;

		for (int i = 1; i < critPathLength; i++) {
			NodeType nodeType = i == critPathLength - 1 ? NodeType.END : NodeType.MIDDLE;
			RoomNode newNode = new RoomNode (true, nodeType);
			prevNode.AddConnection (newNode);
			rootnodes.Add (newNode);
			prevNode = newNode;
			nodesCreated++;
		}
	}

	public void PrintGraph(RoomNode node){
		foreach (RoomNode nextNode in node.Connections) {
			string nodeID = node.IsCriticalPath ? node.ID.ToString() + "*" : node.ID.ToString();
			string nextNodeID = nextNode.IsCriticalPath ? nextNode.ID.ToString() + "*" : nextNode.ID.ToString();
			Debug.Log (nodeID + " -> " + nextNodeID);
		}
		foreach (RoomNode nextNode in node.Connections) {
			PrintGraph (nextNode);
		}
	}

	private void ShuffleRootnodes(){
		rootnodes = rootnodes.OrderBy (n => Random.value).ToList ();
	}

	private void CreateSideRooms(){
		int sideRoomCount = roomsCount - critPathLength; //Remaining rooms are side rooms

		//No siderooms to be created, return
		if (sideRoomCount == 0) {
			return;
		}
		//Each rootnode has a certain supply of doors to be created
		//These doors don't have to be used by the rootnode or it's child all at once
		//Since CreateSideRooms will be later called recursively, the supply will be used up until it's zero		
		//int supplyPerNode = (int)Mathf.Ceil ((sideRoomCount / (float)critPathLength) / distribution);

		for (int i = 0; i < critPathLength; i++) {
			int supplyPerNode = (int)(Mathf.Ceil(sideRoomCount / (float)critPathLength)) + (int)(sideRoomCount * Mathf.Clamp (Random.value, 0f, 1 - distribution)); //(int)Mathf.Ceil ((sideRoomCount / (float)critPathLength) / distribution);
			//How many rooms can be created until roomsCount is reached
			int availableRooms = roomsCount - nodesCreated;
			//Has been previously computed. Since ceil was used for rounding, there's the
			//Potential for supplyPerNode to be larger than the amount of availableRooms.
			int sideRoomsSupply = supplyPerNode > availableRooms ? availableRooms : supplyPerNode;
			//Recursively create subNodes
			CreateSideRooms (rootnodes [i], sideRoomsSupply);
		}
	}

	private void CreateSideRooms(RoomNode node, int roomSupply){
		int availableDoors = Mathf.Max (0, maxDoors - node.DoorCount);
		//There's nothing to do if no doors can be created
		//This will prevent endless loops since the roomSupply eventually drains
		//Prevent, that more doors are created than roomsCounts defines
		if (availableDoors == 0 || roomSupply <= 0 || nodesCreated >= roomsCount)
			return;
		//At least one door should be placed, else return
		int min = 1; 
		//Only create as much doors as there are available
		//Don't create more doors than the supply offers
		int max = Mathf.Min (roomSupply, availableDoors); 
		//The amount of rooms to be created
		//Since Range's max is exclusive I have to add 1 in order to make it inclusive
		int roomsCreated = Random.Range (min, max + 1);
		int remainingSupply = roomSupply - roomsCreated;
		//Prevent possible division by zero.
		int supplyPerNode = (remainingSupply > 0) ? (int)Mathf.Ceil (remainingSupply / (float)roomsCreated) : 0;
		//Create new graph nodes, recursively call this function again with the remainingSupply
		for (int i = 0; i < roomsCreated; i++) {
			RoomNode newNode = new RoomNode (false, NodeType.SIDE);
			node.AddConnection (newNode);
			int newNodeSupply = (supplyPerNode > remainingSupply) ? Mathf.Max(0, remainingSupply) : supplyPerNode;
			CreateSideRooms (newNode, newNodeSupply);
			nodesCreated++;
		}
	}

	public List<RoomNode> Nodes {
		get {
			return this.rootnodes;
		}
	}

	public RoomNode Rootnode {
		get {
			return this.rootnode;
		}
	}

	public int NodesCreated {
		get {
			return this.nodesCreated;
		}
		set {
			nodesCreated = value;
		}
	}
}