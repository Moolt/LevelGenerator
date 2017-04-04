using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;

public class HallwayTemplateMeta{
	public GameObject parent;
	public GameObject child;
	public HallwayMask mask;
	public ChunkTags tags;
	public float defaultSortingIndex;
	public float constraintPriority = 0f;
	public List<Constraint> atMostConstraints;

	public HallwayTemplateMeta (GameObject parent){
		this.atMostConstraints = new List<Constraint> ();
		this.defaultSortingIndex = Random.value * .5f;
		this.parent = parent;
		this.child = Child;
		this.tags = child.GetComponent<ChunkTags> ();
		HallwayPrototype prototype = parent.GetComponent<HallwayPrototype> ();
		this.mask = prototype.Mask;
	}

	private GameObject Child{
		get{
			foreach (Transform t in parent.transform) {
				if (t.tag == "HallwayPrototype") {
					return t.gameObject;
				}
			}
			return null;
		}
	}

	public void RegisterAtMostConstraint(Constraint constraint){
		if (!atMostConstraints.Contains (constraint)) {
			atMostConstraints.Add (constraint);
		}
	}

	public void NotifyCreated(){
		atMostConstraints.ForEach (c => c.MatchingHallwayHasBeenUsed());
	}

	public void MarkPositionAsUsed(AStarGrid grid, int[] position){
		mask.MarkPositionAsUsed (grid, position);
	}

	public void ApplyMask(AStarGrid grid){
		mask.ApplyMask (grid);
	}

	public void UpdatePositions(AStarGrid grid){
		mask.UpdateMatches (grid);
	}

	public int[][] Positions{
		get{ return mask.MatchingPositions; }
	}

	public float SortIndex {
		get {
			return defaultSortingIndex + constraintPriority;
		}
	}

	public float ConstraintPriority {
		get {
			return this.constraintPriority;
		}
		set {
			constraintPriority = value;
		}
	}
}

public class HallwayHelper {
	private List<HallwayTemplateMeta> hallwayTemplates; //The masks defined by the user + metadata
	private List<GridPosition> segmentPositions; //Position of each segment, generated beforehand by the A*
	private ChunkInstantiator chunkInstantiator; //Used to instantiate the content of a hallway template
	private List<Constraint> constraints; //List of constraints that only apply to hallways (filtered)
	private LevelGeneratorPreset preset; //Preset, used to obtain constraints
	private GameObject hallwayObject; //The object which holds the geometry of the hallways, used as parent for all segments
	private AStarGrid grid; //The grid used by the AStar algorithm, used to retrieve the segment positions

	public HallwayHelper(AStarGrid grid, LevelGeneratorPreset preset, GameObject hallwayObject){
		this.preset = preset;
		this.chunkInstantiator = ChunkInstantiator.Instance;
		//this.preset = preset;
		this.grid = grid;
		this.hallwayObject = hallwayObject;
		this.hallwayTemplates = new List<HallwayTemplateMeta> ();
		BuildMetadata ();
	}

	private void BuildMetadata(){
		constraints = preset.Constraints.Where (c => c.Target == ConstraintTarget.Hallways).ToList (); //Find hallway constraints
		List<GameObject> _hallways = Resources.LoadAll<GameObject> (GlobalPaths.RelativeHallwayPath).ToList(); //Load all masks defined by the user
		_hallways.RemoveAll (h => h.name == GlobalPaths.NewHallwayName); //Don't consider the default prefab
		_hallways.ForEach (h => hallwayTemplates.Add (new HallwayTemplateMeta (h))); //Create a metadata object from each prefab
		//hallwayTemplates = hallwayTemplates.OrderBy (h => h.SortIndex).ToList();
		segmentPositions = ShuffledSegments; //Obtain a shuffled list of all segments
	}

	private List<GridPosition> ShuffledSegments{
		get{
			List<GridPosition> _positions = grid.UsedPositions;
			_positions = _positions.OrderBy (p => Random.value).ToList();
			return _positions;
		}
	}

	/// <summary>
	/// 1. Iterate through every segment in random order
	/// 2. Check for every mask, wether it applies to the segment
	/// 3. If so, check wether the mask is constrained
	/// 4. Insert the mask (segment) in a list
	/// 5. Shuffle the list and instantiate the random segment
	/// </summary>
	public void InsertHallwaySegments(){
		hallwayTemplates.ForEach (hs => hs.ApplyMask (grid)); //Search applicable positions in the grid for each mask
		InitializedRelativeLimits();
		HandlePrioritizedSegments ();

		foreach (GridPosition segmentPosition in ShuffledSegments) {
			int[] sPos = new int[]{ segmentPosition.i, segmentPosition.j };
			List<HallwayTemplateMeta> masks = new List<HallwayTemplateMeta> ();

			foreach (HallwayTemplateMeta template in hallwayTemplates) {
				if (MaskContainsPosition(template, sPos) && AppliesToAllConstraints(template)) {
					masks.Add (template);
				}
			}

			masks = masks.OrderByDescending (m => m.SortIndex).ToList();
			if (masks.Count > 0) {
				InstantiateHallway (masks [0], sPos);
				UpdateMasks ();
			}
		}

		ResetConstraints ();
	}

