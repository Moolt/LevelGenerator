using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
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
	private int doorSize;

	public void Reset(){
		roomCount = 2;
		critPathLength = 2;
		maxDoors = 3;
		distribution = 1f;
		roomDistance = 1.5f;
		spacing = 4f;
		seed = 0;
		isSeparateRooms = false;
		doorSize = 1;
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

	public int DoorSize {
		get {
			return this.doorSize;
		}
		set {
			doorSize = value;
		}
	}
}