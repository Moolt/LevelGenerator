using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;

public class Square{
	protected Rect rect;
	private float estimatedMovementCost;
	private float currentMovementCost;
	private Square parent;
	private static float size;

	public Square(Vector2 pos){
		this.rect = new Rect (pos - Vector2.one * (size * .5f), Vector2.one * size);
		estimatedMovementCost = 0f;
		currentMovementCost = 0f;
		parent = null;
	}

	public float Score{
		get{ return estimatedMovementCost + currentMovementCost; }
	}

	public override bool Equals (object obj)
	{
		if (obj == null)
			return false;
		if (ReferenceEquals (this, obj))
			return true;
		if (obj.GetType () != typeof(Square))
			return false;
		Square other = (Square)obj;
		return Position == other.Position;
	}
	

	public override int GetHashCode ()
	{
		unchecked {
			return Position.GetHashCode ();
		}
	}
	

	public override string ToString ()
	{
		return string.Format ("[Square: Score={0}, Position={1}, CurrentMovementCost={2}, EstimatedMovementCost={3}]", Score, Position, CurrentMovementCost, EstimatedMovementCost);
	}

	public Vector2 Position{
		get{ return rect.center; }
	}

	public static float Size{
		set{ size = value; }
	}

	public Rect Rect {
		get {
			return this.rect;
		}
	}

	public Square Parent {
		get {
			return this.parent;
		}
		set {
			parent = value;
		}
	}

	public float CurrentMovementCost {
		get {
			return this.currentMovementCost;
		}
		set {
			currentMovementCost = value;
		}
	}

	public float EstimatedMovementCost {
		get {
			return this.estimatedMovementCost;
		}
		set {
			estimatedMovementCost = value;
		}
	}
}

public class ManhattanRouting{

	private List<Rect> rooms;
	private DoorDefinition startDoor;
	private DoorDefinition endDoor;
	private float padding = 4.88f;

	private List<Square> openList;
	private List<Square> closedList;
	private Rect availableSpace;

	private Square tmpStart;
	private Square tmpEnd;

	public ManhattanRouting (List<Rect> rooms, DoorDefinition start, DoorDefinition end){
		this.rooms = rooms;
		this.startDoor = start;
		this.endDoor = end;
		this.openList = new List<Square> ();
		this.closedList = new List<Square> ();
		Square.Size = padding;
	}

	public Square BuildPath(){
		ComputeAvailableSpace ();
		Square originalSquare = ComputeStartSquare ();
		Square endSquare = ComputeEndSquare (originalSquare);
		tmpEnd = endSquare;
		tmpStart = originalSquare;
		InsertInOpenSteps (originalSquare);
		Square current = null;

		do {
			current = openList[0]; //Square with lowest fscore
			closedList.Add(current);
			openList.RemoveAt(0);

			if(SquarePositionEquals(current, endSquare)){
				openList.Clear();
				return current;
			}

			List<Square> adjacentSquares = AdjacentSquares(current);
			foreach(Square adjSquare in adjacentSquares){

				if(closedList.Contains(adjSquare)) continue;

				if(!openList.Contains(adjSquare)){
					adjSquare.Parent = current;
					adjSquare.CurrentMovementCost = current.CurrentMovementCost + 1;
					adjSquare.EstimatedMovementCost = ComputeHScore(adjSquare, endSquare);
					InsertInOpenSteps(adjSquare);
				} else {
					Square existingSquare = openList[openList.IndexOf(adjSquare)];

					if(current.CurrentMovementCost + 1 < existingSquare.CurrentMovementCost){
						existingSquare.CurrentMovementCost = current.CurrentMovementCost + 1;
						openList = openList.OrderBy(s => s.Score).ToList(); //Score changed, resort list
					}
				}
			}

		} while(openList.Count > 0);
		return null;
	}

	private void InsertInOpenSteps(Square step){
		openList.Add (step);
		openList = openList.OrderBy (s => s.Score).ToList ();
	}

	private float ComputeHScore(Square start, Square end){
		float width = Mathf.Abs(Mathf.Max (end.Position.x, start.Position.x) - Mathf.Min (end.Position.x, start.Position.x));
		float height = Mathf.Abs(Mathf.Max (end.Position.y, start.Position.y) - Mathf.Min (end.Position.y, start.Position.y));
		return width + height;
	}

	private List<Square> AdjacentSquares(Square square){
		List<Square> squares = new List<Square> ();
		squares.Add(new Square (square.Position + Vector2.up * padding));
		squares.Add(new Square (square.Position + Vector2.left * padding));
		squares.Add(new Square (square.Position + Vector2.down * padding));
		squares.Add(new Square (square.Position + Vector2.right * padding));
		return squares.Where (s => IsWalkable (s)).ToList();
	}

	private bool IsWalkable(Square square){
		foreach (Rect room in rooms) {
			if (square.Rect.Overlaps (room, true)) {
				return false;
			}
		}
		return availableSpace.Contains(square.Rect.center);
	}

	public List<Vector2> WalkableTest(){
		ComputeAvailableSpace ();
		List<Vector2> walkable = new List<Vector2> ();
		float margin = 200f;
		float x = availableSpace.xMin - margin;
		float y = availableSpace.yMin - margin;
		while (x < availableSpace.xMax + margin) {
			y = availableSpace.yMin - margin;
			x += padding;
			while (y < availableSpace.yMax + margin) {
				y += padding;
				Square newSquare = new Square (new Vector2 (x, y));
				if (IsWalkable (newSquare)) {
					walkable.Add (newSquare.Position);
				}
			}
		}
		return walkable;
	}

	private Vector2 ClipY(Vector3 vec){
		return new Vector2 (vec.x, vec.z);
	}

	private Square ComputeStartSquare(){
		Vector2 startSquarePosition = ClipY (startDoor.Position) + ClipY (startDoor.Direction) * padding / 2f;
		return new Square (startSquarePosition);
	}

	//Since the algorithm works on a grid, the end square has to be aligned accordingly
	private Square ComputeEndSquare(Square startSquare){
		Vector2 endSquarePosition = ClipY (endDoor.Position);
		//endSquarePosition.x = endSquarePosition.x - mod(endSquarePosition.x, padding) + mod(startSquare.Position.x, padding);
		//endSquarePosition.y = endSquarePosition.y - mod(endSquarePosition.y, padding) + mod(startSquare.Position.y, padding);
		endSquarePosition += padding * ClipY (endDoor.Direction);

		return new Square (endSquarePosition);
	}

	private float mod(float x, float m){
		return x < 0 ? -(Mathf.Abs(x) % m) : x % m;
	}

	//CHANGE LATER
	private bool SquarePositionEquals(Square a, Square b){
		return Vector2.Distance (a.Position, b.Position) < padding; //a.Equals(b);
	}

	private void ComputeAvailableSpace(){
		float x = Mathf.Min (startDoor.Position.x, endDoor.Position.x);
		float y = Mathf.Min (startDoor.Position.z, endDoor.Position.z);
		float width = Mathf.Max (startDoor.Position.x, endDoor.Position.x) - x;
		float height = Mathf.Max (startDoor.Position.z, endDoor.Position.z) - y;
		availableSpace = new Rect (x, y, width, height);
		availableSpace.yMin -= padding * 2;
		availableSpace.yMax += padding * 2;
		availableSpace.xMin -= padding * 2;
		availableSpace.xMax += padding * 2;
	}

	public Rect AvailableSpace{
		get { return availableSpace; }
	}

	public Square TmpStart {
		get {
			return this.tmpStart;
		}
	}

	public Square TmpEnd {
		get {
			return this.tmpEnd;
		}
	}
}