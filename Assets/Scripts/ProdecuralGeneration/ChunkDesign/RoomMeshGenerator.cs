using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DoorGridElement{
	public Rect Rect { get; set; }
	public bool IsInsideDoor { get; set; }
}

[System.Serializable]
public class RoomMeshData{
	private AbstractBounds abstractBounds;
	private List<DoorDefinition> doors;
	private List<float> xCoordinates = new List<float> ();
	private List<float> yCoordinates = new List<float> ();
	private float tiling = 32;
    private bool isDirty = true;

	public RoomMeshData(AbstractBounds abstractBounds){
		this.abstractBounds = abstractBounds;
	}

	//Extends
	private float width = 1f;
	private float height = 1f;
	private float length = 1f;

	public List<Vector3> Vertices {
		get;
		set;
	}

	public List<int>[] Triangles {
		get;
		set;
	}

	public List<Vector2> UVs {
		get;
		set;
	}

	public Vector3 Extends{
		set{
            if(value.x != width || value.y * 2f != height || value.z != length) {
			    width = value.x;
			    height = value.y * 2f;
			    length = value.z;
                isDirty = true;
            }
		}
		get{
			return new Vector3 (width, height, length);
		}
	}

	public float Tiling {
		set {
            if(tiling != value){
			    tiling = value;
                isDirty = true;
            }
		}
	}

	public Vector3 Size{
		get{
			return new Vector3 (width * 2f, height, length * 2f);
		}
	}

	public List<DoorDefinition> Doors {
		get {
			return this.doors;
		}
		set {
			doors = value;
			isDirty = true;
		}
	}

	public void ConstructRoom(){
		if (isDirty || true) {
		    Vertices = new List<Vector3> ();
		    Triangles = new List<int>[6];
		    UVs = new List<Vector2> ();

		    CalculateWallMesh (0, Vector3.forward);
		    CalculateWallMesh (1, Vector3.back);
		    CalculateWallMesh (2, Vector3.right);
		    CalculateWallMesh (3, Vector3.left);
		    BuildPlaneMesh (4, Vector3.up, 0, 6, 8, 2);
		    BuildPlaneMesh (5, Vector3.down, 20, 26, 24, 18);
            isDirty = false;
        }
	}

	public void CalculateWallMesh(int subMesh, Vector3 direction){
		if (doors != null) {
			Rect[] doorRects = GetDoorsAsRect (direction);
			Rect wallRect = GetWallRect (direction);

			BuildCoordinateGrid (doorRects, wallRect);
			DoorGridElement[] gridElements = PopulateGrid (doorRects);
			BuildWallMesh(subMesh, gridElements, direction);
			CalculateUVs (subMesh, direction);
		}
	}

	//Used for floor and ceil
	private void BuildPlaneMesh(int subMesh, Vector3 direction, params int[] cornerIndices){
		Triangles [subMesh] = new List<int> ();
		Vertices.AddRange (abstractBounds.RelativeCorners (cornerIndices[0], cornerIndices[1], cornerIndices[2], cornerIndices[3]));
		int length = Vertices.Count;
		Triangles[subMesh].AddRange (new int[]{ length - 4, length - 3, length - 1 });
		Triangles[subMesh].AddRange (new int[]{ length - 1, length - 3, length - 2 });
		CalculateUVs (subMesh, direction);
	}

	private void BuildWallMesh(int subMesh, DoorGridElement[] gridElements, Vector3 direction){
		Triangles [subMesh] = new List<int> ();
		//Back Vector is used to transform a 2DVec back to a 3DVec
		//It doesn't matter which point on the plane / wall it actually is, as long as it stores the 
		//X or Y coordinate that is missing from the 2DVec. Subtract position to get coordinate in object space
		Vector3 backVec = abstractBounds.FindCorner (0, direction) - abstractBounds.transform.position;
		foreach (DoorGridElement gridElement in gridElements) {
			if (!gridElement.IsInsideDoor) {
				Rect elementRect = gridElement.Rect;
				Vector3 x1y1 = Vec2ToVec3 (elementRect.position, backVec, direction);
				Vector3 x1y2 = Vec2ToVec3 (new Vector2 (elementRect.position.x, elementRect.position.y + elementRect.height), backVec, direction);
				Vector3 x2y2 = Vec2ToVec3 (elementRect.position + elementRect.size, backVec, direction);
				Vector3 x2y1 = Vec2ToVec3 (new Vector2 (elementRect.position.x + elementRect.width, elementRect.position.y), backVec, direction);
				Vertices.AddRange (new Vector3[]{ x1y1, x1y2, x2y2, x2y1 });
				int length = Vertices.Count;

				//Walls should face inward. Triangle order therefore depends on the walls direction
				if (direction == Vector3.forward || direction == Vector3.left) {
					Triangles[subMesh].AddRange (new int[]{ length - 4, length - 3, length - 1 });
					Triangles[subMesh].AddRange (new int[]{ length - 1, length - 3, length - 2 });
				} else {
					Triangles[subMesh].AddRange (new int[]{ length - 1, length - 2, length - 4 });
					Triangles[subMesh].AddRange (new int[]{ length - 4, length - 2, length - 3 });
				}
			}
		}
	}

