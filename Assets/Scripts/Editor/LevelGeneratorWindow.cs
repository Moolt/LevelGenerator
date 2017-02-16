using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class FreeTreeVisualization{
	private float d = 5f;
	private RoomNode rootnode;
	private int nodeAmount;

	public FreeTreeVisualization (float d, int nodeAmount, RoomNode rootnode){
		this.d = d;
		this.nodeAmount = nodeAmount;
		this.rootnode = rootnode;
	}

	public float r(float p){
		return 2f * Mathf.Acos (p / (p + d));
	}

	public void FreeTree(){
		RoomNode[] centre = TreeCentre ();
		if (centre.Length == 1) {
			DrawSubTree (centre [0], 0f, 0f, 2f * Mathf.PI);
		} else {
			DrawSubTree (centre [0], d / 2f, (3 * Mathf.PI) / 2f, Mathf.PI / 2f);
			DrawSubTree (centre [1], d / 2f, Mathf.PI / 2f, Mathf.PI / 2f);
		}
	}

	public void DrawSubTree(RoomNode v, float p, float a1, float a2){
		float s, a;
		v.Position = new Vector3 (p, 0f, (a1 + a2) / 2f);

		if (r (p) < (a2 - a1)) {
			s = r (p) / w (v);
			a = (a1 + a2 - r (p)) / 2f;
		} else {
			s = (a2 - a1) / w (v);
			a = a1;
		}

		foreach (RoomNode u in v.Connections) {
			DrawSubTree (u, p + d, a, a + s * w (u));
			a = a + s * w (u);
		}
	}

	//Subtree width
	public int w(RoomNode v){
		return v.Connections.Count;
	}

	public RoomNode[] TreeCentre(){
		Queue<RoomNode> leaves = new Queue<RoomNode> ();
		int depth = 0;
		FindLeaves (rootnode, leaves, depth++);

		for(int i = 0; i < nodeAmount; i++) {
			if (leaves.Count == 0) {
				FindLeaves (rootnode, leaves, depth++);
			}

			RoomNode leaf = leaves.Dequeue ();
			leaf.Marked = true;

			//The index at which only two nodes remain
			//If their depth is the same, they were added at the same step and
			//Are therefore both the middle
			if (i == nodeAmount - 2) {
				if (leaves.Count == 0) {
					FindLeaves (rootnode, leaves, depth++);
				} else {
					leaves.Enqueue (leaf);
				}
				break;
			}

			if (leaf.Parent != null) {
				leaf.Parent.ChildrenCount -= 1;
			}
		}
		return leaves.ToArray ();
	}

	private void FindLeaves(RoomNode node, Queue<RoomNode> queue, int depth){
		if (node.ChildrenCount < 2 && !node.Marked) {
			node.Depth = depth;
			queue.Enqueue (node);
		} else {
			foreach (RoomNode subNode in node.Connections) {
				FindLeaves (subNode, queue, depth);
			}
		}
	}
}

public class RoomNode{
	private List<RoomNode> connections;
	private bool isCriticalPath;
	private int doorCount = 0;
	private RoomNode parent;
	//Used by the free tree algorithm to find the centre of the graph
	//Don't use it to acutally get the amount of children
	private int childrenCount = -1;
	private int depth = -1;
	private bool marked = false;
	private Vector3 position;

	private static int id_ = 0;
	private int id;

	public RoomNode(bool isCriticalPath){
		connections = new List<RoomNode> ();
		this.isCriticalPath = isCriticalPath;
		id = id_++;
	}

	public bool IsCriticalPath {
		get {
			return this.isCriticalPath;
		}
	}

	public List<RoomNode> Connections {
		get {
			return this.connections;
		}
	}

	public void AddConnection(RoomNode otherNode){
		connections.Add (otherNode);
		otherNode.parent = this;
		otherNode.IncreaseDoorCount();
	}

	public void IncreaseDoorCount(){
		doorCount++;
	}

	public int ID{
		get{ return id; }
	}

	public int DoorCount {
		get {
			return this.doorCount + connections.Count;
		}
	}

	public RoomNode Parent {
		get {
			return this.parent;
		}
	}

	public int ChildrenCount {
		get {
			//Init first
			if (childrenCount == -1) {
				childrenCount = connections.Count;
				childrenCount += parent != null ? 1 : 0;
			}
			return this.childrenCount;
		}
		set {
			if (childrenCount == -1) {
				childrenCount = connections.Count;
				childrenCount += parent != null ? 1 : 0;
			}
			childrenCount = value;
		}
	}

