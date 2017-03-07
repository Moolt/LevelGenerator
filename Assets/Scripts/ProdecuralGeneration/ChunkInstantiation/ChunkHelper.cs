using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public struct ChunkMetadata{
	public GameObject Chunk;
	public DoorManager DoorManager;
	public ChunkTags ChunkTags;
	public int Priority;
}

public class ChunkHelper{
	private static string path = "Chunks";
	private List<ChunkMetadata> chunkMetaData;
	private LevelGeneratorPreset preset;
	private Dictionary<ConstraintTarget, List<NodeType>> targetMapping = new Dictionary<ConstraintTarget, List<NodeType>>{
		{ ConstraintTarget.AllRooms, new List<NodeType>{NodeType.END, NodeType.MIDDLE, NodeType.SIDE, NodeType.START}},
		{ ConstraintTarget.EndRoom, new List<NodeType>{NodeType.END, NodeType.MIDDLE}},
		{ ConstraintTarget.MiddleRooms, new List<NodeType>{NodeType.START, NodeType.MIDDLE, NodeType.END}},
		{ ConstraintTarget.SideRooms, new List<NodeType>{NodeType.SIDE}},
		{ ConstraintTarget.StartRoom, new List<NodeType>{NodeType.START, NodeType.MIDDLE}}
	};

	public ChunkHelper(LevelGeneratorPreset preset){
		chunkMetaData = new List<ChunkMetadata> ();
		BuildMetadata (Resources.LoadAll<GameObject> (path));
	}

	private void BuildMetadata(GameObject[] chunks){
		foreach (GameObject chunk in chunks) {
			DoorManager doorManager = chunk.GetComponent<DoorManager> ();
			if (doorManager != null) {
				ChunkMetadata meta = new ChunkMetadata ();
				meta.Chunk = chunk;
				meta.DoorManager = doorManager;
				meta.ChunkTags = chunk.GetComponent<ChunkTags> ();
				meta.Priority = 0;
				chunkMetaData.Add (meta);
			}
		}
	}

	public List<GameObject> FindChunks(RoomNode node){
		List<GameObject> matchingChunks = FilterByDoorCount (node.DoorCount);
		List<Constraint> applicableConstraints = FindApplicableConstraints (node.NodeType);
		matchingChunks.RemoveAll (c => !MatchesConstraints (c, applicableConstraints));
		return matchingChunks;
	}

	private bool MatchesConstraints(GameObject chunk, List<Constraint> constraints){
		bool isMatching = true;
		constraints.ForEach (c => isMatching &= c.IsConstraintSatisfied (chunk));
		return isMatching;
	}

	private List<Constraint> FindApplicableConstraints(NodeType nodeType){
		List<Constraint> applicableConstraints = new List<Constraint> (preset.Constraints);
		applicableConstraints.RemoveAll (c => !IsConstraintTargetMatching (c.Target, nodeType));
		return applicableConstraints;
	}

	private List<GameObject> FilterByDoorCount(int doorCount){
		return (from meta in chunkMetaData
		        where meta.DoorManager.minCount <= doorCount && meta.DoorManager.maxCount >= doorCount
		        select meta.Chunk).ToList ();
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