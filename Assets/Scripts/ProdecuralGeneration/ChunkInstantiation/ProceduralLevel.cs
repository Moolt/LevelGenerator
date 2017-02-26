using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RoomTransformation{
	protected List<DoorDefinition> availableDoors;

	private List<DoorDefinition> doors;
	private float inflation = 0f;
	private Vector3 chunkSize;
	private Vector3 position;
	private GameObject chunk;
	private Rect rect;

	public RoomTransformation (GameObject chunk, Vector3 position, Vector3 chunkSize, float inflation){
		this.chunk = chunk;
		this.position = position;
		this.chunkSize = chunkSize;
		this.rect = CalculateRect ();
		InflateRectBy (inflation);
		this.doors = ObtainDoors ();
		this.availableDoors = ObtainDoors ();
	}

	private Rect CalculateRect(){
		Vector2 rectPosition = new Vector2 (position.x - chunkSize.x / 2f, position.z - chunkSize.z / 2f);
		Vector2 size = new Vector2 (chunkSize.x, chunkSize.z);
		return new Rect (rectPosition, size);
	}

	private List<DoorDefinition> ObtainDoors(){
		DoorManager doorManager = chunk.GetComponent<DoorManager> ();
		if (doorManager != null) {
			return new List<DoorDefinition>(doorManager.RandomDoors);
		}
		return new List<DoorDefinition> ();
	}

	public void UpdateDoorPositions(){
		foreach (DoorDefinition door in doors) {
			door.Position = new Vector3 (rect.center.x, 0f, rect.center.y) + door.RelPosition;
		}
	}

	public void InflateRectBy(float amount){
		inflation += amount;
		rect.xMin -= amount / 2f;
		rect.yMin -= amount / 2f;
		rect.xMax += amount / 2f;
		rect.yMax += amount / 2f;
	}

	public void DeflateRect(){
		rect.xMin += inflation / 2f;
		rect.yMin += inflation / 2f;
		rect.xMax -= inflation / 2f;
		rect.yMax -= inflation / 2f;
		inflation = 0f;
	}

	public void UpdateRect(Vector2 position, Vector2 size){
		rect.position = position;
		rect.size = size;
	}

	public GameObject Chunk {
		get {
			return this.chunk;
		}
	}

	public Rect Rect {
		get {
			return this.rect;
		}
	}

	public HallwayMeta FindMatchingDoors(RoomTransformation otherRoom){
		float distance = float.MaxValue;
		Vector3 thisPosition = new Vector3 (rect.x, 0f, rect.y);
		Vector3 otherPosition = new Vector3 (otherRoom.Rect.x, 0f, otherRoom.Rect.y);
		HallwayMeta hallwayMeta = new HallwayMeta (null, null);

		foreach (DoorDefinition thisDoor in availableDoors) {
			foreach (DoorDefinition otherDoor in otherRoom.availableDoors) {
				float tmpDistance = Vector3.Distance (thisDoor.RelPosition + thisPosition, otherDoor.RelPosition + otherPosition);
				if (tmpDistance < distance) {
					hallwayMeta.StartDoor = thisDoor;
					hallwayMeta.EndDoor = otherDoor;
					distance = tmpDistance;
				}
			}
		}
			
		availableDoors.Remove (hallwayMeta.StartDoor);
		otherRoom.availableDoors.Remove (hallwayMeta.EndDoor);
		return hallwayMeta;
	}

	public List<DoorDefinition> Doors {
		get {
			return this.doors;
		}
	}
}

public class ProceduralLevel{
	private DebugData debugData;
	private RoomNode rootnode; //Rootnode of the level graph
	private ChunkHelper helper; //Helps searching Chunks
	private ChunkInstantiator chunkInstantiator;
	private int tmpChunkPos = -10000; //Temporary variable used for instantiating chunks at position
	private List<RoomTransformation> positionMeta;
	private List<HallwayMeta> hallwayMeta;
	private float spacing; //Space between rooms, used is separation is active
	private float distance; //Distance used in the FreeTree algorithm
	private bool isSeparate; //Separate rooms, avoid overlapping
	private int doorSize;

	public ProceduralLevel(string path, LevelGraph graph, bool separateRooms, float distance, bool isSeparate, float spacing, int doorSize){
		this.rootnode = graph.Rootnode;
		this.helper = new ChunkHelper (path);
		this.distance = distance;
		this.spacing = spacing;
		this.isSeparate = isSeparate;
		this.positionMeta = new List<RoomTransformation>();
		this.hallwayMeta = new List<HallwayMeta> ();
		this.doorSize = doorSize;
		this.debugData = new DebugData ();
		chunkInstantiator = ChunkInstantiator.Instance;

		GenerateLevel (graph);
		ApplyTransformation();
		CreateHallways ();
	}

