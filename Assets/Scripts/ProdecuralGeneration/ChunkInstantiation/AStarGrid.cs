using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public struct GridPosition{
	public float x;
	public float y;
	public bool shiftedX;
	public bool shiftedY;
	private bool isAccessible;
	private int roomID;
	private int doorID;

	public GridPosition (float x, float y){
		this.x = x;
		this.y = y;
		shiftedX = false;
		shiftedY = false;
		isAccessible = true;
		roomID = -1;
		doorID = -1;
	}

	public Vector2 Position {
		get{ return new Vector2 (x, y); }
	}

	public bool IsAccessible {
		get {
			return this.isAccessible;
		}
		set {
			isAccessible = value;
		}
	}

	public bool HasBeenShifted{
		get{ return shiftedX || shiftedY; }
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
}

public class AStarGrid {

	List<RoomTransformation> rooms;
	private GridPosition[,] grid;
	private Rect availableSpace;
	private List<Vector2> positions;
	private List<Rect> roomRects;
	private List<Vector2> doorPositions;
	private List<float> xCoordinates;
	private List<float> yCoordinates;
	private float padding;
	private float threshold;
	private float gridCellSize;

	public AStarGrid(List<Rect> roomRects, List<RoomTransformation> rooms, float padding){
		this.padding = 4.88f;
		this.threshold = padding / 2f;
		this.gridCellSize = padding / 2f;
		this.roomRects = roomRects;
		this.rooms = rooms;
		xCoordinates = new List<float> ();
		yCoordinates = new List<float> ();
		positions = new List<Vector2> ();
		doorPositions = new List<Vector2> ();
		CalculateSpace();
		BuildGrid ();
		SortOut ();
		//FindDoorCandidates ();
		Shift ();
	}

	private void FindDoorCandidates(){
		
	}

	private void Shift(){
		foreach (RoomTransformation room in rooms) {
			foreach (DoorDefinition door in room.Doors) {
				int x = (int)Mathf.Round((door.Position.x - availableSpace.xMin) / gridCellSize);
				int y = (int)Mathf.Round((door.Position.z - availableSpace.yMin) / gridCellSize);
				grid [x, y].DoorID = door.ID;


			}
		}
	}

	private void SortOut(){
		InflateBy (padding * 0.5f);

		for (int i = 0; i < grid.GetLength (0); i++) {
			for (int j = 0; j < grid.GetLength (1); j++) {
				GridPosition pos = grid [i, j];
				if (pos.IsAccessible) {
					grid [i, j].RoomID = CollidesWithRoom (pos.Position);
				}
			}
		}

		InflateBy (padding * 2f);

		for (int i = 0; i < grid.GetLength (0); i++) {
			for (int j = 0; j < grid.GetLength (1); j++) {
				GridPosition pos = grid [i, j];
				if (pos.IsAccessible) {
					grid [i, j].IsAccessible = !IsFree (pos.Position);
				}
			}
		}

		InflateBy (padding * -2.5f);
	}

	private void InflateBy(float val){
		for(int i = 0; i < roomRects.Count; i++) {
			Rect r = roomRects [i];
			r.yMin -= val;
			r.xMin -= val;
			r.xMax += val;
			r.yMax += val;
			roomRects [i] = r;
		}
	}

	private void BuildGrid(){		
		int xIterations = (int)(availableSpace.width / gridCellSize);
		int yIterations = (int)(availableSpace.height / gridCellSize);

		grid = new GridPosition[xIterations, yIterations];

		for (int i = 0; i < xIterations; i++) {
			for (int j = 0; j < yIterations; j++) {
				Vector2 newPos = new Vector2 (availableSpace.xMin + i * gridCellSize, availableSpace.yMin + j * gridCellSize);
				grid [i, j] = new GridPosition (newPos.x, newPos.y);
			}
		}
	}

	private void CalculateSpace(){
		availableSpace = new Rect (Vector2.zero, Vector2.zero);

		foreach (Rect room in roomRects) {
			availableSpace.xMin = Mathf.Min (availableSpace.xMin, room.xMin);
			availableSpace.yMin = Mathf.Min (availableSpace.yMin, room.yMin);
			availableSpace.xMax = Mathf.Max (availableSpace.xMax, room.xMax);
			availableSpace.yMax = Mathf.Max (availableSpace.yMax, room.yMax);
		}

		availableSpace.yMin -= padding * 2f;
		availableSpace.xMin -= padding * 2f;
		availableSpace.xMax += padding * 2f;
		availableSpace.yMax += padding * 2f;
	}

	private int CollidesWithRoom(Vector2 position){
		int id = 0;
		foreach (Rect rect in roomRects) {
			id++;
			if (rect.Contains (position)) {
				return id;
			}
		}
		return 0;
	}

	private bool IsFree(Vector2 position){
		return CollidesWithRoom (position) == 0;
	}

	public GridPosition[,] Grid {
		get {
			return this.grid;
		}
	}
}
