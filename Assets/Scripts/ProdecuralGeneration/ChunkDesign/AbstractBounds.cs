using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ChunkBoundsHelper{
	public Vector3 MinSize;
	public Vector3 MaxSize;
	public Vector3 Size;

	public ChunkBoundsHelper(Vector3 min, Vector3 max, Vector3 size){
		Size = size;
		UpdateValues (min, max);
	}

	public void UpdateValues (Vector3 min, Vector3 max){
		MinSize = min;
		MaxSize = max;
	}

	public Vector3 RoundedMin {
		get {
			return RoundVector (MinSize);
		}
	}

	public Vector3 RoundedMax {
		get {
			return RoundVector (MaxSize);
		}
	}

	public Vector3 RoundedSize {
		get {
			return RoundVector (Size);
		}
	}

	public static Vector3 RoundVector(Vector3 input){
		float factor = DoorDefinition.GlobalSize * 2f;
		input.x = Mathf.Max (factor / 2f, RoundTo (input.x, factor));
		input.y = Mathf.Max (factor / 2f, RoundTo (input.y, factor / 4f));
		input.z = Mathf.Max (factor / 2f, RoundTo (input.z, factor));
		return input;
	}

	public static float RoundTo(float value, float roundTo){
		return Mathf.Round (value / roundTo) * roundTo;
	}
}

[DisallowMultipleComponent]
public class AbstractBounds : TransformingProperty {
	public Vector3 minSize = Vector3.one;
	public Vector3 maxSize = Vector3.one * DoorDefinition.GlobalSize * 2f;
	public bool hasFixedSize;
	[Range(0f, 1f)]
	public float lerp;
	public bool keepAspectRatio;
	public AbstractBounds adaptToParent = null;
	[SerializeField]
	public ChunkBoundsHelper chunkBounds;

	[SerializeField]
	private StretchInfo[] stretchInfos = null;
	private Vector3 size;
	private List<Vector3> corners;

	public AbstractBounds(){
		chunkBounds = new ChunkBoundsHelper (minSize, maxSize, size);
	}

	public override void DrawEditorGizmos(){
		Gizmos.color = (hasFixedSize) ? Color.yellow : Color.white;
		Vector3 pos = transform.position;
		float originalY = pos.y;

		//MinSize is both used for min and fidexSize
		//The Inspector will change the label to "Size" if fixedSize is true
		//However, this vector and preview is used for both
		pos.y = (IsChunk || IsHallway) ? MinSize.y / 2f + originalY : originalY;
		Gizmos.color = (hasFixedSize) ? Color.cyan : Color.yellow;
		Gizmos.DrawWireCube (pos, MinSize);

		//Don't draw maxSize if the size is fixed
		if (!hasFixedSize) {
			pos.y = (IsChunk) ? MaxSize.y / 2f + originalY : originalY;
			Gizmos.color = Color.red;
			Gizmos.DrawWireCube (pos, MaxSize);
		}

		pos.y = (IsChunk || IsHallway) ? Size.y / 2f + originalY: originalY;
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube (pos, size);
	}

	public override void Preview(){
		UpdateScaling (false);
		ClampValues ();
		ChunkPositionAtOrigin ();
	}

	public override void Generate(){
		//RandomizeSize (null); //null because the children dont have to be updated
		lerp = Random.value;
		UpdateScaling (true);
	}

	public Vector3 Size{ 
		get { return size; } 
	}

	public Vector3 Extends{ 
		get { return Size * .5f; }
	}

	public Vector3 Center{
		get{
			return transform.position + new Vector3(0f, size.y / 2f, 0f);
		}
	}

	public Vector3 MinSize{
		get{ 
			return minSize + Vector3.Scale (size - minSize, LockedAxes); 
		}
		set{
			minSize = value;
		}
	}

	public Vector3 MaxSize{
		get{ 
			return maxSize + Vector3.Scale (size - maxSize, LockedAxes); 
		}
		set{
			maxSize = value;
		}
	}

	public bool IsChunk{
		get{
			return gameObject.tag.StartsWith ("Chunk");
		}
	}

	public bool IsHallway{
		get{ return gameObject.tag == "HallwayPrototype"; }
	}

	public StretchInfo[] StretchInfos{
		get{
			if (stretchInfos == null) {
				stretchInfos = new StretchInfo[] {
					new StretchInfo (false, Vector3.right, true, 1f, "X"),
					new StretchInfo (false, Vector3.up, true, 1f, "Y"),
					new StretchInfo (false, Vector3.forward, true, 1f, "Z")
				};
			}
			return stretchInfos;
		}
	}

	//Calculates 27 corners which fully define the bounds
	//Used by docking component
	public Vector3[] Corners {
		get {
			//Only recalculate if the bounds changed since the last time
			corners = new List<Vector3> ();

			Vector3 point = new Vector3 (Extends.x,  Extends.y, Extends.z);

			//6  7  8 -> 15  16  17 -> 24 25 26
			//3  4  5 -> 12  13  14 -> 21 22 23
			//0  1  2 -> 9   10  11 -> 18 19 20
			for (int i = 0; i < 3; i++) {
				for (int j = 0; j < 3; j++) {
					for (int k = 0; k < 3; k++) {
						corners.Add (new Vector3 (point.x * (k - 1), point.y * i, point.z * (j - 1)) + transform.position);
					}
				}
			}
			return corners.ToArray ();
		}
	}