	//The priority will change the sorting of the list that contains the masks
	//But since the order of the segments is random, smaller pieces might occupy larger masks, regardles of their priority
	//If the priority is set to a value of anything larger than 0.5, this method will be called instead, ensuring, that all positions
	//Where the mask applies, will be used by the segment. The only exception are other masks with even higher priorities
	private void HandlePrioritizedSegments(){
		ApplyPriorities ();
		List<int[]> badPositions = new List<int[]> (); //Positions that fit the mask but not the constraints
		List<HallwayTemplateMeta> prioritized = hallwayTemplates.Where (ht => ht.ConstraintPriority > 0.5f).ToList();
		prioritized = prioritized.OrderByDescending (p => p.ConstraintPriority).ToList ();
		foreach (HallwayTemplateMeta template in prioritized) {

			//While the mask still has positions and not all of them are badPositions
			//Positions will decrease, since instantiating segments will fill the grid
			while (template.Positions.GetLength(0) != 0 && badPositions.Count < template.Positions.GetLength(0)) {
				foreach (int[] templatePosition in template.Positions) {
					if(!badPositions.Contains(templatePosition)){ //Ensures this position is unused
						if (AppliesToAllConstraints (template)) {
							InstantiateHallway (template, templatePosition);
							UpdateMasks (); //Update masks only if a new segment has been instantiated
						} else {
							badPositions.Add (templatePosition);
						}
						break;
					}
				}
			}
		}
		//Masks with high priority don't have to be considered again
		hallwayTemplates.RemoveAll (ht => prioritized.Contains (ht));
	}

	private void ApplyPriorities(){
		foreach (HallwayTemplateMeta template in hallwayTemplates) {
			constraints.ForEach (c => c.CalculateHallwayPriority (template));
		}
	}

	private void UpdateMasks(){
		hallwayTemplates.ForEach (hs => hs.UpdatePositions (grid));
	}

	private bool MaskContainsPosition(HallwayTemplateMeta template, int[] position){
		return template.Positions.Any (tp => tp [0] == position [0] && tp [1] == position [1]);
	}

	private bool AppliesToAllConstraints(HallwayTemplateMeta templateMeta){
		return constraints.All (c => c.IsConstraintSatisfied (templateMeta));
	}

	private void ResetConstraints(){
		constraints.ForEach (c => c.ResetConstraint ());
	}

	private void InitializedRelativeLimits(){
		List<Constraint> relativeConstraints = constraints
			.Where (c => c.HallwayAmount == HallwayAmount.AtMost && c.AmountType == ConstraintAmountType.Percentual)
			.ToList();
		foreach (Constraint relConstraint in relativeConstraints) {
			HashSet<int[]> applyingPositions = new HashSet<int[]> ();
			hallwayTemplates.Where (ht => relConstraint.IsAffectedByConstraints (ht.tags))
				.ToList ()
				.ForEach (ht => AddToHashSet (applyingPositions, ht.Positions));
			relConstraint.AbsoluteAmount = (int)Mathf.Round (relConstraint.RelativeAmount * applyingPositions.Count);
		}
	}

	private void AddToHashSet(HashSet<int[]> hashSet, int[][] val){
		foreach (int[] _val in val) {
			hashSet.Add (_val);
		}
	}

	private void InstantiateHallway(HallwayTemplateMeta segment, int[] position){
		//Debug.Log ("Hallway Instantiated");
		//Find position
		Vector2 pos2D = grid.Grid [position [0], position [1]].Position;
		Vector3 pos3D = new Vector3 (pos2D.x, 0f, pos2D.y);
		//Vector3 offset = new Vector3 (-1f, 0, 1f) * DoorDefinition.GlobalSize * 0.5f;
		pos3D += segment.child.transform.position - new Vector3(-1f, 0f, -1f) * DoorDefinition.GlobalSize * 0.5f;
		//Instantiate Prefab and position it correctly
		GameObject segmentCopy = GameObject.Instantiate (segment.child);
		segmentCopy.transform.position = pos3D;
		//Set the parent to the hallway object (ensures the segments can be deleted easily later)
		segmentCopy.transform.SetParent(hallwayObject.transform);
		//Instantiate the copy as a Chunk
		chunkInstantiator.ProcessType = ProcessType.GENERATE;
		chunkInstantiator.InstantiateChunk (segmentCopy, true);
		//Update the grid, since positions are now occupied
		segment.MarkPositionAsUsed (grid, position);
		segment.NotifyCreated ();
	}
}
