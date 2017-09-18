using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEditor;
using System.Linq;

public class RoomTransformation{
	protected List<DoorDefinition> availableDoors;
	private List<RoomTransformation> connections;
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
		this.connections = new List<RoomTransformation> ();
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
		AlignToGrid ();
	}

	public void AlignToGrid(){
		Vector2 pos = rect.position;
		float gridSize = DoorDefinition.GlobalSize;
		pos.x = Mathf.Round (pos.x / gridSize) * gridSize;
		pos.y = Mathf.Round (pos.y / gridSize) * gridSize;
		rect.position = pos;
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

	//Is neccessary to calculate the furthest distance (see below)
	//Will store a reference to a connecting room and vice versa
	public void AddConnection(RoomTransformation otherChunk){
		if (!connections.Contains (otherChunk) && otherChunk != null) {
			connections.Add (otherChunk);
			otherChunk.AddConnection (this);
		}
	}

	//Calculates the furthest distance between any rooms connected to this one
	//Used by the AStarGrid to optimize the grid the AStar Algorithm works on
	public float FurthestDistance{
		get{
			RoomTransformation furthest = connections.OrderByDescending (c => Vector2.Distance (c.rect.center, rect.center)).FirstOrDefault ();
			return Vector2.Distance (furthest.rect.center, rect.center);
		}
	}

	//Y position will be calculated using the door positions
	//At this point, all doors of a chunk have the same y position
	private float FindYPosition(){
		//Doors currently have a static size of 2*2 resulting in extends of 1*1
		//Subtract 1 to compensate for the doors position being mesured from the center
		//-> Writing this a few weeks later: Thank you past me. Very intuitive to hardcode this value *change*
		return doors.Count == 0 ? 0f : -(doors [0].RelPosition.y - DoorDefinition.GlobalSize * 0.5f);
	}

	public Vector3 Position {
		get {
			return new Vector3 (rect.center.x, FindYPosition(), rect.center.y);
		}
	}

	public List<RoomTransformation> Connections {
		get {
			return this.connections;
		}
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

	public List<DoorDefinition> Doors {
		get {
			return this.doors;
		}
	}
}

public class ProceduralLevel{
	private static bool isGenerating = false;
	private DebugData debugData; //Meta data created during the process. Used to display in editor
	private LevelMetadata levelMetadata; //Output of the generation process
	private RoomNode rootnode; //Rootnode of the level graph
	private ChunkHelper helper; //Helps searching Chunks
	private ChunkInstantiator chunkInstantiator;
	private float tmpChunkPos; //Temporary variable used for instantiating chunks at position
	private float tmpChunkStep;
	private List<RoomTransformation> positionMeta;
	private bool isConstraintError = false; //True, if no chunk could be found
	private List<HallwayMeta> hallwayMeta;
	private float spacing; //Space between rooms, used is separation is active
	private float distance; //Distance used in the FreeTree algorithm
	private bool isSeparate; //Separate rooms, avoid overlapping
	private LevelGeneratorPreset preset;
	private Material[] hallwayMaterials;
	private float hallwayTiling;
	private bool setIsStatic;

	public ProceduralLevel(LevelGraph graph, LevelGeneratorPreset preset, bool setIsStatic){
		//IMPORTANT, multiply with the door size. Doors require the chunk to be aligned on the grid on GENERATION time
		//Ensure, that chunks are on the grid, since the doors align to the grid regardless of the chunk position, which
		//Will result in shifted doorpositions on repositioning the chunks
		tmpChunkPos = DoorDefinition.GlobalSize * -5000f;
		tmpChunkStep = DoorDefinition.GlobalSize * -50f;
		isGenerating = true;
		this.preset = preset;
		this.hallwayTiling = preset.HallwayTiling;
		this.distance = preset.RoomDistance;
		this.rootnode = graph.Rootnode;
		this.spacing = preset.Spacing;
		this.isSeparate = preset.IsSeparateRooms;
		this.hallwayMaterials = preset.HallwayMaterials;
		this.helper = new ChunkHelper (preset);
		this.debugData = new DebugData ();
		this.chunkInstantiator = ChunkInstantiator.Instance;
		this.hallwayMeta = new List<HallwayMeta> ();
		this.positionMeta = new List<RoomTransformation>();
		this.levelMetadata = new LevelMetadata ();
		this.setIsStatic = setIsStatic;

		GenerateLevel (graph);
		if (!isConstraintError) {
			ApplyTransformation ();
			CreateHallways ();
		} else {
			HandleRollback ();
		}
		helper.CleanUp ();
		ChunkInstantiator.RemoveManualProperties ();
		isGenerating = false;
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
		GameObject chunk = InstantiateChunk (node, new Vector3(tmpChunkPos, 0f, tmpChunkStep));

		if (isConstraintError) {
			return;
		}

		tmpChunkPos += tmpChunkStep;
		//Obtain the actual position, the chunk will have later on
		Vector3 chunkSize = ChunkSize (chunk);
		RoomTransformation roomTransform = new RoomTransformation (chunk, node.Position, chunkSize, spacing);
		roomTransform.AddConnection (prevChunk);
		positionMeta.Add(roomTransform);
		debugData.AddRoomMeta (chunk, node);

		if (prevChunk != null) {
			HallwayMeta hallway = prevChunk.FindMatchingDoors (roomTransform);
			hallwayMeta.Add (hallway);
		}

		//Recursively generate subrooms
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
		GameObject randomChunk = helper.PickRandomChunk (node);
		if (randomChunk != null) {
			randomChunk = GameObject.Instantiate (randomChunk); //Instantiate Unity Object
			//randomChunk = (GameObject) PrefabUtility.InstantiatePrefab(randomChunk);
			//PrefabUtility.DisconnectPrefabInstance (randomChunk);
			randomChunk.transform.position = position;
			chunkInstantiator.ProcessType = ProcessType.GENERATE;
			chunkInstantiator.InstantiateChunk (randomChunk, node.DoorCount, setIsStatic); //Instantiate Abstract Object
			randomChunk.tag = "ChunkInstance";
			randomChunk.layer = LayerMask.NameToLayer ("LevelGeometry");
		} else {
			isConstraintError = true;
		}
		return randomChunk;
	}

	//A rollback happens, when not chunks could be found to be instantiated
	//This can happen, if the constraints defined by the user are too strict
	private void HandleRollback(){
		positionMeta.ForEach (pm => GameObject.DestroyImmediate (pm.Chunk));
		debugData.Aborted = true;
		Debug.LogWarning ("No chunk could be found that satisfy all of your constraints.");
	}

	//There may be delayed abstract properties on the object that need to be removed
	//Usually, AbstractProperties are removed during chunk instantiation, but the DoorManager e.g.
	//Is needed for Hallway creation and is therefore deleted by this function
	private void RemoveDelayedAbstractProperties(){
		foreach (RoomTransformation transformation in positionMeta) {
			List<AbstractProperty> aps = transformation.Chunk.GetComponents<AbstractProperty> ().ToList();
			aps.ForEach (ap => GameObject.DestroyImmediate (ap));
		}
	}

	//Applies the Room positions to the instances.
	private void ApplyTransformation(){
		positionMeta.ForEach (t => t.DeflateRect ());
		foreach (RoomTransformation transformation in positionMeta) {
			//Usually door's positions are updated during editor time, but not during generation time
			//Since the rooms have been placed and then separated, the door positions have to be recalculated
			//For the hallway generation.
			transformation.UpdateDoorPositions ();
			Vector3 position = transformation.Position;
			AddLevelMetadataRoom (transformation);
			transformation.Chunk.transform.position = position;
		}
		levelMetadata.RoomCount = positionMeta.Count;
	}

	private void CreateHallways(){
		List<Rect> roomRects = GetRoomRects ();
		AStarGrid grid = new AStarGrid (roomRects, positionMeta);
		debugData.Grid = grid.Grid;
		HallwayMeshGenerator meshGenerator = new HallwayMeshGenerator (grid, hallwayTiling);

		foreach (HallwayMeta hw in hallwayMeta) {
			HallwayAStar routing = new HallwayAStar (hw.StartDoor, hw.EndDoor, grid);
			List<Square> path = routing.BuildPath ();
			debugData.AddPath (path);
			AddLevelMetadataPath (path);
			meshGenerator.AddPath (path);
		}
		Mesh mesh = meshGenerator.GenerateMesh (true);
		GameObject hallways = new GameObject ("Hallways");
		hallways.isStatic = setIsStatic;
		hallways.tag = "ChunkInstance";
		hallways.layer = LayerMask.NameToLayer ("LevelGeometry");
		MeshFilter meshFilter = hallways.AddComponent<MeshFilter> ();
		meshFilter.sharedMesh = mesh;
		MeshRenderer meshRenderer = hallways.AddComponent<MeshRenderer> ();
		meshRenderer.sharedMaterials = hallwayMaterials;
		hallways.AddComponent<MeshCollider> ();
		FillHallways (grid, hallways);
	}

	private void FillHallways(AStarGrid grid, GameObject hallwayObject){
		HallwayHelper helper = new HallwayHelper (grid, preset, hallwayObject);
		helper.InsertHallwaySegments ();
	}

	private List<Rect> GetRoomRects(){		
		List<Rect> rects = positionMeta.Select (t => t.Rect).ToList ();
		return rects;
	}

	//Used by LevelGeneratorWindow to manage (clear) rooms in editor view
	public List<GameObject> GeneratedRooms{
		get{ return positionMeta.Select (c => c.Chunk).ToList (); }
	}

	private void SeparateRooms(){
		positionMeta.ForEach (pm => pm.AlignToGrid ());
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
					velocity *= DoorDefinition.GlobalSize * 1.51f;
					room.UpdateRect(room.Rect.position + velocity, room.Rect.size);
				}
			}

		} while(!separated);
	}

	private void AddLevelMetadataPath(List<Square> path){
		List<Vector3> _path = new List<Vector3> ();
		path.ForEach (s => _path.Add (new Vector3 (s.Position.x, DoorDefinition.GlobalSize, s.Position.y)));
		levelMetadata.Paths.Add (_path.ToArray ());
	}

	private void AddLevelMetadataRoom(RoomTransformation roomTransformation){
		levelMetadata.Rooms.Add (new RoomInstanceMeta (roomTransformation));
	}

	public LevelMetadata LevelMetadata {
		get {
			return this.levelMetadata;
		}
	}

	public DebugData DebugData {
		get {
			return this.debugData;
		}
	}

	public static bool IsGenerating {
		get {
			return isGenerating;
		}
	}
}