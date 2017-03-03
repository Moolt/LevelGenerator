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
	private int[] gridPos;
	private Vector2 direction;

	public Square(Vector2 pos, int[] gridPos){
		this.rect = InitRectByCenter (pos);
		estimatedMovementCost = 0f;
		currentMovementCost = 0f;
		parent = null;
		this.gridPos = gridPos;
		direction = Vector2.zero;
	}

	private Rect InitRectByCenter(Vector2 center){
		return new Rect (center - Vector2.one * (size * .5f), Vector2.one * size);
	}

	private Vector2 DetermineDirection(Square newParent){
		int thisX = gridPos [0];
		int thisY = gridPos [1];

		if (newParent.GridX == thisX) {
			return thisY > newParent.GridY ? Vector2.down : Vector2.up;
		} else {
			return thisX > newParent.GridX ? Vector2.left: Vector2.right;
		}
	}

	public float TurningPenalty(Square _parent){
		if (_parent != null) {
			Vector2 testDirection = DetermineDirection (_parent);
			return _parent.direction == testDirection ? 0f : 1f;
		}
		return 0f;
	}

	public float TurningPenalty(){
		return TurningPenalty (parent);
	}

	public float Score{
		get{
			return estimatedMovementCost + currentMovementCost + TurningPenalty (parent);
		}
	}

	public override bool Equals (object obj){
		if (obj == null)
			return false;
		if (ReferenceEquals (this, obj))
			return true;
		if (obj.GetType () != typeof(Square))
			return false;
		Square other = (Square)obj;
		return Position == other.Position;
	}

	public override int GetHashCode (){
		unchecked {
			return Position.GetHashCode ();
		}
	}	

	public override string ToString (){
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
			direction = DetermineDirection (value);
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
		get{ return 1; }
	}

	public int GridX{
		get{ return gridPos [0]; }
	}

	public int GridY{
		get{ return gridPos [1]; }
	}

	public Vector2 Direction {
		get {
			return this.direction;
		}
	}
}

public class HallwayAStar{

	private List<Rect> rooms;
	private DoorDefinition startDoor;
	private DoorDefinition endDoor;
	private int doorSize;
	private float padding;
	private List<Square> openList;
	private List<Square> closedList;
	private List<Square> finalPath;
	private AStarGrid grid;

	public HallwayAStar (List<Rect> rooms, DoorDefinition start, DoorDefinition end, AStarGrid grid, int doorSize){
		this.rooms = rooms;
		this.startDoor = start;
		this.endDoor = end;
		this.openList = new List<Square> ();
		this.closedList = new List<Square> ();
		this.finalPath = new List<Square> ();
		this.doorSize = doorSize;
		this.padding = doorSize * .5f;
		this.grid = grid;
		Square.Size = padding;
	}

	public List<Square> BuildPath(){
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
				UpdateGrid();
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
					//Read the existing square from the list, don't use the one from the foreach loop
					//Equality is determinded by position. Here we want the old square with all it's other data
					Square existingSquare = openList[openList.IndexOf(adjSquare)];
					float potentialCost = current.CurrentMovementCost + existingSquare.MovementCost + existingSquare.TurningPenalty(current);
					float currentCost = existingSquare.CurrentMovementCost + existingSquare.TurningPenalty();

					if(potentialCost < currentCost){
						existingSquare.CurrentMovementCost = current.CurrentMovementCost + existingSquare.MovementCost;
						existingSquare.Parent = current;
						openList = openList.OrderBy(s => s.Score).ToList(); //Score changed, resort list
					}
				}
			}

		} while(openList.Count > 0);
		return new List<Square> ();
	}

	//Update the grid to contain all adjacent positions. This information will be used to create the hallway mesh
	private void UpdateGrid(){
		for(int i = 0; i < finalPath.Count; i++){
			grid.MarkPositionAsUsed (finalPath [i]);	
			grid.UpdateDirection (finalPath [i]);

			if (i < finalPath.Count - 1) {
				grid.AddAdjacentRelation (finalPath [i], finalPath [i + 1]);
			}
		}
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
		squares.Add(grid.GetSquareInGrid(square.GridX + 1, square.GridY));
		squares.Add(grid.GetSquareInGrid(square.GridX - 1, square.GridY));
		squares.Add(grid.GetSquareInGrid(square.GridX, square.GridY - 1));
		squares.Add(grid.GetSquareInGrid(square.GridX, square.GridY + 1));

		return squares.Where (s => s != null).ToList();
	}

	private Vector2 ClipY(Vector3 vec){
		return new Vector2 (vec.x, vec.z);
	}

	private Square ComputeStartSquare(){
		return grid.SquareAtCoordinate (ClipY(startDoor.Position));
	}
		
	private Square ComputeEndSquare(Square startSquare){
		return grid.SquareAtCoordinate (ClipY(endDoor.Position));
	}
		
	//Good enough solution. a and b can't be tested for equality, since in most cases they will never be equal
	//This A* works on a grid defined by the startPosition and padding. Since the room's positions don't align to this grid,
	//The position of the end door will always be a bit off.
	private bool SquarePositionEquals(Square a, Square b){
		return Vector2.Distance (a.Position, b.Position) < padding; //a.Equals(b);
	}

	private void RoundByVal(ref Vector2 vec, float val){
		vec.x = vec.x < 0 ? Mathf.Ceil (vec.x / val) : Mathf.Floor (vec.x / val);
		vec.y = vec.y < 0 ? Mathf.Ceil (vec.y / val) : Mathf.Floor (vec.y / val);
		vec *= val;
	}
}