	public int Depth {
		get {
			return this.depth;
		}
		set {
			depth = value;
		}
	}

	public bool Marked {
		get {
			return this.marked;
		}
		set {
			marked = value;
		}
	}

	public Vector3 Position {
		get {
			return this.position;
		}
		set {
			position = value;
		}
	}
}

public class LevelGraph{
	private List<RoomNode> rootnodes;
	private RoomNode rootnode;
	private int roomsCount;
	private int nodesCreated;
	private int critPathLength;
	private int maxDoors;
	private float distribution;

	public LevelGraph(){
		rootnodes = new List<RoomNode> ();
	}

	public void GenerateGraph (int roomsCount, int critPathLength, int maxDoors, float distribution){
		rootnodes.Clear ();
		nodesCreated = 0;

		this.roomsCount = roomsCount;
		this.critPathLength = critPathLength;
		this.maxDoors = maxDoors;
		this.distribution = distribution;

		CreateCriticalPath ();
		ShuffleRootnodes ();
		CreateSideRooms ();
	}

	private void CreateCriticalPath(){
		RoomNode prevNode = new RoomNode (true);
		rootnode = prevNode;
		rootnodes.Add (rootnode);
		nodesCreated++;

		for (int i = 1; i < critPathLength; i++) {
			RoomNode newNode = new RoomNode (true);
			prevNode.AddConnection (newNode);
			rootnodes.Add (newNode);
			prevNode = newNode;
			nodesCreated++;
		}
	}

	private void ShuffleRootnodes(){
		rootnodes = rootnodes.OrderBy (n => Random.value).ToList ();
	}

	private void CreateSideRooms(){
		int sideRoomCount = roomsCount - critPathLength; //Remaining rooms are side rooms

		//No siderooms to be created, return
		if (sideRoomCount == 0) {
			return;
		}
		//Each rootnode has a certain supply of doors to be created
		//These doors don't have to be used by the rootnode or it's child all at once
		//Since CreateSideRooms will be later called recursively, the supply will be used up until it's zero		
		//int supplyPerNode = (int)Mathf.Ceil ((sideRoomCount / (float)critPathLength) / distribution);

		for (int i = 0; i < critPathLength; i++) {
			int supplyPerNode = (int)(Mathf.Ceil(sideRoomCount / (float)critPathLength)) + (int)(sideRoomCount * Mathf.Clamp (Random.value, 0f, 1 - distribution)); //(int)Mathf.Ceil ((sideRoomCount / (float)critPathLength) / distribution);
			//How many rooms can be created until roomsCount is reached
			int availableRooms = roomsCount - nodesCreated;
			//Has been previously computed. Since ceil was used for rounding, there's the
			//Potential for supplyPerNode to be larger than the amount of availableRooms.
			int sideRoomsSupply = supplyPerNode > availableRooms ? availableRooms : supplyPerNode;
			//Recursively create subNodes
			CreateSideRooms (rootnodes [i], sideRoomsSupply);
		}
	}

	private void CreateSideRooms(RoomNode node, int roomSupply){
		int availableDoors = Mathf.Max (0, maxDoors - node.DoorCount);
		//There's nothing to do if no doors can be created
		//This will prevent endless loops since the roomSupply eventually drains
		//Prevent, that more doors are created than roomsCounts defines
		if (availableDoors == 0 || roomSupply <= 0 || nodesCreated >= roomsCount)
			return;
		//At least one door should be placed, else return
		int min = 1; 
		//Only create as much doors as there are available
		//Don't create more doors than the supply offers
		int max = Mathf.Min (roomSupply, availableDoors); 
		//The amount of rooms to be created
		//Since Range's max is exclusive I have to add 1 in order to make it inclusive
		int roomsCreated = Random.Range (min, max + 1);
		int remainingSupply = roomSupply - roomsCreated;
		//Prevent possible division by zero.
		int supplyPerNode = (remainingSupply > 0) ? (int)Mathf.Ceil (remainingSupply / (float)roomsCreated) : 0;
		//Create new graph nodes, recursively call this function again with the remainingSupply
		for (int i = 0; i < roomsCreated; i++) {
			RoomNode newNode = new RoomNode (false);
			node.AddConnection (newNode);
			int newNodeSupply = (supplyPerNode > remainingSupply) ? Mathf.Max(0, remainingSupply) : supplyPerNode;
			CreateSideRooms (newNode, newNodeSupply);
			nodesCreated++;
		}
	}

