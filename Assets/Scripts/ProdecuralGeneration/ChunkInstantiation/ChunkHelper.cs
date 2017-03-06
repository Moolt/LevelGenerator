using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ChunkMetadata{
	public GameObject chunk;
	public int doors;
}

public class ChunkHelper{
	private static string path = "Chunks";
	private List<ChunkMetadata> chunkMetaData;

	public ChunkHelper(){
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