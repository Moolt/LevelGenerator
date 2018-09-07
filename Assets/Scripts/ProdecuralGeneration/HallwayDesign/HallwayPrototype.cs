using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[Serializable]
public class MatchResult{
	public int[] Position;
	public int Rotation;
	public bool IsMatching;

	public MatchResult (bool isMatching, int[] position, int rotation){
		this.IsMatching = isMatching;
		this.Position = position;
		this.Rotation = rotation;
	}

	public GridPosition GridPosition(AStarGrid grid){
		return grid.Grid [Position [0], Position [1]];
	}
}

[Serializable]
public struct MaskSegment{
	public int[] Offset;
	public MaskState State;
	public int RotatedBy;

	public MaskSegment (int[] offset, MaskState state, int rotation){
		this.Offset = offset;
		this.State = state;
		this.RotatedBy = rotation;
	}
}

[Serializable]
public class HallwayMask{
	private MaskState[,] mask;
	private int[] center;
	private List<List<MaskSegment>> relativePositions;
	private List<MatchResult> matchingPositions;
	private bool allowRotate;
    private bool makeCenterNonAffecting;

	public HallwayMask(MaskState[,] mask, int[] center, bool allowRotate, bool makeCenterNonAffecting){
		this.mask = mask;
		this.center = center;
		this.allowRotate = allowRotate;
        this.makeCenterNonAffecting = makeCenterNonAffecting;
		relativePositions = new List<List<MaskSegment>> ();
		FindMaskPositions ();
	}

	//Iterate through the matrix and search only for positive values
	//If found, calculate their relative position to the center and push them to list
	//This will heavily increase the efficiency for comparing the matrix with the generated hallways
	private void FindMaskPositions(){
		List<MaskSegment> originalPositions = new List<MaskSegment> ();
		for (int i = 0; i < mask.GetLength (0); i++) {
			for (int j = 0; j < mask.GetLength (1); j++) {
				if (mask [i, j] != MaskState.UNUSED) { //Both EMPTY and FILL have to be tested, UNUSED can be ignored
					int[] relativePosition = new int[]{ i - center [0], j - center [1] };
					originalPositions.Add (new MaskSegment (relativePosition, mask [i, j], 0));
				}
			}
		}
		relativePositions.Add (originalPositions);
		if (allowRotate) {
			List<MaskSegment> rotateThis = originalPositions;
			for (int i = 0; i < 3; i++) {
				rotateThis = RotateClockwise (rotateThis);
				relativePositions.Add (rotateThis);
			}
		}
	}

	private List<MaskSegment> RotateClockwise(List<MaskSegment> input){
		List<MaskSegment> rotatedPositions = new List<MaskSegment> ();
		foreach (MaskSegment oldSegment in input) {
			int[] mirrored = new int[]{ oldSegment.Offset [0] * -1, oldSegment.Offset [1] };
			int[] swapped = new int[]{ mirrored [1], mirrored [0] };
			rotatedPositions.Add (new MaskSegment (swapped, oldSegment.State, oldSegment.RotatedBy + 90));
		}
		return rotatedPositions;
	}

	public void ApplyMask(AStarGrid grid){
		matchingPositions = grid.UsedPositions.Select (p => IsMatchingMaskAt (grid, p)).Where (r => r.IsMatching).ToList ();
	}

	public void UpdateMatches(AStarGrid grid){
		matchingPositions = matchingPositions.Select (mr => IsMatchingMaskAt (grid, mr.GridPosition (grid))).Where (mr => mr.IsMatching).ToList();
	}

	//Marks the positions of the mask relative to the given position in the grid
	//This will hinder other masks from matching at this position, thus instantiating several hallways
	//at the same position
	public void MarkPositionAsUsed(AStarGrid grid, MatchResult match){
		List<MaskSegment> _mask = FindMaskByRotation (match.Rotation);
		foreach (MaskSegment relative in _mask) {
            if (relative.State == MaskState.EMPTY) continue;
			int x = relative.Offset [0] + match.Position [0];
			int y = relative.Offset [1] + match.Position [1];

            if (0 == relative.Offset[0] && 0 == relative.Offset[1] && makeCenterNonAffecting) continue;
			grid.Grid [x, y].UsedByHallwayTemplate = true;
		}
	}

	private List<MaskSegment> FindMaskByRotation(int rotation){
		int index = rotation / 90;
		return relativePositions [index];
	}

	private MatchResult IsMatchingMaskAt(AStarGrid grid, GridPosition pos){
		foreach (List<MaskSegment> rotatedMask in relativePositions) {
			if (IsMatchingMaskAt (grid, rotatedMask, pos)) {
				return new MatchResult (true, new int[]{ pos.i, pos.j }, rotatedMask [0].RotatedBy);
			}
		}
		return new MatchResult (false, new int[]{ 0, 0 }, 0);
	}