	public int[] CornerIndicesByDirection(Vector3 direction){
		if (direction == Vector3.right) {
			return new int[] { 8, 5, 2, 17, 14, 11, 26, 23, 20 };
		} else if (direction == Vector3.left) {
			return new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24 };
		} else if (direction == Vector3.forward) {
			return new int[] { 6, 7, 8, 15, 16, 17, 24, 25, 26 };
		} else if (direction == Vector3.back) {
			return new int[] { 2, 1, 0, 11, 10, 9, 20, 19, 18 };
		}
		return new int[0];
	}

	public int CornersHeightDegree(int cornerIndex){
		if (cornerIndex < 9) {
			return 0;
		} else if (cornerIndex < 18) {
			return 1;
		} else {
			return 2;
		}
	}

	public int ChangeCornersHeight(int originalCorner, int targetHeight){
		int originalHeight = CornersHeightDegree (originalCorner);
		int difference = targetHeight - originalHeight;
		return originalCorner + 9 * difference;
	}

	public Vector3 FindCorner(int relativeIndex, Vector3 direction){
		int[] absoluteIndices = CornerIndicesByDirection (direction);
		return Corners [absoluteIndices [relativeIndex]];
	}

	public Vector3[] RelativeCorners(params int[] corners){
		List<Vector3> relCorners = new List<Vector3> ();
		foreach (int index in corners) {
			relCorners.Add (Corners[index] - transform.position);
		}
		return relCorners.ToArray ();
	}

	private void UpdateScaling(bool randomize){
		if (ParentsAbstractBounds != null) {
			Vector3 otherSize = ParentsAbstractBounds.Size;
			Vector3 stretchSize = Vector3.Scale (otherSize, StretchScaleVector);
			Vector3 definedSize = hasFixedSize ? minSize : RandomizeVector(randomize, MinSize, MaxSize);

			size += Vector3.Scale (stretchSize - size, LockedAxes);
			size += Vector3.Scale (definedSize - size, Vector3.one - LockedAxes);
		} else if (IsChunk || IsHallway) {
			//Set size to minSize if the size is fixed. Else: lerp between min / max
			size = hasFixedSize ? minSize : RandomizeVector(randomize, MinSize, MaxSize);
			if (IsChunk) {
				ApplyRoundedChunkValues ();
			}
		}
	}

	private void ApplyRoundedChunkValues(){
		//chunkBounds.UpdateValues (minSize, maxSize);
		this.size = ChunkBoundsHelper.RoundVector (size);
		//this.size = chunkBounds.RoundedSize;
		this.minSize = chunkBounds.RoundedMin;
		this.maxSize = chunkBounds.RoundedMax;
		ClampChunkValues ();
	}

	private Vector3 RandomizeVector(bool randomize, Vector3 min, Vector3 max){
		if (keepAspectRatio || !randomize) {
			float lerpVal = randomize ? Random.value : lerp;
			return Vector3.Lerp (min, max, lerpVal);
		} else {
			Vector3 result = Vector3.zero;
			result.x = Mathf.Lerp (min.x, max.x, Random.value);
			result.y = Mathf.Lerp (min.y, max.y, Random.value);
			result.z = Mathf.Lerp (min.z, max.z, Random.value);
			return result;
		}
	}

	private void ClampChunkValues(){
		chunkBounds.MaxSize = Clamp (chunkBounds.MaxSize, chunkBounds.MinSize, chunkBounds.MaxSize);
		chunkBounds.MinSize = Clamp (chunkBounds.MinSize, chunkBounds.MinSize, chunkBounds.MaxSize);
		//chunkBounds.UpdateValues (minSize, maxSize, size);
	}

	private void ClampValues(){
		Vector3 requiredSpace = Vector3.zero;

		if (IsConstrainedByDoors) {
			DoorManager doorManager = GetComponent<DoorManager> ();
			requiredSpace = doorManager.RequiredSpace;
		}

		if (IsChunk) {
			this.transform.localScale = Vector3.one;
		}

		maxSize = Clamp (maxSize, MinSize, maxSize);
		minSize = Clamp (minSize, requiredSpace, MaxSize);
		//chunkBounds.UpdateValues (minSize, maxSize, size);
	}

	public Vector3 LockedAxes {
		get {
			Vector3 axisLock = Vector3.zero;
			for (int i = 0; i < 3; i++) {
				if (StretchInfos.Length == 3 && StretchInfos [i].Active) {
					axisLock += StretchInfos [i].Direction.normalized;
				}
			}
			return axisLock;
		}
	}

	public Vector3 StretchScaleVector {
		get {
			Vector3 stretchVector = Vector3.zero;
			stretchVector.x = StretchInfos [0].Percent;
			stretchVector.y = StretchInfos [1].Percent;
			stretchVector.z = StretchInfos [2].Percent;
			return stretchVector;
		}
	}

	private void ChunkPositionAtOrigin(){
		if (IsChunk) {
			transform.position = Vector3.zero;
		}
	}

	public override RemovalTime RemovalTime{
		get { return RemovalTime.DELAYED; }
	}

	public bool IsConstrainedByDoors{
		get{ return gameObject.GetComponent<DoorManager> () != null; }
	}

	private Vector3 Clamp(Vector3 value, Vector3 min, Vector3 max){
		return new Vector3 (Mathf.Clamp (value.x, min.x, max.x), Mathf.Clamp (value.y, min.y, max.y), Mathf.Clamp (value.z, min.z, max.z));
	}
}
