using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HallwayGizmo : MonoBehaviour {

	public Square square;
	public Rect availableSpace;
	public Rect[] rooms;
	public List<Vector2> walkable;
	public Square start, end;

	void OnDrawGizmos(){
		if (square != null) {
			Square current = square;
			bool stripe = true;
			while (current != null && current.Parent != null) {
				Gizmos.color = stripe ? Color.black : Color.white;
				stripe = !stripe;
				Gizmos.DrawLine (AddY (current.Position, 1f), AddY (current.Parent.Position, 1f));
				current = current.Parent;
			}
		}

		if (availableSpace != null) {
			//Gizmos.DrawCube (AddY(availableSpace.center, -2f), AddY(availableSpace.size, -2f));
		}

		foreach(Rect r in rooms){
			//Gizmos.DrawCube (AddY(r.center, -2f), AddY(r.size, -2f));
		}

		if (walkable != null) {
			foreach (Vector2 s in walkable) {
				//Gizmos.DrawSphere (AddY(s, 0f), 1f);
			}
		}

		if (start != null) {
			Gizmos.color = Color.green;
			Gizmos.DrawSphere (AddY(start.Position, 0f), 1f);
		}

		if (end != null) {
			Gizmos.color = Color.red;
			Gizmos.DrawSphere (AddY(end.Position, 0f), 1f);
		}
	}

	private Vector3 AddY(Vector2 vec, float y){
		return new Vector3 (vec.x, y, vec.y);
	}
}