	private bool IsMatchingMaskAt(AStarGrid grid, List<MaskSegment> _mask, GridPosition pos){
		foreach (MaskSegment segment in _mask) {
			int[] absolutePosition = new int[]{ segment.Offset [0] + pos.i, segment.Offset [1] + pos.j };
			GridPosition testPosition = grid.Grid [absolutePosition [0], absolutePosition [1]];
			bool matchesSegment = IsMatchingSegment (segment.State, testPosition) && !testPosition.UsedByHallwayTemplate;
			if (!matchesSegment) {
				return false;
			}
		}
		return true;
	}
		
	public List<MatchResult> MatchingPositions {
		get {
			return this.matchingPositions;
		}
	}

	private bool IsMatchingSegment(MaskState state, GridPosition pos){
		return ((state == MaskState.DOOR && pos.IsDoor) || (state == MaskState.FILL && pos.IsPartOfPath) || (state == MaskState.EMPTY && !pos.IsPartOfPath));
	}
}

public enum MaskState{ UNUSED, FILL, EMPTY, DOOR }

[ExecuteInEditMode]
public class HallwayPrototype : MonoBehaviour{

	private HallwayMeshGenerator meshGenerator;
	[System.NonSerialized]
	public AStarGrid grid;
	[SerializeField]
	[HideInInspector]
	public GridRow[] mapping;
	public bool allowRotation = true;
    public bool makeCenterNonAffecting = false;
    private List<Square> squares;
	private delegate void TraverseMethod(int x, int y);
	private MaskState[,] mask;
	private float extends = 4.5f;

	public void OnEnable(){
		grid = new AStarGrid (extends);
		squares = new List<Square> ();
		//Debug.Log ("onenable");

		if (mapping == null) {
			Debug.Log ("init mapping");
			InitMapping ();
		}
		InitGridFromMapping ();
		DrawGeometry ();
	}

	public void Reset(){
		//Debug.Log ("Resset");
		mapping = null;
		InitMapping ();
		InitGridFromMapping ();
		DrawGeometry ();
	}

	private void InitMapping(){
		mapping = new GridRow[grid.Grid.GetLength (0)];
		for (int i = 0; i < mapping.Length; i++) {
			mapping [i] = new GridRow (grid.Grid.GetLength (1));
		}
	}
		
	private void DebugPrint(int i, int j){
		if (mapping [i][j] == MaskState.FILL) {
			Debug.Log (mapping [i] [j]);
		}
	}		

	public void SetState(int x, int y, MaskState state){
		GridPosition _p = grid.Grid [x, y];
		if (state == MaskState.UNUSED) {
            _p.DoorID = -1;
            _p.IsPartOfPath = false;
			_p.ShouldBeEmpty = false;
		} else if (state == MaskState.FILL) {
            _p.DoorID = -1;
            _p.IsPartOfPath = true;
			_p.ShouldBeEmpty = false;
		} else if(state == MaskState.EMPTY){
            _p.DoorID = -1;
            _p.IsPartOfPath = false;
			_p.ShouldBeEmpty = true;
		} else if(state == MaskState.DOOR){
            _p.DoorID = 1;
            _p.IsPartOfPath = false;
            _p.ShouldBeEmpty = false;
        }
	}

	public static MaskState GetState(int x, int y, AStarGrid _grid){
        if(_grid.Grid[x,y].IsDoor){
            return MaskState.DOOR;
        }
		if (_grid.Grid [x, y].IsPartOfPath) {
			return MaskState.FILL;
		} else if (_grid.Grid [x, y].ShouldBeEmpty) {
			return MaskState.EMPTY;
		}
		return MaskState.UNUSED;
	}

	public MaskState GetState(int x, int y){
		return GetState (x, y, grid);
	}

	public void SwitchState(int x, int y, MaskState state){
		switch (GetState (x, y)) {
		case MaskState.UNUSED:
			SetState (x, y, MaskState.FILL);
			break;
		case MaskState.FILL:
			SetState (x, y, MaskState.EMPTY);
			break;
		case MaskState.EMPTY:
			SetState (x, y, MaskState.DOOR);
			break;
        case MaskState.DOOR:
            SetState(x, y, MaskState.UNUSED);
            break;
        }
	}

	public Vector3 GetPosition(int x, int y){
		Vector2 pos = grid.GetSquareInGrid (x, y).Position;
		return new Vector3 (pos.x, 0f, pos.y);
	}

	public int[] CenterIndices{
		get{
			int _ext = (int)Mathf.Floor (extends);
			return new int[]{ _ext, _ext };
		}
	}

	public int[] GridSize{
		get{
			int _size = (int)(extends * 2f);
			return new int[]{ _size, _size };
		}
	}

	public bool HasAdjacents(int x, int y){
		if (CenterIndices [0] == x && CenterIndices [1] == y) {
			return true;
		}
		bool hasAdjacents = false;
		hasAdjacents |= IsUsed (x - 1, y);
		hasAdjacents |= IsUsed (x, y - 1);
		hasAdjacents |= IsUsed (x + 1, y);
		hasAdjacents |= IsUsed (x, y + 1);
		return hasAdjacents;
	}

