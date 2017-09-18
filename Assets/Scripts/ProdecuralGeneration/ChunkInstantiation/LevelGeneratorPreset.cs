using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Linq;
using System.Xml.Serialization;
using System.IO;

[System.Serializable]
public class LevelGeneratorPreset{
	private int roomCount;
	private int critPathLength;
	private int maxDoors;
	private float distribution;
	//Procedural Level Properties
	private float roomDistance;
	private float spacing;
	private int seed = 0;
	private bool isSeparateRooms = true;
	private string[] hallwayMaterialPaths;
	private Material[] hallwayMaterials;
	private float hallwayTiling;
	//Constraints
	private List<Constraint> constraints;

	public void Reset(){
		constraints = new List<Constraint> ();
		hallwayMaterials = new Material[3];
		hallwayMaterialPaths = new string[] { "null", "null", "null" };
		isSeparateRooms = true;
		roomDistance = 1.5f;
		hallwayTiling = 1f;
		critPathLength = 2;
		distribution = 1f;
		roomCount = 2;
		maxDoors = 3;
		spacing = 4f;
		seed = 0;
	}

	public int RoomCount {
		get {
			return this.roomCount;
		}
		set {
			roomCount = value;
		}
	}

	public int CritPathLength {
		get {
			return this.critPathLength;
		}
		set {
			critPathLength = value;
		}
	}

	public int MaxDoors {
		get {
			return this.maxDoors;
		}
		set {
			maxDoors = value;
		}
	}

	public float Distribution {
		get {
			return this.distribution;
		}
		set {
			distribution = value;
		}
	}

	public float RoomDistance {
		get {
			return this.roomDistance;
		}
		set {
			roomDistance = value;
		}
	}

	public float Spacing {
		get {
			return this.spacing;
		}
		set {
			spacing = value;
		}
	}

	public int Seed {
		get {
			return this.seed;
		}
		set {
			seed = value;
		}
	}

	public bool IsSeparateRooms {
		get {
			return this.isSeparateRooms;
		}
		set {
			isSeparateRooms = value;
		}
	}

	//Static value, might change in future versions
	public int DoorSize {
		get {
			return 2;
		}
	}

	public string[] HallwayMaterialPaths {
		get {
			return this.hallwayMaterialPaths;
		}
		set {
			hallwayMaterialPaths = value;
		}
	}

	[XmlIgnore]
	public Material[] HallwayMaterials {
		get {
#if UNITY_EDITOR
            UpdateMatPaths();
#endif
            return this.hallwayMaterials;
		}
		set {
			hallwayMaterials = value;
#if UNITY_EDITOR
            UpdateMatPaths();
#endif
        }
	}

	public float HallwayTiling {
		get {
			return this.hallwayTiling;
		}
		set {
			hallwayTiling = value;
		}
	}

	public List<Constraint> Constraints {
		get {
			if (constraints == null) {
				constraints = new List<Constraint> ();
			}
			return this.constraints;
		}
		set {
			constraints = value;
		}
	}

#if UNITY_EDITOR
    private void UpdateMatPaths(){
		if (hallwayMaterials == null) {
			hallwayMaterials = new Material[3];
		}
		string[] separator = { "Resources/" };
		for(int i = 0; i < hallwayMaterials.Length; i++){
			hallwayMaterialPaths [i] = "null";
			string path = AssetDatabase.GetAssetPath (hallwayMaterials [i]);
			if (path.Contains ("Resources/")) {
				path = path.Split (separator, System.StringSplitOptions.None) [1];
				string filename = Path.GetFileNameWithoutExtension (path);
				string relPath = Path.GetDirectoryName(path);
				string sep = relPath.Length > 0 ? "\\" : "";
				path = relPath + sep + filename;
				hallwayMaterialPaths [i] = path;
			}
		}
	}
#endif

    public void LoadMaterials(){
		hallwayMaterials = new Material[3];
		for (int i = 0; i < hallwayMaterials.Length; i++) {
			if (hallwayMaterialPaths [i] != "null") {
				hallwayMaterials [i] = Resources.Load (hallwayMaterialPaths [i]) as Material;
			}
		}
	}
}