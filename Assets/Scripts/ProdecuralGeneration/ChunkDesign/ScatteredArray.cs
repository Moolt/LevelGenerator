using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ScatteredArray : MultiplyingProperty {

	public int editorSeed = 100; //Only used within editor
	public int duplicateCount = 3;
	public bool randomizeCount = false;
	public float radius = 1f; //Radius of Area of type SPHERICAL
	public Vector2 rectArea = new Vector2(5f, 5f);
	public bool preventOverlapping = true; //Slow
	public float spacing = 0f; //Space between objects
	public bool hasOverflow = false; //Generation of positions stops, if area is too crowded
	public AreaType areaType = AreaType.SPHERICAL;
	public bool autoFill; //Fills the available area

	private MeshFilter meshFilter;
	private int iterationsUntilOverflow = 150; //Max. amount of iteration until process stops
	private int calculatedCount; //for tagging only

	public override void DrawEditorGizmos(){
		Vector3[] positions = CalculatePositions ();
		for (int i = 0; i < positions.Length; i++) {
			if (MeshFound()) {
				Gizmos.color = Color.black;
				Gizmos.DrawWireMesh (PreviewMesh.sharedMesh, positions [i], Quaternion.identity, PreviewMesh.gameObject.transform.localScale);
			} else {
				Gizmos.color = Color.black;
				Gizmos.DrawWireCube(positions[i], new Vector3(1f, 1f, 1f));
			}
		}
	}

	public override void Preview(){
		
	}
		
	protected override Vector3[] CalculatePositions(){
		calculatedCount = 0;
		ICollection<Vector3> positions = new List<Vector3> ();
		float meshSize = (MeshFound ()) ? Vector3.Scale (PreviewMesh.sharedMesh.bounds.size, PreviewMesh.gameObject.transform.localScale).magnitude * .5f : 1f;
		Vector3 meshBounds = (MeshFound()) ? Vector3.Scale (PreviewMesh.sharedMesh.bounds.extents, PreviewMesh.gameObject.transform.localScale) : new Vector3(1f, 1f, 1f);
		int producedPositions = 0;
		bool done = false;

		//Only use the seed when both in edit mode and not while generating
		if (Application.isEditor && SceneUpdater.IsActive) {			
			Random.InitState (editorSeed);
		}

		while(!done) {
			hasOverflow = false;
			bool searchingFreeSpace = true;
			Vector2 planePosition = new Vector2 ();
			int counter = 0;

			//Searching for free Space, until a position is found
			while (searchingFreeSpace) {
				//Generatig random 2D position
				planePosition = GetRandomPosition (meshSize);
				//If overlapping should be prevented, the process is repeated until a free spot was found
				if (preventOverlapping) {
					searchingFreeSpace = IsOverlapping (meshBounds, planePosition, positions);
					counter++; //Counts the amount of iterations
					if (counter > iterationsUntilOverflow) {
						hasOverflow = true;
						break;
					}
				} else {
					//Only one iteration in case overlapping is allowed
					searchingFreeSpace = false;
				}
			}

			//Stop the process, if no free space can be found after several iterations
			if (hasOverflow) {
				done = true;
				break;
			}

			Vector3 finalPosition = new Vector3 (planePosition.x, 0f, planePosition.y) + GetOffset();
			positions.Add(finalPosition);
			producedPositions++;

			if (!autoFill && producedPositions > duplicateCount + 1) {
				done = true;
			}

			calculatedCount++;
		}
		return positions.ToArray();
	}

	private Vector2 GetRandomPosition(float meshSize){
		if (areaType == AreaType.SPHERICAL) {
			return Random.insideUnitCircle * (radius - meshSize);
		} 
		if (areaType == AreaType.RECT) { 
			return GetRandomPosInRect(rectArea, meshSize);
		}
		if (areaType == AreaType.ABSTRACTBOUNDS) {
			Vector3 bounds = AbstractBounds.Size;
			Vector2 boundsRect = new Vector2 (bounds.x, bounds.z);
			return GetRandomPosInRect(boundsRect, meshSize);
		}
		return new Vector2();
	}

	private Vector2 GetRandomPosInRect(Vector2 rect, float meshSize){
		Vector2 randomPosition = new Vector2 ();
		//Calculating random position for each axis
		randomPosition.x = Random.value * rect.x - rect.x / 2f;
		//Model should be within borders
		randomPosition.x = Mathf.Clamp (randomPosition.x, -rect.x / 2f + meshSize, rect.x / 2f - meshSize);
		randomPosition.y = Random.value * rect.y - rect.y / 2f;
		randomPosition.y = Mathf.Clamp (randomPosition.y, -rect.y / 2f + meshSize, rect.y / 2f - meshSize);
		return randomPosition;
	}

	//Random values aren't calculated in world space. In order to calculate the final coordinates, an offset has to be added
	//In case of spherical and this is the gameobjects position
	//In case of abstract bounds the pos of the parents abstract bounds are used
	private Vector3 GetOffset(){
		if (areaType == AreaType.RECT || areaType == AreaType.SPHERICAL) {
			return transform.position;
		} else {
			Vector3 yAxisOffset = Vector3.Scale (Vector3.up, transform.position);
			return AbstractBounds.transform.position + yAxisOffset;
		}
	}

	//Overlapping is handled using positions and actual mesh bounds
	private bool IsOverlapping(Vector3 meshExtends, Vector2 newPos, ICollection<Vector3> existingPos){
		Vector2 meshSize = new Vector2 (meshExtends.x, meshExtends.z);
		//Spacing is handled by adding the space onto the meshSize
		meshSize.x += spacing;
		meshSize.y += spacing;
		//newPos is the position the is to be checked for overlapping with existing positions
		//Since the existing positions are world coordinates, we have to add the gameObjects position first
		newPos += new Vector2 (GetOffset().x, GetOffset().z);
		Rect newObjRect = new Rect (newPos, meshSize);

		foreach (Vector3 singlePos in existingPos) {
			Vector2 existingPos2D = new Vector3 (singlePos.x, singlePos.z);
			Rect existingObjRect = new Rect (existingPos2D, meshSize);
			//Checking both rects for overlapping
			if (newObjRect.Overlaps (existingObjRect, true)) {
				return true;
			}
		}
		return false;
	}

	public int CalculatedCount {
		get {
			return this.calculatedCount;
		}
	}
}