	private bool IsUsed(int x, int y){
		if (IsValidPosition (x, y)) {
			return grid.Grid [x, y].IsPartOfPath;
		}
		return false;
	}

	private bool IsValidPosition(int x, int y){
		return (x >= 0 && x < GridSize [0] && y >= 0 && y < GridSize [1]);
	}

	private void SetAdjacents(int x, int y){
		if (GetState (x, y) == MaskState.FILL) {
			squares.Add (grid.GetSquareInGrid (x, y));
			grid.Grid [x, y].Direction = Vector2.up;
			grid.Grid [x, y].AddAdjacent (Vector2.down, GetAdjacent (x, y - 1));
			grid.Grid [x, y].AddAdjacent (Vector2.right, GetAdjacent (x + 1, y));
			grid.Grid [x, y].AddAdjacent (Vector2.up, GetAdjacent (x, y + 1));
			grid.Grid [x, y].AddAdjacent (Vector2.left, GetAdjacent (x - 1, y));
		}
	}

	private GridPosition GetAdjacent(int x, int y){
		GridPosition adjacent = null;
		if (IsValidPosition (x, y) && grid.Grid [x, y].IsPartOfPath) {
			adjacent = grid.Grid [x, y];
		}
		return adjacent;
	}

	private void TraverseThroughGrid(TraverseMethod method){
		ForeachGridCell (ResetGridCell);
		TraverseThroughGrid (CenterIndices [0], CenterIndices [1], method);
		ForeachGridCell (UpdateInacessable);
	}

	private void TraverseThroughGrid(int x, int y, TraverseMethod method){
		if (IsValidPosition (x, y) && !grid.Grid [x, y].visitedByAstar && GetState (x, y) != MaskState.UNUSED) {
			method (x, y);
			grid.Grid [x, y].visitedByAstar = true;
			TraverseThroughGrid (x - 1, y, method);
			TraverseThroughGrid (x, y - 1, method);
			TraverseThroughGrid (x + 1, y, method);
			TraverseThroughGrid (x, y + 1, method);
		}
	}
	
	private void ResetGridCell(int i, int j){
		grid.Grid [i, j].visitedByAstar = false;
		grid.Grid [i, j].AdjacentPositions.Clear ();
		grid.Grid [i, j].Direction = Vector2.zero;
	}

	private void UpdateInacessable (int i, int j){
		if (!grid.Grid [i, j].visitedByAstar) {
			SetState (i, j, MaskState.UNUSED);
		}
	}

	public void DrawGeometry(){
		SetState (CenterIndices [0], CenterIndices [1], MaskState.FILL);        
		squares.Clear ();
		TraverseThroughGrid (SetAdjacents);
		HallwayMeshGenerator meshGenerator = new HallwayMeshGenerator (grid, 1f);
		meshGenerator.AddPath (squares);
		Mesh m = meshGenerator.GenerateMesh (false);
		MeshFilter meshFilter = GetComponent<MeshFilter> ();
		meshFilter.sharedMesh = m;
		AdjustAbstractBounds (m);
		MappingFromGrid ();
    }

	public void AdjustAbstractBounds(Mesh mesh){
		AbstractBounds boundsObject = GetComponentInChildren<AbstractBounds> ();
		if (boundsObject != null) {
			AbstractBounds bounds = boundsObject.GetComponent<AbstractBounds> ();

			if (bounds != null) {
				Vector3 pos = new Vector3 (mesh.bounds.center.x, 0f, mesh.bounds.center.z);
				boundsObject.transform.position = pos;
				bounds.hasFixedSize = true;
				bounds.MinSize = mesh.bounds.size;
				bounds.Preview ();
			}
		}
	}

	private void ForeachGridCell(TraverseMethod method){
		for (int i = 0; i < GridSize [0]; i++) {
			for (int j = 0; j < GridSize [1]; j++) {
				method (i, j);
			}
		}
	}

	public void InitGridFromMapping(){
		ForeachGridCell (InitGridFromMapping);
	}

	private void InitGridFromMapping(int i, int j){
		SetState (i, j, mapping [i] [j]);
	}

	private void MappingFromGrid(){
		ForeachGridCell (MappingFromGrid);
	}

	private void MappingFromGrid(int i, int j){		
		mapping [i] [j] = GetState (i, j);
	}

	public void PositionUpdate(){
		transform.position = Vector3.zero;
	}

	public HallwayMask Mask{
		get{
			int _size = mapping.Length;
			mask = new MaskState[_size, _size];
			for (int i = 0; i < _size; i++) {
				for (int j = 0; j < _size; j++) {
					mask [i, j] = mapping [i] [j];
				}
			}
			return new HallwayMask (mask, CenterIndices, allowRotation, makeCenterNonAffecting);
		}
	}
}