	public List<RoomNode> Nodes {
		get {
			return this.rootnodes;
		}
	}

	public RoomNode Rootnode {
		get {
			return this.rootnode;
		}
	}

	public int NodesCreated {
		get {
			return this.nodesCreated;
		}
		set {
			nodesCreated = value;
		}
	}
}

public class ChunkMetadata{
	public GameObject chunk;
	public int doors;
}

public class PositionMetadata{
	private GameObject chunk;
	private Rect rect;
	private List<DoorDefinition> doors;
	public PositionMetadata (GameObject chunk, Rect rect)
	{
		this.chunk = chunk;
		this.rect = rect;

		DoorManager doorManager = chunk.GetComponent<DoorManager> ();
		if (doorManager != null) {
			this.doors = doorManager.RandomDoors;
		}
	}

	public GameObject Chunk {
		get {
			return this.chunk;
		}
		set {
			chunk = value;
		}
	}

	public Rect Rect {
		get {
			return this.rect;
		}
		set {
			rect = value;
		}
	}

	public void UpdateDoorPositions(){
		foreach (DoorDefinition door in doors) {
			door.Position = new Vector3 (rect.center.x, 0f, rect.center.y) + door.RelPosition;
		}
	}
}

public class ChunkHelper{

	private List<ChunkMetadata> chunkMetaData;

	public ChunkHelper(string path){
		chunkMetaData = new List<ChunkMetadata> ();
		BuildMetadata (Resources.LoadAll<GameObject> (path));
	}

	private void BuildMetadata(GameObject[] chunks){
		foreach (GameObject chunk in chunks) {
			DoorManager doorManager = chunk.GetComponent<DoorManager> ();
			if (doorManager != null) {
				ChunkMetadata meta = new ChunkMetadata ();
				meta.chunk = chunk;
				meta.doors = doorManager.doors.Count;
				chunkMetaData.Add (meta);
			}
		}
	}

	public List<GameObject> FindChunks(int doorAmount){
		return (from meta in chunkMetaData
		        where meta.doors == doorAmount
		        select meta.chunk).ToList ();
	}

	public int MaxDoors(){
		return 0;
	}
}

public struct LevelGenerationDTO{
	public RoomNode Node;
	public RoomNode PrevNode;
	public Vector3 Direction;
	public Vector3 Position;
	public Vector3 PrevPosition;
	public Vector3 CritPathMeanDir;
	public GameObject PrevChunk;
	public bool AlternatingSideroomDir;
	public int SideRoomIndex;
	public int SideRoomCount;
}

public class ProceduralLevel{
	private RoomNode rootnode;
	private ChunkHelper helper;
	private ChunkInstantiator chunkInstantiator;

	private int tmpChunkPos = -10000;
	private List<PositionMetadata> positionMeta;
	private List<HallwayMeta> hallwayMeta;
	private float spacing;
	private bool showSideRooms;

	public ProceduralLevel(string path, LevelGraph graph, bool separateRooms, float spacing, bool showSideRooms){
		this.rootnode = graph.Rootnode;
		this.helper = new ChunkHelper (path);
		this.spacing = spacing;
		//this.chunkPositions = new Dictionary<GameObject, Rect> ();
		this.positionMeta = new List<PositionMetadata>();
		this.hallwayMeta = new List<HallwayMeta> ();
		this.showSideRooms = showSideRooms;
		chunkInstantiator = ChunkInstantiator.Instance;
		//GenerateLevel (rootnode);
		//if(separateRooms) SeparateRooms();
		FreeTreeVisualization freeTree = new FreeTreeVisualization(10f, graph.NodesCreated, graph.Rootnode);
		RoomNode[] center = freeTree.TreeCentre ();
		//freeTree.FreeTree ();
		//__GenerateLevel(rootnode, null);
		//ApplyPositions();
		//UpdateDoorPositions ();
		//CreateHallways ();
	}

	private void _GenerateLevel(){
		LevelGenerationDTO defaultSettings = new LevelGenerationDTO ();
		defaultSettings.Node = rootnode;
		defaultSettings.PrevNode = null;
		defaultSettings.Direction = Vector3.zero;
		defaultSettings.Position = Vector3.zero;
		defaultSettings.PrevPosition = Vector3.zero;
		defaultSettings.CritPathMeanDir = Vector3.zero;
		defaultSettings.PrevChunk = null;
		defaultSettings.AlternatingSideroomDir = false;
		defaultSettings.SideRoomCount = 0;
		defaultSettings.SideRoomIndex = 0;
		_GenerateLevel (defaultSettings);
	}

