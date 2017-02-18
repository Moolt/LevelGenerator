using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ChunkMetadata{
	public GameObject chunk;
	public int doors;
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