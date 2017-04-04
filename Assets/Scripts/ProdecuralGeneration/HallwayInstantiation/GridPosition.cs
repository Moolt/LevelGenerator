using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class GridPosition{
	private Dictionary<Vector2,GridPosition> adjacentPositions; //Only relevant for hallway mesh
	private bool usedByHallwayTemplate = false;
	public bool visitedByAstar = false;
	private bool shouldBeEmpty = false;
	private static AStarGrid grid;
	private Vector3 doorDirection; //Only relevant for hallway mesh
	private bool isPartOfPath;
	private Vector2 direction; //hallway mesh
	private bool isAccessible;
	private int roomID;
	private int doorID;
	public float x;
	public float y;
	public int i; 
	public int j;

	public GridPosition (float x, float y){
		InitAdjacentDict ();
		isPartOfPath = false;
		doorDirection = Vector3.zero;
		direction = Vector2.zero;
		this.x = x;
		this.y = y;
		isAccessible = true;
		roomID = -1;
		doorID = -1;
	}

	private void InitAdjacentDict(){
		adjacentPositions = new Dictionary<Vector2, GridPosition> ();
		adjacentPositions.Add (Vector2.up, null);
		adjacentPositions.Add (Vector2.right, null);
		adjacentPositions.Add (Vector2.down, null);
		adjacentPositions.Add (Vector2.left, null);
	}

	public Vector2 Position {
		get{ return new Vector2 (x, y); }
		set{
			x = value.x;
			y = value.y;
		}
	}

	public bool IsAccessible {
		get {
			return this.isAccessible;
		}
		set {
			isAccessible = value;
		}
	}

	public int RoomID {
		get {
			return this.roomID;
		}
		set {
			isAccessible = value == 0;
			roomID = value;
		}
	}

	public int DoorID {
		get {
			return this.doorID;
		}
		set {
			doorID = value;
		}
	}

	public void AddAdjacent(Vector2 direction, GridPosition adjacent){
		if (direction != Vector2.zero) {
			adjacentPositions [direction] = adjacent;
		}
	}

	public Vector3 DoorDirection {
		get {
			return this.doorDirection;
		}
		set {
			doorDirection = value;
		}
	}

	public Dictionary<Vector2, GridPosition> AdjacentPositions {
		get {
			return this.adjacentPositions;
		}
	}

	public Vector2 Direction {
		get {
			if (direction == Vector2.zero || doorID > -1) {
				direction = new Vector2 (doorDirection.x, doorDirection.z);
			}
			return this.direction;
		}
		set {
			direction = value;
		}
	}

	public bool IsDoor{
		get{
			return  doorID > -1;
		}
	}

	public bool IsPartOfPath {
		get {
			return this.isPartOfPath;
		}
		set {
			isPartOfPath = value;
		}
	}

	public static AStarGrid Grid {
		get {
			return grid;
		}
		set {
			grid = value;
		}
	}

	public bool UsedByHallwayTemplate {
		get {
			return this.usedByHallwayTemplate;
		}
		set {
			usedByHallwayTemplate = value;
		}
	}

	public bool ShouldBeEmpty {
		get {
			return this.shouldBeEmpty;
		}
		set {
			shouldBeEmpty = value;
		}
	}
}