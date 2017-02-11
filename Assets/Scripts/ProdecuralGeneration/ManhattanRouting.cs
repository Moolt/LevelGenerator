using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;

public class PathSegment{
	private Vector2 fromPos;
	private Vector2 toPos;

	public PathSegment (Vector2 fromPos, Vector2 toPos)
	{
		this.fromPos = fromPos;
		this.toPos = toPos;
	}	

	public Vector2 FromPos {
		get {
			return this.fromPos;
		}
		set {
			fromPos = value;
		}
	}

	public Vector2 ToPos {
		get {
			return this.toPos;
		}
		set {
			toPos = value;
		}
	}
}

public class HallwayPath{
	private List<PathSegment> segments;
	private PathSegment startAnchor;
	private PathSegment endAnchor;

	public void AddSegment(PathSegment segment){
		segments.Add (segment);
	}

	public PathSegment StartAnchor {
		get {
			return this.startAnchor;
		}
		set {
			startAnchor = value;
		}
	}

	public PathSegment EndAnchor {
		get {
			return this.endAnchor;
		}
		set {
			endAnchor = value;
		}
	}
}

public class ManhattanRouting{

	private Rect[] rooms;
	private DoorDefinition startDoor;
	private DoorDefinition endDoor;
	private float padding = 1f;
	private HallwayPath path;

	public ManhattanRouting (Rect[] rooms, DoorDefinition start, DoorDefinition end)
	{
		this.rooms = rooms;
		this.startDoor = start;
		this.endDoor = end;
		this.path = new HallwayPath ();
		BuildPath ();
	}

	private HallwayPath BuildPath(){
		bool finished = false;
		PreparePath ();
		Vector2 currentPosition = path.StartAnchor.ToPos;
		Vector2 endPos = path.EndAnchor.ToPos; //The destination of the path
		bool isHorizontal = IsHorizontal(ClipY(startDoor.Direction)); //Used for alternating the direction

		while (!finished) {
			Vector2 nextPosition = GetNextPoint (currentPosition, endPos, isHorizontal);
			nextPosition = Raycast (currentPosition, nextPosition);
			path.AddSegment(new PathSegment(currentPosition, nextPosition));

			isHorizontal = !isHorizontal; //Alternate the path direction
			currentPosition = nextPosition;
			finished = currentPosition == endPos; //Finished when destination is reached
		}

		path.AddSegment (path.EndAnchor);
		return path;
	}

	private Vector2 GetNextPoint(Vector2 currentPos, Vector2 endPos, bool isHorizontal){
		return isHorizontal ? new Vector2 (endPos.x, currentPos.y) : new Vector2 (currentPos.x, endPos.y);
	}

	private void PreparePath(){
		path.StartAnchor = new PathSegment (ClipY (startDoor.Position), ClipY (startDoor.Position) + ClipY(startDoor.Direction) * padding);
		path.EndAnchor = new PathSegment (ClipY (endDoor.Position), ClipY (endDoor.Position) + ClipY(endDoor.Direction) * padding);
		path.AddSegment (path.StartAnchor);
	}

	private Vector2 Raycast(Vector2 fromPos, Vector2 toPos){
		Vector2 direction = MakeDirection(fromPos, toPos);
		Vector2 size = IsHorizontal (direction) ? new Vector2 (toPos.x - fromPos.x, 1f) : new Vector2 (1f, toPos.y - fromPos.y);
		Rect desiredSpace = new Rect (fromPos, size);
		Vector2 result = toPos;

		foreach (Rect room in rooms) {
			if (room.Overlaps (desiredSpace, true)) {

				Vector2 collisionPoint = GetCollisionAt (fromPos, room, direction);

				if (collisionPoint.magnitude < result.magnitude) {
					result = collisionPoint;
				}
			}
		}
		return result;
	}

	private Vector2 GetCollisionAt(Vector2 fromPos, Rect room, Vector2 direction){
		Vector2 collisionPoint = Vector3.zero;

		if (direction == Vector2.up) {
			collisionPoint = new Vector2 (fromPos.x, room.yMin);
		} else if (direction == Vector2.down) {
			collisionPoint = new Vector2 (fromPos.x, room.yMax);
		} else if (direction == Vector2.left) {
			collisionPoint = new Vector2 (room.xMax, fromPos.y);
		} else if (direction == Vector2.right) {
			collisionPoint = new Vector2 (room.xMin, fromPos.y);
		}

		return collisionPoint;
	}

	private bool IsHorizontal(Vector2 direction){
		return direction == Vector2.left || direction == Vector2.right;
	}

	private Vector2 ClipY(Vector3 vec){
		return new Vector2 (vec.x, vec.z);
	}

	private Vector2 MakeDirection(Vector2 fromPos, Vector2 toPos){
		return (toPos - fromPos).normalized;
	}
}