	private void CalculateUVs(int subMesh, Vector3 direction){
		int vertCount = (Triangles [subMesh].Count / 6) * 4;
		Vector2[] planeUVs = new Vector2[vertCount];

		for (int i = 0; i < vertCount; i++) {
			int index = Vertices.Count - vertCount + i;
			Vector2 uv = Vec3ToVec2 (Vertices [index], direction);
			uv.x *= tiling;
			uv.y *= tiling;
			planeUVs [i] = uv;
		}
		UVs.AddRange (planeUVs);
	}

	private DoorGridElement[] PopulateGrid(Rect[] doorRects){
		List<DoorGridElement> gridElements = new List<DoorGridElement> ();
		//Used to merge grid cells if they have no door between then
		//Has to be true for the first iteration to avoid out of index problems
		//Is reset for every row, could be expanded for both axis
		bool lastElementWasDoor = true;

		for (int y = 0; y < yCoordinates.Count - 1; y++) {
			for (int x = 0; x < xCoordinates.Count - 1; x++) {
				//Build the grid cell
				DoorGridElement gridElement = new DoorGridElement ();
				float width = Mathf.Max (xCoordinates [x], xCoordinates [x + 1]) - Mathf.Min (xCoordinates [x], xCoordinates [x + 1]);
				float height = Mathf.Max (yCoordinates [y], yCoordinates [y + 1]) - Mathf.Min (yCoordinates [y], yCoordinates [y + 1]);
				gridElement.Rect = new Rect (xCoordinates [x], yCoordinates [y], width, height);
				gridElement.IsInsideDoor = HasIntersectionWithDoor (doorRects, gridElement.Rect);

				//Only adds the new element if the last element was inside a door and therefore theres nothing to merge
				//Or of the new grid cell itself is inside a door
				if (lastElementWasDoor || gridElement.IsInsideDoor) {
					gridElements.Add (gridElement);
				} else {
					//Expand the width of the last grid cell by the width of the new one, preventing unneeded vertices
					Rect oldRect = gridElements [gridElements.Count - 1].Rect;
					Rect extendedRect = new Rect (oldRect.x, oldRect.y, oldRect.width + gridElement.Rect.width, oldRect.height);
					gridElements [gridElements.Count - 1].Rect = extendedRect;
				}

				lastElementWasDoor = gridElement.IsInsideDoor;
			}
			lastElementWasDoor = true;
		}
		return gridElements.ToArray ();
	}
		
	private bool HasIntersectionWithDoor(Rect[] doorRects, Rect gridElementRect){
		foreach (Rect doorRect in doorRects) {			
			if (doorRect.Contains (gridElementRect.center)) {
				return true;
			}
		}
		return false;
	}

	//Retrieves all x- and y-values from both the wall and all doors
	//Doubles are removed and the lists are sorted
	private void BuildCoordinateGrid(Rect[] doorRects, Rect wallRect){
		xCoordinates.Clear ();
		yCoordinates.Clear ();

		foreach (Rect doorRect in doorRects) {
			AddRectCoords (doorRect);
		}

		AddRectCoords (wallRect);
		xCoordinates.Sort ();
		yCoordinates.Sort ();
	}
		
	private void AddRectCoords(Rect rect){
		AddCoord (xCoordinates, rect.xMin);
		AddCoord (xCoordinates, rect.xMax);
		AddCoord (yCoordinates, rect.yMin);
		AddCoord (yCoordinates, rect.yMax);
	}

	private void AddCoord(List<float> coords, float val){
		if (!coords.Contains (val)) {
			coords.Add (val);
		}
	}

	//Transforms the wall defined by the direction to 2D and creates a Rect
	private Rect GetWallRect(Vector3 direction){
		Vector3 origin = new Vector3 (-Extends.x, 0f, -Extends.z);
		Vector2 _2DOrigin = Vec3ToVec2 (origin, direction);
		Vector2 _2DSize = Vec3ToVec2 (Size, direction);
		return new Rect (_2DOrigin, _2DSize);
	}

	private Rect[] GetDoorsAsRect(Vector3 direction){
		List<Rect> doorRects = new List<Rect> ();
		foreach (DoorDefinition door in doors) {
			if (door.Direction == direction) {
				Vector2 pos2D = Vec3ToVec2 (door.RelPosition, direction);
				Vector2 size2D = Vec3ToVec2 (DoorDefinition.GlobalSize * Vector3.one, direction);
				pos2D -= size2D * .5f;
				doorRects.Add (new Rect (pos2D, size2D));
			}
		}
		return doorRects.ToArray();
	}