	//Creates a visual representation of the level graph using a free tree algorithm
	//The positions are written to the node instances
	private void GenerateLevel(LevelGraph graph){
		FreeTreeVisualization freeTree = new FreeTreeVisualization(distance * 10f, graph.NodesCreated, graph.Rootnode);
		freeTree.FreeTree ();
		GenerateLevel(rootnode, null);

		if (isSeparate) {
			SeparateRooms ();
		}
	}

	private void GenerateLevel(RoomNode node, RoomTransformation prevChunk){
		//Place the Chunk somewhere, where it won't collide with another chunk
		GameObject chunk = InstantiateChunk (node, new Vector3(tmpChunkPos, -100f, -100f));
		tmpChunkPos += 100;
		//Obtain the actual position, the chunk will have later on
		Vector3 chunkSize = ChunkSize (chunk);
		RoomTransformation roomTransform = new RoomTransformation (chunk, node.Position, chunkSize, spacing);
		positionMeta.Add(roomTransform);

		if (prevChunk != null) {
			HallwayMeta hallway = prevChunk.FindMatchingDoors (roomTransform);
			hallwayMeta.Add (hallway);
		}

		foreach (RoomNode subNode in node.Connections) {
			GenerateLevel (subNode, roomTransform);
		}
	}

	private Vector3 ChunkSize(GameObject chunk){
		MeshCollider meshCollider = chunk != null ? chunk.GetComponent<MeshCollider> () as MeshCollider : null;
		return meshCollider != null ? meshCollider.bounds.size : Vector3.zero;
	}

	//Instantiates a Chunk at a certain Position. The node is used to determinde the amount of doors needed.
	private GameObject InstantiateChunk(RoomNode node, Vector3 position){
		GameObject randomChunk = PickRandomChunk (helper.FindChunks (node.DoorCount));
		randomChunk.transform.position = position;
		randomChunk = GameObject.Instantiate (randomChunk); //Instantiate Unity Object
		chunkInstantiator.ProcessType = ProcessType.GENERATE;
		chunkInstantiator.InstantiateChunk (randomChunk, node.DoorCount); //Instantiate Abstract Object
		randomChunk.tag = "ChunkInstance";
		return randomChunk;
	}

	//Determindes chunk candidates using the door amount specified. A random chunk is then picked and returned.
	private GameObject PickRandomChunk(List<GameObject> candidates){
		return candidates [Random.Range (0, candidates.Count)];
	}

	//Applies the Room positions to the instances.
	private void ApplyTransformation(){
		foreach (RoomTransformation transformation in positionMeta) {
			//Usually door's positions are updated during editor time, but not during generation time
			//Since the rooms have been placed and then separated, the door positions have to be recalculated
			//For the hallway generation.
			transformation.UpdateDoorPositions ();
			Vector3 position = new Vector3 (transformation.Rect.center.x, 0f, transformation.Rect.center.y);
			transformation.Chunk.transform.position = position;
		}
	}

	private void CreateHallways(){
		List<Rect> roomRects = GetDeflatedRoomRects ();
		AStarGrid grid = new AStarGrid (roomRects, positionMeta, spacing);
		debugData.Grid = grid.Grid;

		foreach (HallwayMeta hw in hallwayMeta) {
			HallwayAStar routing = new HallwayAStar (roomRects, hw.StartDoor, hw.EndDoor, grid, doorSize);
			debugData.AddPath(routing.BuildPath ());
		}
	}

	private List<Rect> GetDeflatedRoomRects(){
		positionMeta.ForEach (t => t.DeflateRect ());
		List<Rect> rects = positionMeta.Select (t => t.Rect).ToList ();
		return rects;
	}

	//Used by LevelGeneratorWindow to manage (clear) rooms in editor view
	public List<GameObject> GeneratedRooms{
		get{ return positionMeta.Select (c => c.Chunk).ToList (); }
	}

	private void SeparateRooms(){
		bool separated = false;
		do {
			separated = true;

			foreach(RoomTransformation room in positionMeta){
				Vector2 velocity = Vector2.zero;
				Vector2 center = room.Rect.center;

				foreach(RoomTransformation other in positionMeta){
					//No comparison with itself
					if(room == other) continue;
					//Only search for overlapping rectangles
					if(!room.Rect.Overlaps(other.Rect)) continue;

					Vector2 otherCenter = other.Rect.center;
					Vector2 diff = center - otherCenter;
					float diffLen = diff.sqrMagnitude; ///BUGPOTENTIAL

					if(diffLen > 0f){
						float repelDecayCoefficient = 1f;
						float scale = repelDecayCoefficient / diffLen;
						diff.Normalize();
						diff *= scale;
						velocity += diff;
					}
				}

				if(velocity.magnitude > 0f){
					separated = false;

					velocity.Normalize();
					room.UpdateRect(room.Rect.position + velocity, room.Rect.size);
				}
			}

		} while(!separated);
	}

	public DebugData DebugData {
		get {
			return this.debugData;
		}
	}
}