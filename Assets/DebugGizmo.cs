using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugData{
	GridPosition[,] grid;
	List<List<Square>> paths;

	public DebugData(){
		paths = new List<List<Square>> ();
	}

	public GridPosition[,] Grid {
		get {
			return this.grid;
		}
		set {
			grid = value;
		}
	}

	public List<List<Square>> Paths {
		get {
			return this.paths;
		}
		set {
			paths = value;
		}
	}

	public void AddPath(List<Square> path){
		if (path != null) {
			paths.Add (path);
		}
	}
}

public class DebugInfo{
	private bool showPaths;
	private bool showConnections;
	private bool showAStarGrid;

	public DebugInfo(){
		this.showPaths = false;
		this.showConnections = false;
		this.showAStarGrid = false;
	}

	public bool IsDebugUsed{
		get { return showPaths || showConnections || showAStarGrid; }
	}

	public bool ShowPaths {
		get {
			return this.showPaths;
		}
		set {
			showPaths = value;
		}
	}

	public bool ShowConnections {
		get {
			return this.showConnections;
		}
		set {
			showConnections = value;
		}
	}

	public bool ShowAStarGrid {
		get {
			return this.showAStarGrid;
		}
		set {
			showAStarGrid = value;
		}
	}
}


public class DebugGizmo : MonoBehaviour {
	
	public DebugInfo debugInfo;
	public DebugData debugData;

	void OnDrawGizmos(){
		if (debugInfo != null && debugData != null) {
			
			if (debugInfo.ShowPaths && debugData.Paths != null) {
				foreach (List<Square> subPath in debugData.Paths) {
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

			if (debugInfo.ShowAStarGrid && debugData.Grid != null) {
				for (int i = 0; i < debugData.Grid.GetLength (0); i++) {
					for (int j = 0; j < debugData.Grid.GetLength (1); j++) {
						Gizmos.color = Color.white;
						//Gizmos.color = debugData.Grid [i, j].IsAccessible ? Color.white : Color.red;

						Gizmos.color = debugData.Grid [i, j].MarkedOnce ? Color.cyan : Gizmos.color;
						Gizmos.color = debugData.Grid [i, j].DoorID > -1 ? Color.blue : Gizmos.color;

						if (debugData.Grid [i, j].IsAccessible) {
							Gizmos.DrawSphere (new Vector3 (debugData.Grid [i, j].x, 0f, debugData.Grid [i, j].y), 0.4f);
						}
					}
				}
			}
		}
	}

	private Vector3 AddY(Vector2 vec, float y){
		return new Vector3 (vec.x, y, vec.y);
	}

	public DebugInfo DebugInfo {
		get {
			return this.debugInfo;
		}
		set {
			debugInfo = value;
		}
	}

	public DebugData DebugData {
		get {
			return this.debugData;
		}
		set {
			debugData = value;
		}
	}
}
