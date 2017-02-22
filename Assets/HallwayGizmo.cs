using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HallwayGizmo : MonoBehaviour {

	public GridPosition[,] grid;
	public List<List<Square>> paths;
	public Rect availableSpace;
	public Rect[] rooms;
	public List<Vector2> walkable;
	public Square start, end;

	void OnEnable(){
		paths = new List<List<Square>> ();
	}

	void OnDrawGizmos(){
		if (paths != null) {
			foreach (List<Square> subPath in paths) {
				if (subPath == null)
					continue;
				bool stripe = true;
				for (int i = 0; i < subPath.Count - 1; i++) {
					Gizmos.color = stripe ? Color.black : Color.white;
					stripe = !stripe;
					Gizmos.DrawLine (AddY (subPath [i].Position, 1f), AddY (subPath [i + 1].Position, 1f));
				}
			}
		}
			
		if (grid != null) {
			for (int i = 0; i < grid.GetLength (0); i++) {
				for (int j = 0; j < grid.GetLength (1); j++) {
					Gizmos.color = grid [i, j].IsAccessible ? Color.white : Color.red;
					Gizmos.color = grid [i, j].DoorID > -1 ? Color.blue : Gizmos.color;
					Gizmos.DrawSphere (new Vector3 (grid[i,j].x, 0f, grid[i,j].y), 0.4f);
				}
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

	public void ResetPaths(){
		paths = new List<List<Square>> ();
	}

	public void AddNewPath (List<Square> newPath){
		paths.Add (newPath);
	}

	private Vector3 AddY(Vector2 vec, float y){
		return new Vector3 (vec.x, y, vec.y);
	}
}