	private void __GenerateLevel(RoomNode node, GameObject prevChunk){
		
		GameObject chunk = InstantiateChunk (node, node.Position);
		positionMeta.Add(new PositionMetadata(chunk, new Rect()));
		DebugRoomID roomid = chunk.AddComponent<DebugRoomID> () as DebugRoomID;
		roomid.first = prevChunk;
		roomid.second = chunk;

		foreach (RoomNode subNode in node.Connections) {
			__GenerateLevel (subNode, chunk);
		}
	}

	private void _GenerateLevel(LevelGenerationDTO p){
		Vector3 randomDirection = new Vector3 (-1f + Random.value * 2f, 0f, -1f + Random.value * 2f).normalized * 0.8f;
		Vector3 routeDirection;

		if (p.Node.IsCriticalPath) {
			randomDirection *= p.Node.IsCriticalPath ? 1f : 0.4f;
			routeDirection = p.Direction == Vector3.zero ? randomDirection : p.Direction + randomDirection;
		} else {
			if (p.PrevNode != null && !p.PrevNode.IsCriticalPath) {
				float degree = p.SideRoomCount * 10f;
				float progress = p.SideRoomIndex / p.SideRoomCount;
				Vector3 sideRoomDirection = p.SideRoomCount < 2 ? randomDirection : Quaternion.AngleAxis (-degree + degree * progress, Vector3.up) * p.Direction + randomDirection * 0.1f;
				routeDirection = (randomDirection * 0.4f + p.Direction).normalized;
			} else {
				float degree = p.AlternatingSideroomDir ? 80f : -80f;
				Vector3 perpendicularDir = Quaternion.AngleAxis (degree, Vector3.up) * (p.Position - p.PrevPosition);
				Vector3 meanDir = Quaternion.AngleAxis (degree, Vector3.up) * p.CritPathMeanDir;
				routeDirection = (perpendicularDir - meanDir).normalized;//+ randomDirection * 0.2f;
				p.AlternatingSideroomDir = !p.AlternatingSideroomDir;
			}
		}
		routeDirection.Normalize ();

		Vector3 previousSize = Vector3.Scale(ChunkSize (p.PrevChunk), routeDirection);
		Vector3 computedPosition = p.Position + routeDirection * (spacing + previousSize.magnitude);

		GameObject chunk = InstantiateChunk (p.Node, computedPosition);
		positionMeta.Add(new PositionMetadata(chunk, new Rect()));
		DebugRoomID roomid = chunk.AddComponent<DebugRoomID> () as DebugRoomID;
		roomid.first = p.PrevChunk;
		roomid.second = chunk;

		//roomRect = new Rect (new Vector2(0f, 0f), new Vector2(roomBounds.size.x, roomBounds.size.z) * spacing);
		//positionMeta.Add (new PositionMetadata(newChunk, roomRect));

		for(int i = 0; i < p.Node.Connections.Count; i++){
			if (p.Node.Connections[i].IsCriticalPath || showSideRooms) {
				if (p.Node.IsCriticalPath) {
					p.CritPathMeanDir = (p.CritPathMeanDir + routeDirection * 0.6f).normalized;
				}
				p.SideRoomCount = p.Node.Connections.Count;
				p.PrevNode = p.Node;
				p.Node = p.Node.Connections [i];
				p.Direction = routeDirection;
				p.Position = computedPosition;
				p.PrevChunk = chunk;
				p.SideRoomIndex = i;
				_GenerateLevel (p);
			}
		}
	}

	private Vector3 ChunkSize(GameObject chunk){
		MeshCollider meshCollider = chunk != null ? chunk.GetComponent<MeshCollider> () as MeshCollider : null;
		return meshCollider != null ? meshCollider.bounds.size : Vector3.zero;
	}

	private void _GenerateSideRooms(RoomNode sideRoomNode, Vector3 direction){
	}

	private void GenerateLevel(RoomNode node){
		GenerateLevel (rootnode, null, null);
	}


