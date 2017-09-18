using System.Collections.Generic;
using System.Xml.Serialization;
using System.Collections;
using System.Linq;
using UnityEngine;
using System.IO;

public class LevelMetadata{
	public List<RoomInstanceMeta> Rooms;
	public List<Vector3[]> Paths;
	public int RoomCount;

	public LevelMetadata(){
		this.Rooms = new List<RoomInstanceMeta> ();
		this.Paths = new List<Vector3[]> ();
	}
}

public struct DoorMetadata{
	public Vector3 RelativePosition;
	public Vector3 Direction;
	public Vector3 Position;

	public DoorMetadata (Vector3 relativePosition, Vector3 direction, Vector3 position){
		this.RelativePosition = relativePosition;
		this.Direction = direction;
		this.Position = position;
	}	
}

public class RoomInstanceMeta{
	private List<GameObject> connections;
	private List<DoorMetadata> doors;
	private List<TagInstance> tags;
	private Vector3 position;
	private GameObject chunk;
	private Bounds bounds;
	private Rect rect;

	public RoomInstanceMeta(RoomTransformation input){
		this.connections = new List<GameObject> ();
		this.doors = new List<DoorMetadata> ();
		this.chunk = input.Chunk;
		this.position = input.Position;
		this.rect = input.Rect;
		input.Connections.ForEach (c => connections.Add (c.Chunk));
		input.Doors.ForEach (d => doors.Add (new DoorMetadata (d.RelPosition, d.Direction, d.Position)));
		this.bounds = FindBounds ();
		this.tags = FindChunkTags ();
	}

	private Bounds FindBounds(){
		Collider collider = chunk.GetComponent<Collider> ();
		if (collider != null) {
			return collider.bounds;
		}
		return new Bounds();
	}

	private List<TagInstance> FindChunkTags(){
		ChunkTags chunkTags = chunk.GetComponent<ChunkTags> ();
		if (chunkTags != null) {
			return chunkTags.Tags;
		}
		return new List<TagInstance> (0);
	}

	public List<GameObject> Connections {
		get {
			return this.connections;
		}
	}

	public Vector3 Position {
		get {
			return this.position;
		}
	}

	public GameObject Chunk {
		get {
			return this.chunk;
		}
	}

	public Bounds Bounds {
		get {
			return this.bounds;
		}
	}

	public Rect Rect {
		get {
			return this.rect;
		}
	}

	public List<DoorMetadata> Doors {
		get {
			return this.doors;
		}
	}

	public List<TagInstance> Tags {
		get {
			return this.tags;
		}
	}
}

public class LevelGenerator {
	private LevelGeneratorPreset preset;
	private bool setLevelToStatic = true;

	public LevelGenerator(){
		preset = new LevelGeneratorPreset ();
	}

	public LevelGeneratorPreset LoadPreset(string presetName){
		XmlSerializer xmlSerializer = new XmlSerializer (typeof(LevelGeneratorPreset));
		string path = GlobalPaths.PresetPathIngame;
        string pathWithFilename = path + presetName;// + ".xml";
        TextAsset textAsset = Resources.Load(pathWithFilename) as TextAsset;
        TextReader textReader = new StringReader(textAsset.text);
		
		//FileStream fileStream = new FileStream (pathWithFilename, FileMode.Open);            
		LevelGeneratorPreset loadedPreset = xmlSerializer.Deserialize (textReader) as LevelGeneratorPreset;
		loadedPreset.LoadMaterials ();
		//fileStream.Close ();
		if (loadedPreset != null) {
			preset = loadedPreset;
		}

		return preset;
	}

	public LevelMetadata GenerateLevel(int seed){
		Random.InitState (seed);
		LevelGraph levelGraph = new LevelGraph ();
		levelGraph.GenerateGraph (preset.RoomCount, preset.CritPathLength, preset.MaxDoors, preset.Distribution);
		ProceduralLevel level = new ProceduralLevel (levelGraph, preset, true);
		return level.LevelMetadata;
	}

	public LevelMetadata GenerateLevel(string presetName, int seed, bool clear){
		LoadPreset (presetName);
		if (preset == null) {
			Debug.LogError ("Error loading Preset");
			return null;
		}
		if (clear) {
			ClearLevel ();
		}
		return GenerateLevel (seed);
	}

	public LevelMetadata GenerateLevel(string presetName, int seed){
		return GenerateLevel (presetName, seed, setLevelToStatic);
	}

	public LevelMetadata GenerateLevel(int seed, bool clear){
		if (clear) {
			ClearLevel ();
		}
		return GenerateLevel (seed);
	}

	public LevelMetadata GenerateLevel(){
		return GenerateLevel ((int)Random.value * 10000);
	}

	public static void ClearLevel(){
		List<GameObject> instances = GameObject.FindGameObjectsWithTag ("ChunkInstance").ToList();
		foreach (GameObject room in instances) {
			Object.DestroyImmediate (room);
		}
		List<GameObject> hallways = GameObject.FindGameObjectsWithTag ("HallwayTemplate").ToList();
		hallways.ForEach (h => h.SetActive (false));
	}

	public bool SetLevelToStatic {
		get {
			return this.setLevelToStatic;
		}
		set {
			setLevelToStatic = value;
		}
	}
}