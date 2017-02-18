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
	private bool isDiagonal;

	public Square(Vector2 pos, bool isDiagonal){
		this.rect = InitRectByCenter (pos);
		this.isDiagonal = isDiagonal;
		estimatedMovementCost = 0f;
		currentMovementCost = 0f;
		parent = null;
	}

	private Rect InitRectByCenter(Vector2 center){
		return new Rect (center - Vector2.one * (size * .5f), Vector2.one * size);
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
		set{ this.rect = InitRectByCenter (value); }
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

	public int MovementCost{
		get{ return isDiagonal ? 14 : 10; }
	}
}

public class HallwayAStar{

	private List<Rect> rooms;
	private DoorDefinition startDoor;
	private DoorDefinition endDoor;
	private float padding = 4.88f;

	private List<Square> openList;
	private List<Square> closedList;
	private List<Square> finalPath;
	private Rect availableSpace;

	public HallwayAStar (List<Rect> rooms, DoorDefinition start, DoorDefinition end){
		this.rooms = rooms;
		this.startDoor = start;
		this.endDoor = end;
		this.openList = new List<Square> ();
		this.closedList = new List<Square> ();
		this.finalPath = new List<Square> ();
		Square.Size = padding;
	}

	public List<Square> BuildPath(){
		ComputeAvailableSpace ();
		Square originalSquare = ComputeStartSquare ();
		Square endSquare = ComputeEndSquare (originalSquare);
		InsertInOpenSteps (originalSquare);
		Square current = null;

		do {
			current = openList[0]; //Square with lowest fscore
			closedList.Add(current);
			openList.RemoveAt(0);

			//Path found
			if(SquarePositionEquals(current, endSquare)){
				Square tmp = current;
				openList.Clear();
				//Build a list that contains the final path
				//Has to be interverd later on, since we start with the last element
				while(tmp != null){
					finalPath.Add(tmp);
					tmp = tmp.Parent;
				}
				finalPath.Reverse();
				//FixStartEndPositions();
				return finalPath;
			}

			List<Square> adjacentSquares = AdjacentSquares(current);
			foreach(Square adjSquare in adjacentSquares){

				if(closedList.Contains(adjSquare)) continue;

				if(!openList.Contains(adjSquare)){
					adjSquare.Parent = current;
					adjSquare.CurrentMovementCost = current.CurrentMovementCost + current.MovementCost;
					adjSquare.EstimatedMovementCost = ComputeHScore(adjSquare, endSquare);
					InsertInOpenSteps(adjSquare);
				} else {
					Square existingSquare = openList[openList.IndexOf(adjSquare)];

					if(current.CurrentMovementCost + existingSquare.MovementCost < existingSquare.CurrentMovementCost){
						existingSquare.CurrentMovementCost = current.CurrentMovementCost + existingSquare.MovementCost;
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
		squares.Add(new Square (square.Position + Vector2.up * padding, false));
		squares.Add(new Square (square.Position + Vector2.left * padding, false));
		squares.Add(new Square (square.Position + Vector2.down * padding, false));
		squares.Add(new Square (square.Position + Vector2.right * padding, false));

		squares.Add(new Square (square.Position + (Vector2.up + Vector2.left) * padding, true));
		squares.Add(new Square (square.Position + (Vector2.up + Vector2.right) * padding, true));
		squares.Add(new Square (square.Position + (Vector2.down + Vector2.left) * padding, true));
		squares.Add(new Square (square.Position + (Vector2.down + Vector2.right) * padding, true));

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

	private Vector2 ClipY(Vector3 vec){
		return new Vector2 (vec.x, vec.z);
	}

	//The square at the position of the first room's door. It is not placed directly at the doors position but one unit (padding)
	//In front of it. In case the path is perpendicular to the door's direction, this will ensure that a hallway will always begin facing the
	//Direction of the door.
	private Square ComputeStartSquare(){
		Vector2 startSquarePosition = ClipY (startDoor.Position) + ClipY (startDoor.Direction) * padding * 1f;
		return new Square (startSquarePosition, false);
	}

	//Since the algorithm works on a grid, the end square has to be aligned accordingly
	private Square ComputeEndSquare(Square startSquare){
		Vector2 endSquarePosition = ClipY (endDoor.Position);
		endSquarePosition += padding * ClipY (endDoor.Direction);

		return new Square (endSquarePosition, false);
	}
		
	//Good enough solution. a and b can't be tested for equality, since in most cases they will never be equal
	//This A* works on a grid defined by the startPosition and padding. Since the room's positions don't align to this grid,
	//The position of the end door will always be a bit off.
	private bool SquarePositionEquals(Square a, Square b){
		return Vector2.Distance (a.Position, b.Position) < padding; //a.Equals(b);
	}

	//An area that spans from start to end door (with a margin) in order to constraint the algorithms space
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

	//Fix the positions, since they may no be aligned to the grid
	private void FixStartEndPositions(){
		Square startSquare = new Square (ClipY(startDoor.Position), false);
		finalPath.Insert (0, startSquare);
		int index = finalPath.Count - 1;

		while (index > 0) {
			Square last = finalPath [index];
			Square secondToLast = finalPath [index - 1];
			Vector2 pathDirection = (last.Position - secondToLast.Position).normalized;

			//Dot Product is 0 when the two vectors are perpendicular to each other
			//If they are, the last point has to be aligned to the x or y position of the door
			if (Vector2.Dot (pathDirection, ClipY (endDoor.Direction)) == 0) {
				//Vector2 lastPosCoordinate = Vector2.Scale (last.Position, pathDirection);
				//Vector2 endDoorCoordinate = Vector2.Scale (ClipY (endDoor.Position), pathDirection);
				Vector2 delta = last.Position - ClipY (endDoor.Position);
				delta = pathDirection == Vector2.right || pathDirection == Vector2.up ? -delta : delta;
				if (delta.magnitude > padding) {
					last.Position += Vector2.Scale (delta, pathDirection);
				}
				break;
			} else { //Not perpendicular, so it's a straigth hallway. Align the hallway.				
				Vector2 sameHeightAsDoor = last.Position + Vector2.Scale(ClipY (endDoor.Position) - last.Position, pathDirection);
				Vector2 perpendicularVec = ClipY (endDoor.Position) - sameHeightAsDoor;
				float delta = perpendicularVec.magnitude;
				perpendicularVec.Normalize ();
				last.Position += Vector2.Scale (Vector2.one * delta, perpendicularVec);
			}
			index--;
		}

		Square endSquare = new Square (ClipY(endDoor.Position), false);
		finalPath.Add (endSquare);
	}

	public Rect AvailableSpace{
		get { return availableSpace; }
	}
}