	private void GenerateLevel(RoomNode node, GameObject parentChunk, DoorDefinition door){
		bool isFirstCall = parentChunk == null;
		GameObject newChunk;
		Rect roomRect;
		Rect parentRect = new Rect ();

		if (isFirstCall) {
			newChunk = InstantiateChunk (node, Vector3.zero);
			Bounds roomBounds = newChunk.GetComponent<MeshCollider> ().bounds;
			roomRect = new Rect (new Vector2(0f, 0f), new Vector2(roomBounds.size.x, roomBounds.size.z) * spacing);
			positionMeta.Add (new PositionMetadata(newChunk, roomRect));
		} else {
			parentRect = positionMeta.Where(c => c.Chunk == parentChunk).First().Rect;
			Vector3 chunkPosition = new Vector3 (parentRect.position.x, 0, parentRect.position.y);
			Vector3 doorNormal = Vector3.Cross (door.Direction, Vector3.up);
			Vector3 horizontalOffset = Vector3.Scale (door.RelPosition, doorNormal) * Random.value;

			//First line up the chunks at predefined positions to ensure that they will not overlap
			//They are later placed at their actual position after overlapping has been handled
			//Therefore a class containing the chunk and rectangle is created. Rect is used for separation algorithm.
			newChunk = InstantiateChunk (node, new Vector3(tmpChunkPos, 0, -1000));
			Bounds roomBounds = newChunk.GetComponent<MeshCollider> ().bounds;
			Vector2 roomSize = new Vector2 (roomBounds.size.x, roomBounds.size.z) * spacing;
			float roomDistance = node.IsCriticalPath ? 1f : 0.5f; //Smaller distance if sideroom
			chunkPosition += door.Direction * roomSize.magnitude * roomDistance + horizontalOffset * 5f;
			roomRect = new Rect (new Vector2(chunkPosition.x, chunkPosition.z), roomSize);
			tmpChunkPos += (int)roomRect.size.magnitude + 100;
			positionMeta.Add (new PositionMetadata(newChunk, roomRect));
		}

		DebugRoomID roomid = newChunk.AddComponent<DebugRoomID> () as DebugRoomID;
		roomid.ID = node.ID;
		roomid.isCritical = node.IsCriticalPath;

		//Choose the door of the NEW chunk that will be connected with the PARENTs door
		//Then create a Metadata Object that represents the connection and is later used to create the hallway
		DoorManager doorManager = newChunk.GetComponent<DoorManager> ();
		List<DoorDefinition> doorDefinitions = new List<DoorDefinition> (doorManager.RandomDoors);

		//The rootnode will have no parent rooms, so theres nothing to compare or to connect
		if (!isFirstCall) {
			DoorDefinition closestDoor = FindClosestDoor (doorDefinitions, door);
			doorDefinitions.Remove (closestDoor);
			HallwayMeta newHallway = new HallwayMeta (door, closestDoor);
			roomid.hallwayMeta = newHallway;
			hallwayMeta.Add (newHallway);
		}

		for (int i = 0; i < doorDefinitions.Count; i++) {
			GenerateLevel (node.Connections[i], newChunk, doorDefinitions[i]);
		}
		//Not needed anymore so it can be destroyed
		GameObject.DestroyImmediate (doorManager);
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

	private DoorDefinition FindClosestDoor(List<DoorDefinition> doors, DoorDefinition compare){
		float distance = float.MaxValue;
		DoorDefinition closestDoor = null;

		foreach (DoorDefinition newDoor in doors) {
			if (Vector3.Distance (newDoor.RelPosition + compare.Direction * 1000f + compare.Position, compare.Position) < distance) {
				closestDoor = newDoor;
			}
		}

		return closestDoor;
	}
		
	private void SeparateRooms(){
		bool separated = false;
		int iterations = 0;
		do {
			iterations++;
			separated = true;

			foreach(PositionMetadata room in positionMeta){
				Vector2 velocity = Vector2.zero;
				Vector2 center = room.Rect.center;

				foreach(PositionMetadata other in positionMeta){
					//No comparison with itself
					if(room == other) continue;
					//Only search for overlapping rectangles
					if(!room.Rect.Overlaps(other.Rect)) continue;

					Vector2 otherCenter = other.Rect.center;
					Vector2 diff = center - otherCenter;
					float diffLen = diff.sqrMagnitude;

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
					Rect newRect = new Rect(room.Rect.position + velocity, room.Rect.size);
					room.Rect = newRect;
				}
			}

		} while(!separated);
	}

	//Applies the Room positions to the instances.
	private void ApplyPositions(){
		foreach (PositionMetadata chunkPos in positionMeta) {
			Vector3 position = new Vector3 (chunkPos.Rect.center.x, 0f, chunkPos.Rect.center.y);
			chunkPos.Chunk.transform.position = position;
		}
	}

	private void CreateHallways(){
		UpdateDoorPositions ();
		HallwayGizmo gizmo = GameObject.FindGameObjectWithTag ("Respawn").GetComponent<HallwayGizmo> ();
		List<Rect> originalRects = (from r in positionMeta
			select r.Rect).ToList();

		List<Rect> resizedRects = new List<Rect> ();
		originalRects.ForEach (r => resizedRects.Add (new Rect (r.center -  (r.size / spacing) * .5f, r.size / spacing)));
		gizmo.rooms = resizedRects.ToArray();

		foreach (HallwayMeta hallwayMeta in hallwayMeta) {
			HallwayAStar routing = new HallwayAStar (resizedRects, hallwayMeta.StartDoor, hallwayMeta.EndDoor);
			gizmo.path = routing.BuildPath ();
			gizmo.availableSpace = routing.AvailableSpace;
		}
	}

	//Usually door's positions are updated during editor time, but not during generation time
	//Since the rooms have been placed and then separated, the door positions have to be recalculated
	//For the hallway generation.
	private void UpdateDoorPositions(){
		foreach (PositionMetadata meta in positionMeta) {
			meta.UpdateDoorPositions ();
		}
	}

	//Used by LevelGeneratorWindow to manage (clear) rooms in editor view
	public List<GameObject> GeneratedRooms{
		get{ return positionMeta.Select (c => c.Chunk).ToList (); }
	}
}

public class LevelGeneratorWindow : EditorWindow {
	//Level Graph Properties
	private int roomCount;
	private int critPathLength;
	private int maxDoors;
	private float distribution;
	private LevelGraph levelGraph;
	//Procedural Level Properties
	private float size;
	//GUI Properties
	private bool showLevelGraph = true;
	private bool showProceduralLevel = true;
	private string path = "Chunks";
	private int seed = 0;
	//Used to delete old object before generating new ones
	private List<GameObject> generatedObjects = new List<GameObject>();
	private bool isAutoUpdate = false;
	private bool isSeparateRooms = true;
	private bool showSideRooms = false;

