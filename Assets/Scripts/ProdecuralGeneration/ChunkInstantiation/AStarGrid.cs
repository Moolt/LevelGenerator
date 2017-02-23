using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GridPosition{
	public float x;
	public float y;
	public bool shiftedX;
	public bool shiftedY;
	private bool isAccessible;
	private bool markedOnce;
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

	public bool HasBeenShifted{
		get{ return shiftedX || shiftedY; }
	}

	public bool HasBeenShiftedDir(int direction){
		return direction == 1 && shiftedX || direction == 0 && shiftedY;
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

	public void UnmarkShifted(int direction){
		shiftedX = direction == 1 ? false : shiftedX;
		shiftedY = direction == 0 ? false : shiftedY;
	}

	//0 = horizontal
	//1 = vertical
	public void Shift(Vector2 shift, int direction){
		if (direction == 1) {
			x = shift.x;
			shiftedX = true;
		} else {
			y = shift.y;
			shiftedY = true;
		}
		markedOnce = true;
	}

	public bool MarkedOnce {
		get {
			return this.markedOnce;
		}
	}
}

public class AStarGrid {

	List<RoomTransformation> rooms;
	private GridPosition[,] grid;
	private Rect availableSpace;
	private List<Rect> roomRects;
	private float padding;
	private float threshold;
	private float gridCellSize;

	public AStarGrid(List<Rect> roomRects, List<RoomTransformation> rooms, float padding){
		this.padding = 4.88f;
		this.threshold = padding / 2f;
		this.gridCellSize = padding / 3f;
		this.roomRects = roomRects;
		this.rooms = rooms;
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
				grid [x, y].IsAccessible = true;
				int gridRoomID = grid [x, y].RoomID;

				grid [x, y].Position = new Vector2 (door.Position.x, door.Position.z);
				int[] interval = GetInterval(x,y, ClipY(door.Direction));

				bool shiftFinished = false;
				int i = interval[0];
				Stack<GridPosition> visitedPositions = new Stack<GridPosition> ();

				while (!shiftFinished) {
					//Determine whether algorithm iterates horizontally ([2] = 0) or vertically ([2] = 1)
					GridPosition gridElement = interval [2] == 0 ? grid [i, y] : grid [x, i];
					visitedPositions.Push (gridElement);
					//Inaccessable and not inside of a room
					bool isOutsideLevel = !gridElement.IsAccessible && gridElement.RoomID == -1;
					bool isInOtherRoom = !gridElement.IsAccessible && gridElement.RoomID != gridRoomID;
					bool hasBeenShifted = gridElement.HasBeenShiftedDir (interval [2]);
					shiftFinished |= isOutsideLevel || isInOtherRoom || hasBeenShifted;

					if (isInOtherRoom) {
						int elementsToUnmark = visitedPositions.Count / 2;
						for (int j = 0; j < elementsToUnmark; j++) {
							visitedPositions.Pop ().UnmarkShifted (interval [2]);
						}
					}

					if (shiftFinished) {
						break;
					}

					gridElement.IsAccessible = true;
					gridElement.Shift (ClipY(door.Position), interval[2]);

					i += interval [0] > interval [1] ? -1 : 1;
					shiftFinished |= i == interval [1];
				}
			}
		}
	}

	private int[] GetInterval(int x, int y, Vector2 direction){
		int[] interval = new int[3];

		if (direction == Vector2.left) {
			interval [0] = x;
			interval [1] = -1;
			interval [2] = 0;
		} else if (direction == Vector2.right) {
			interval [0] = x;
			interval [1] = grid.GetLength(0);
			interval [2] = 0;
		}
		else if (direction == Vector2.up) {
			interval [0] = y;
			interval [1] = grid.GetLength(1);
			interval [2] = 1;
		}
		else if (direction == Vector2.down) {
			interval [0] = y;
			interval [1] = -1;
			interval [2] = 1;
		}
		return interval;
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

	public Square SquareAtCoordinate(Vector2 position){
		int[] pos = new int[2];
		pos[0] = (int)Mathf.Round((position.x - availableSpace.xMin) / gridCellSize);
		pos[1] = (int)Mathf.Round((position.y - availableSpace.yMin) / gridCellSize);
		Square square = new Square (position, pos);
		return square;
	}

	public Square GetSquareInGrid(int i, int j){
		//Return null, if the index is out of bounds or the element is set to be unaccessable
		if (i < 0 || i > grid.GetLength (0) - 1 || j < 0 || j > grid.GetLength (1) - 1) {
			return null;
		}
		if (!grid [i, j].IsAccessible) {
			return null;
		}
		return new Square (grid [i, j].Position, new int[]{ i, j });
	}

	private Vector2 ClipY(Vector3 vec){
		return new Vector2(vec.x, vec.z);
	}
}
