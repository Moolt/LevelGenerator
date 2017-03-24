using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct RoomMeta{
	public GameObject Chunk;
	public RoomNode Node;

	public RoomMeta (GameObject chunk, RoomNode node){
		this.Chunk = chunk;
		this.Node = node;
	}
}

public class DebugData{
	private GridPosition[,] grid;
	private List<List<Square>> paths;
	private List<RoomMeta> roomMeta;
	private bool aborted;

	public DebugData(){
		paths = new List<List<Square>> ();
		roomMeta = new List<RoomMeta> ();
		aborted = false;
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

	public void AddRoomMeta(GameObject chunk, RoomNode node){
		roomMeta.Add (new RoomMeta (chunk, node));
	}

	public List<RoomMeta> RoomMeta {
		get {
			return this.roomMeta;
		}
	}

	public bool Aborted {
		get {
			return this.aborted;
		}
		set {
			aborted = value;
		}
	}
}

public class DebugInfo{
	private bool showPaths;
	private bool showConnections;
	private bool showAStarGrid;
	private bool showRoomTypes;
	private bool setStatic;

	public DebugInfo(){
		this.showPaths = false;
		this.showConnections = false;
		this.showAStarGrid = false;
		this.setStatic = false;
	}

	public bool IsDebugUsed{
		get { return showPaths || showConnections || showAStarGrid || showRoomTypes; }
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

	public bool ShowRoomTypes {
		get {
			return this.showRoomTypes;
		}
		set {
			showRoomTypes = value;
		}
	}

	public bool SetStatic {
		get {
			return this.setStatic;
		}
		set {
			setStatic = value;
		}
	}
}


public class DebugGizmo : MonoBehaviour {
	
	public DebugInfo debugInfo;
	public DebugData debugData;

	void OnDrawGizmos(){
		if (debugInfo != null && debugData != null && !debugData.Aborted) {
			
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
						Gizmos.color = debugData.Grid [i, j].visitedByAstar ? Color.magenta: Gizmos.color;
						Gizmos.color = debugData.Grid [i, j].DoorID > -1 ? Color.blue : Gizmos.color;

						if (debugData.Grid [i, j].IsAccessible) {
							Gizmos.DrawSphere (new Vector3 (debugData.Grid [i, j].x, 0f, debugData.Grid [i, j].y), 0.4f);
						}
					}
				}
			}

			if (debugData.RoomMeta != null && debugInfo.ShowRoomTypes) {
				foreach (RoomMeta meta in debugData.RoomMeta) {
					Gizmos.color = RoomTypeColor (meta.Node.NodeType);
					Gizmos.DrawSphere (meta.Chunk.transform.position, 1.5f);
					Gizmos.color = Color.white;
				}
			}
		}
	}

	private Color RoomTypeColor(NodeType nodeType){
		switch (nodeType) {
		case NodeType.END:
			return Color.red;
		case NodeType.MIDDLE:
			return Color.blue;
		case NodeType.START:
			return Color.green;
		case NodeType.SIDE:
			return Color.yellow;
		}
		return Color.black;
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