	//Assuming that the direction is never along the y axis
	private Vector2 Vec3ToVec2(Vector3 input, Vector3 direction){
		Vector3 normal = Vector3.Cross (direction, Vector3.up);
		Vector2 output = Vector2.zero;

		if (direction == Vector3.up || direction == Vector3.down) {
			return new Vector2 (input.x, input.z);
		} else {
			output.y = input.y;
			//The axis which the direction points to should be eliminated by the scale with the normal
			//The remaining value is the x value
			output.x += input.x * Mathf.Abs (normal.x);
			output.x += input.z * Mathf.Abs (normal.z);
			return output;
		}
	}

	private Vector3 Vec2ToVec3(Vector3 input, Vector3 original, Vector3 direction){
		Vector3 normal = Vector3.Cross (direction, Vector3.up);
		normal = Vector3.Scale (normal, normal);
		Vector3 output = Vector3.zero;
		output.y = input.y;
		//Static axis
		output += Vector3.Scale (original, Vector3.Scale(direction, direction));
		//Map x axis to its original axis
		output += Vector3.Scale (Vector3.one * input.x, normal);
		return output;
	}

	public bool IsDirty {
		get {
			//return true;
			return this.isDirty;
		}
		set {
			isDirty = value;
		}
	}
}

[RequireComponent (typeof(MeshFilter), typeof(MeshRenderer))]
[RequireComponent (typeof(AbstractBounds), typeof(DoorManager))]
[ExecuteInEditMode()]
public class RoomMeshGenerator : MeshProperty {
	public Material floorMaterial;
	public Material wallMaterial;
	public float tiling;
	//Meta data
	private List<DoorDefinition> doors = new List<DoorDefinition>(0);
	private AbstractBounds abstractBounds;
	private Vector3 roomExtends;
	//Procedural Data
	private List<Vector3> vertices;
	private RoomMeshData meshData = null;
	private List<int> triangles;
	//Mesh components
	private MeshRenderer meshRenderer;
	private MeshFilter meshFilter;
	private Mesh mesh;

	void Init(bool isGenerate) {
        abstractBounds = GetComponent<AbstractBounds>();
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (meshData == null){
            meshData = new RoomMeshData(abstractBounds);
        }

		if(meshFilter.sharedMesh == null || isGenerate){
			meshFilter.sharedMesh = new Mesh ();
		}
		mesh = meshFilter.sharedMesh;
		mesh.name = "RoomMesh";

		roomExtends = abstractBounds.Extends;

		if (Application.isEditor) {
			mesh.MarkDynamic();
		}
	}

	public override void Preview(){
		Init (false);
		ObtainDoors ();
		UpdateMesh ();
		UpdateMeshCollider ();
	}

	private void UpdateMesh(){
		mesh.Clear ();
		meshData.Extends = roomExtends;
		meshData.Tiling = tiling;
		meshData.ConstructRoom ();

		mesh.vertices = meshData.Vertices.ToArray ();
		//Retrieve submeshes
		mesh.subMeshCount = meshData.Triangles.Length;
		for (int i = 0; i < mesh.subMeshCount; i++) {
			mesh.SetTriangles(meshData.Triangles[i].ToArray(), i);
		}

		meshRenderer.materials = new Material[] {
			wallMaterial,
			wallMaterial,
			wallMaterial,
			wallMaterial,
			floorMaterial,
			floorMaterial
		};

		mesh.uv = meshData.UVs.ToArray();

		mesh.RecalculateNormals ();
		mesh.RecalculateBounds ();
	}

	public override void Generate(){
		Init (true);
		ObtainDoors ();
		UpdateMesh ();
		UpdateMeshCollider ();
	}

	private void UpdateMeshCollider(){
		MeshCollider meshCollider = GetComponent<MeshCollider> () as MeshCollider;
		if (meshCollider != null) {
			meshCollider.convex = false;
			meshCollider.sharedMesh = meshFilter.sharedMesh;
		}
	}
	
	private void ObtainDoors(){
		DoorManager doorDefinitions = GetComponent<DoorManager> () as DoorManager;
		if (doorDefinitions != null) {
			if (true) {
				doors.Clear ();
				doors.AddRange (doorDefinitions.RandomDoors);
				//doors.ForEach (d => Debug.Log (d.RelPosition));
				meshData.Doors = doors;
			}
		}
	}

	//This helps to prevent a bug occuring in the ChunkGenerator Window since
	//Eventhough when the object is deleted, in most cases (all?) it remains for an uncertain
	//Amount of time before being actually removed from the scene. 
	//Removing the tag at least gives a hint about the Chunks status of removal.
	public void OnDestroy(){
		transform.gameObject.tag = "Untagged";
	}

	//Force recreation of the mesh. Needed after (de)activation of the object, since the mesh seems to get corrupted
	public void OnEnable(){
		if (meshData != null) {
			meshData.IsDirty = true;
		}
	}
}
