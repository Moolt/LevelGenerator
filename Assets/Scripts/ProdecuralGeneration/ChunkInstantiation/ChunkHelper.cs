using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ChunkMetadata{
	public List<Constraint> matchingConstraints;
	public GameObject Chunk;
	public DoorManager DoorManager;
	public ChunkTags ChunkTags;
	public float Priority;

	public ChunkMetadata(){
		matchingConstraints = new List<Constraint> ();
	}

	public void RegisterAsMatching(Constraint constraint){
		if (!matchingConstraints.Contains (constraint)) {
			matchingConstraints.Add (constraint);
		}
	}

	public void NotifyConstraints(){
		matchingConstraints.ForEach (mc => mc.MatchingChunkHasBeendUsed ());
	}

	public void ClearConstraintDependencies(){
		matchingConstraints.Clear ();
	}
}

public class ChunkHelperProgress{
	private LevelGeneratorPreset preset;
	private List<ChunkMetadata> created;
	private int sideRoomsCreated = 0;
	private int middleRoomsCreated = 0;

	public ChunkHelperProgress (LevelGeneratorPreset preset){
		this.created = new List<ChunkMetadata> ();
		this.preset = preset;
	}

	public int TotalRoomCount(ConstraintTarget target){		
		switch (target) {
		case ConstraintTarget.AllRooms:
			return preset.RoomCount;
		case ConstraintTarget.MiddleRooms:
			return preset.CritPathLength - 2;
		case ConstraintTarget.SideRooms:
			return preset.RoomCount - preset.CritPathLength;
		}
		return 1;
	}

	public int InstantiatedRoomCount(ConstraintTarget target){		
		switch (target) {
		case ConstraintTarget.AllRooms:
			return created.Count;
		case ConstraintTarget.MiddleRooms:
			return middleRoomsCreated;
		case ConstraintTarget.SideRooms:
			return sideRoomsCreated;
		}
		return 1;
	}		

	public void NoteChunkUsed(ChunkMetadata meta, NodeType nodeType){
		if (meta != null) {
			meta.NotifyConstraints ();
			created.Add (meta);

			switch (nodeType) {
			case NodeType.MIDDLE:
				middleRoomsCreated++;
				break;
			case NodeType.SIDE:
				sideRoomsCreated++;
				break;
			}
		}
	}

	public int Remaining(ConstraintTarget target){
		return TotalRoomCount (target) - InstantiatedRoomCount (target);
	}
}

public class ChunkHelper{
	private static string path = "Chunks";
	private List<ChunkMetadata> chunkMetaData;
	private LevelGeneratorPreset preset;
	private ChunkHelperProgress progress;
	private Dictionary<ConstraintTarget, List<NodeType>> targetMapping = new Dictionary<ConstraintTarget, List<NodeType>> {
		{ ConstraintTarget.AllRooms, new List<NodeType>{ NodeType.END, NodeType.MIDDLE, NodeType.SIDE, NodeType.START } },
		{ ConstraintTarget.EndRoom, new List<NodeType>{ NodeType.END} },
		{ ConstraintTarget.MiddleRooms, new List<NodeType>{ NodeType.MIDDLE} },
		{ ConstraintTarget.SideRooms, new List<NodeType>{ NodeType.SIDE } },
		{ ConstraintTarget.StartRoom, new List<NodeType>{ NodeType.START } }
	};

	public ChunkHelper(LevelGeneratorPreset preset){
		this.preset = preset;
		chunkMetaData = new List<ChunkMetadata> ();
		BuildMetadata (Resources.LoadAll<GameObject> (path));
		this.progress = new ChunkHelperProgress (preset);
	}

	public void CleanUp(){
		//Reset all temporary values in the constraints that are only relevant for instantiation
		preset.Constraints.ForEach(c => c.ResetConstraint());
	}

	private void BuildMetadata(GameObject[] chunks){
		foreach (GameObject chunk in chunks) {
			DoorManager doorManager = chunk.GetComponent<DoorManager> ();
			if (doorManager != null) {
				ChunkMetadata meta = new ChunkMetadata ();
				meta.Chunk = chunk;
				meta.DoorManager = doorManager;
				meta.ChunkTags = chunk.GetComponent<ChunkTags> ();
				meta.Priority = Random.value;
				chunkMetaData.Add (meta);
			}
		}
	}

	private void ResetMetadata(){
		chunkMetaData.ForEach (cm => cm.Priority = Random.value);
		chunkMetaData.ForEach (cm => cm.ClearConstraintDependencies());
	}

	public GameObject PickRandomChunk(RoomNode node){
		ResetMetadata ();
		List<ChunkMetadata> candidates = FindChunks (node);
		candidates = candidates.OrderByDescending (cm => cm.Priority).ToList();
		ChunkMetadata selectedMeta = candidates.Count > 0 ? candidates [0] : null;
		GameObject selectedChunk = selectedMeta == null ? null : selectedMeta.Chunk;
		progress.NoteChunkUsed (selectedMeta, node.NodeType);
		return selectedChunk;
	}

	private List<ChunkMetadata> FindChunks(RoomNode node){
		List<ChunkMetadata> matchingChunks = FilterByDoorCount (node.DoorCount);
		List<Constraint> applicableConstraints = FindApplicableConstraints (node.NodeType);
		matchingChunks.RemoveAll (c => !MatchesConstraints (c, applicableConstraints));
		return matchingChunks;
	}

	private bool MatchesConstraints(ChunkMetadata chunkMeta, List<Constraint> constraints){
		bool isMatching = true;
		constraints.ForEach (c => isMatching &= c.IsConstraintSatisfied (chunkMeta, progress));
		return isMatching;
	}

	private List<Constraint> FindApplicableConstraints(NodeType nodeType){
		List<Constraint> applicableConstraints = new List<Constraint> (preset.Constraints);
		applicableConstraints.RemoveAll (c => !IsConstraintTargetMatching (c.Target, nodeType));
		return applicableConstraints;
	}

	private List<ChunkMetadata> FilterByDoorCount(int doorCount){
		return (from meta in chunkMetaData
		        where meta.DoorManager.minCount <= doorCount && meta.DoorManager.maxCount >= doorCount
		        select meta).ToList ();
	}

	private bool IsConstraintTargetMatching(ConstraintTarget cTarget, NodeType nType){
		return targetMapping [cTarget].Contains (nType);
	}

	//Returns a list of all user defined tags without duplicates
	public static string[] GlobalUserTags{
		get{
			List<string> globalUserTags = new List<string> ();
			List<GameObject> chunks = Resources.LoadAll<GameObject> (path).ToList();
			List<ChunkTags> chunkTags = new List<ChunkTags> ();

			chunks.Where (c => c.GetComponent<ChunkTags> () != null)
				.ToList ()
				.ForEach (c => chunkTags.Add (c.GetComponent<ChunkTags> ()));
			
			chunkTags.SelectMany (ct => ct.userGenerated)
				.Where (t => !globalUserTags.Contains (t.Name))
				.ToList ()
				.ForEach (t => globalUserTags.Add (t.Name));
			
			return globalUserTags.ToArray ();
		}
	}

	public int MaxDoors(){
		return 0;
	}

	public static string Path {
		get {
			return path;
		}
	}
}