	[MenuItem("Window/Level Generator")]
	public static void ShowWindow(){
		EditorWindow.GetWindow (typeof(LevelGeneratorWindow));
	}

	void OnGUI(){

		EditorGUILayout.Space ();
		showLevelGraph = EditorGUILayout.Foldout (showLevelGraph, "Level Graph Properties");
		if (showLevelGraph) {
			roomCount = EditorGUILayout.IntField ("Room Count", roomCount);
			roomCount = Mathf.Clamp (roomCount, 1, 20);
			critPathLength = EditorGUILayout.IntField ("Critical Path", critPathLength);
			critPathLength = Mathf.Clamp (critPathLength, Mathf.Min (2, roomCount), Mathf.Max (2, roomCount));
			maxDoors = EditorGUILayout.IntField ("Max. Doors", maxDoors);
			maxDoors = Mathf.Clamp (maxDoors, 3, 10);
			distribution = EditorGUILayout.Slider ("Distribution", distribution, 0.05f, 1f);
		}

		EditorGUILayout.Space ();

		showProceduralLevel = EditorGUILayout.Foldout (showProceduralLevel, "Level Properties");
		if (showProceduralLevel) {
			EditorGUILayout.LabelField (path);
			size = EditorGUILayout.FloatField ("Size", size);
			size = Mathf.Max (0f, size);
		}

		EditorGUILayout.Space ();

		seed = EditorGUILayout.IntField ("Seed", seed);
		isAutoUpdate = EditorGUILayout.Toggle ("Auto Update", isAutoUpdate);
		isSeparateRooms = EditorGUILayout.Toggle ("Separate Rooms", isSeparateRooms);
		showSideRooms = EditorGUILayout.Toggle ("Show side rooms", showSideRooms);

		EditorGUILayout.BeginHorizontal ();
		if (GUILayout.Button ("Generate Level")) {
			Generate ();
		}
		if (GUILayout.Button ("Clear")) {
			ClearLevel ();
		}

		if (isAutoUpdate) {
			Generate ();
		}

		EditorGUILayout.EndHorizontal ();
	}

	private void Generate(){
		ClearLevel ();
		Random.InitState (seed);
		levelGraph = new LevelGraph ();
		levelGraph.GenerateGraph (roomCount, critPathLength, maxDoors, distribution);
		ProceduralLevel level = new ProceduralLevel (path, levelGraph, isSeparateRooms, size, showSideRooms);
		generatedObjects = level.GeneratedRooms;
	}

	private void ClearLevel(){
		foreach (GameObject room in generatedObjects) {
			DestroyImmediate (room);
		}
	}
}
