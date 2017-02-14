using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HallwayGizmo : MonoBehaviour {

	public List<Square> path;
	public Rect availableSpace;
	public Rect[] rooms;
	public List<Vector2> walkable;
	public Square start, end;

	void OnDrawGizmos(){
		if (path != null) {
			bool stripe = true;
			for (int i = 0; i < path.Count - 1; i++) {
				Gizmos.color = stripe ? Color.black : Color.white;
				stripe = !stripe;
				Gizmos.DrawLine (AddY (path [i].Position, 1f), AddY (path [i + 1].Position, 1f));
			}
		}
			
		//Gizmos.DrawCube (AddY(availableSpace.center, -2f), AddY(availableSpace.size, -2f));

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
			//Gizmos.DrawSphere (AddY(start.Position, 0f), 1f);
		}

		if (end != null) {
			Gizmos.color = Color.red;
			//Gizmos.DrawSphere (AddY(end.Position, 0f), 1f);
		}
	}

	private Vector3 AddY(Vector2 vec, float y){
		return new Vector3 (vec.x, y, vec.y);
	}
}
