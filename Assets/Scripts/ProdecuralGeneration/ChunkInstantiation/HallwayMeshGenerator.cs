using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HallwaySegment{
	private Vector3 fromPosition;
	private Vector3 toPosition;
	private Vector3 direction;
	private bool isCurve;

	public HallwaySegment (Vector3 fromPosition, Vector3 toPosition, Vector3 direction, bool isCurve){
		this.fromPosition = fromPosition;
		this.toPosition = toPosition;
		this.direction = direction;
		this.isCurve = isCurve;
	}
	
}

public class HallwayMeshGenerator : MonoBehaviour {

	private List<List<Square>> hallwaySquares;
	private Mesh mesh;

	public HallwayMeshGenerator(){
		this.hallwaySquares = new List<List<Square>> ();
	}

	private void GenerateMesh(){
		mesh = new Mesh ();
		//List<HallwaySegment> segments = PrepareHallwaySegments ();

		foreach (List<Square> squares in hallwaySquares) {

			foreach (Square square in squares) {
				
			}

		}
	}

	private void PrepareHallwaySegments(List<Square> source){
		for (int i = 0; i < source.Count; i++) {
			
		}
	}

	//Create four vertices each square

	private List<Vector3> ComputeVertices(List<Square> squares){
		/*List<Vector3>
		foreach (Square square in squares) {
			
		}*/
		return null;
	}

	public Mesh Mesh{
		get{ 
			GenerateMesh ();
			return mesh; 
		}
	}

	public void AddHallway(List<Square> hallwaySquares){
		this.hallwaySquares.Add (hallwaySquares);
	}
}
