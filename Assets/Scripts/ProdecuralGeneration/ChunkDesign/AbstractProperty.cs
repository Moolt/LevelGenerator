using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;

abstract public class AbstractProperty : MonoBehaviour {
	private bool isDirty = false;
	private ICollection<GameObject> generatedObjects = new List<GameObject> ();
	[SerializeField]
	private GizmoPreviewState previewState = GizmoPreviewState.ONSELECTION;
	private bool hasBeenDeleted = false;
	//Dirty flag used for components with delayed removal.
	//Helps the generator to never execute a component twice
	public bool IsDirty { get { return isDirty; } set { isDirty = value; } }
	public abstract float ExecutionOrder { get; }
	//If true, the component only gets removed at the end of the generator process
	//After execution the dirty flag will be set to true
	//public abstract bool DelayRemoval { get; }
	public abstract RemovalTime RemovalTime { get; }
	//Auto Property storing any Objects generated during generation process
	//Null if it is unused by the inherited component
	public ICollection<GameObject> GeneratedObjects { get{ return generatedObjects; } }

	public abstract void Preview();

	public abstract void Generate();

	public AbstractBounds AbstractBounds{
		get { return GetComponentInParent<AbstractBounds> (); }
	}
	public AbstractBounds ParentsAbstractBounds{
		get { 
			if (transform.parent != null) {
				return transform.parent.GetComponentInParent<AbstractBounds> (); 
			} else {
				return null;
			}
		}
	}

	public Vector3 FindAbsoluteScale(Transform t){
		Vector3 tmpScale = Vector3.one;
		Transform tmpTransform = t;

		while (tmpTransform != null) {
			tmpScale = Vector3.Scale (tmpScale, tmpTransform.localScale);
			tmpTransform = tmpTransform.parent;
		}
		return tmpScale;
	}

	public MeshFilter PreviewMesh{
		get{
			if (PreviewMeshes.Length > 0) {
				return PreviewMeshes [0];
			}
			MeshFilter meshFilter = new MeshFilter ();
			return meshFilter;
		}
	}

	public MeshFilter[] PreviewMeshes{
		get{
			WildcardAsset wildcard = gameObject.GetComponentInChildren<WildcardAsset> ();
			if (wildcard != null) {
				return wildcard.IndexedPreviewMeshes;
			} else {
				return GetComponentsInChildren<MeshFilter> ();
			}
		}
	}

	public GizmoPreviewState GizmoPreviewState{
		get{ return previewState; }
		set{ previewState = value; }
	}

	public virtual void DrawEditorGizmos (){
	}

	void OnDrawGizmos(){
		if (previewState == GizmoPreviewState.ALWAYS) {
			DrawEditorGizmos ();
		}
	}

	void OnDrawGizmosSelected(){
		if (previewState == GizmoPreviewState.ONSELECTION) {
			DrawEditorGizmos ();
		}
	}

	//Should be called before using the PreviewMesh AutoProp to avoid nullref
	public bool MeshFound(){
		return PreviewMesh != null && PreviewMesh.sharedMesh != null;
	}

	public bool HasBeenDeleted {
		get {
			return this.hasBeenDeleted;
		}
		set {
			hasBeenDeleted = value;
		}
	}
}

abstract public class ValueProperty : AbstractProperty{
	public override float ExecutionOrder{
		get { return 0.2f; }
	}

	public override RemovalTime RemovalTime{
		get { return RemovalTime.INSTANTLY; }
	}
}

abstract public class InstantiatingProperty : AbstractProperty{
	public override float ExecutionOrder{
		get { return 0.3f; }
	}

	public override RemovalTime RemovalTime{
		get { return RemovalTime.INSTANTLY; }
	}
}

abstract public class TransformingProperty : AbstractProperty{
	private AbstractBounds abstractBounds;

	public override float ExecutionOrder{
		get { return 2; }
	}

	public override RemovalTime RemovalTime{
		get { return RemovalTime.INSTANTLY; }
	}
}

[DisallowMultipleComponent]
abstract public class DoorProperty : AbstractProperty{
	public override float ExecutionOrder{
		get { return 3.9f; }
	}

	public override RemovalTime RemovalTime{
		get { return RemovalTime.MANUAL; }
	}
}

[DisallowMultipleComponent]
abstract public class TagProperty : AbstractProperty{
	public override float ExecutionOrder{
		get { return 0; }
	}

	public override RemovalTime RemovalTime{
		get { return RemovalTime.MANUAL; }
	}
}

abstract public class ConditionalProperty : AbstractProperty{

	protected virtual void Remove(){
		List<AbstractProperty> props = GetComponents<AbstractProperty> ().ToList();
		props.ForEach (p => p.HasBeenDeleted = true);
		DestroyImmediate (transform.gameObject);
	}

	public override float ExecutionOrder{
		get { return 0.1f; }
	}

	public override RemovalTime RemovalTime{
		get { return RemovalTime.INSTANTLY; }
	}
}

[DisallowMultipleComponent]
abstract public class MeshProperty : AbstractProperty{
	public override float ExecutionOrder{
		get { return 4; }
	}

	public override RemovalTime RemovalTime{
		get { return RemovalTime.INSTANTLY; }
	}
}

[DisallowMultipleComponent]
abstract public class MultiplyingProperty : InstantiatingProperty{
	
	protected abstract Vector3[] CalculatePositions ();

	public override void Generate(){
		Vector3[] copyPositions = CalculatePositions ();

		if (copyPositions.Length > 0) {
			transform.position = copyPositions [0];

			for (int i = 1; i < copyPositions.Length; i++) {
				GameObject copy = GameObject.Instantiate (gameObject);
				//Array needs to be removed, since the copies are not under control of the Generator yet to handle removal
				DestroyImmediate (copy.GetComponent<MultiplyingProperty> ());
				copy.transform.position = copyPositions [i];
				copy.transform.SetParent (gameObject.transform.parent);
				GeneratedObjects.Add (copy);
			}
		}
	}

	protected Vector3 PreviewBounds(bool applyScale){
		if (PreviewMeshes.Length > 0) {
			Vector3 bounds = Vector3.zero;
			foreach (MeshFilter meshFilter in PreviewMeshes) {
				if (meshFilter.sharedMesh != null) {
					Vector3 meshSize = meshFilter.sharedMesh.bounds.size;
					meshSize = applyScale ? Vector3.Scale (meshSize, FindAbsoluteScale (meshFilter.transform)) : meshSize;
					bounds.x = Mathf.Max (meshSize.x, bounds.x);
					bounds.y = Mathf.Max (meshSize.y, bounds.y);
					bounds.z = Mathf.Max (meshSize.z, bounds.z);
				}
			}
			List<AbstractBounds> abstractBounds = GetComponentsInChildren<AbstractBounds> ().ToList();
			foreach (AbstractBounds ab in abstractBounds) {
				ab.Preview ();
				bounds.x = Mathf.Max (ab.Size.x, bounds.x);
				bounds.y = Mathf.Max (ab.Size.y, bounds.y);
				bounds.z = Mathf.Max (ab.Size.z, bounds.z);
			}
			return bounds;
		}
		return Vector3.one;
	}

	protected bool HasMultiplyingParents(){
		List<MultiplyingProperty> mulProps = GetComponentsInParent<MultiplyingProperty> ().ToList ();
		return mulProps.Any (mp => mp.gameObject != gameObject);
	}

	protected bool HasMultiplyingChildren(){
		List<MultiplyingProperty> mulProps = GetComponentsInChildren<MultiplyingProperty> ().ToList ();
		return mulProps.Any (mp => mp.gameObject != gameObject);
	}
		
	protected List<Vector3> RecursivePreviewPositions(){
		return CalculatePositions ().ToList();
		/*if (HasMultiplyingParents ()) {
			return new List<Vector3> ();
		} else if (!HasMultiplyingChildren ()) {
			return CalculatePositions ().ToList();
		}

		List<MultiplyingProperty> mulProps = GetComponentsInChildren<MultiplyingProperty> ().ToList (); //Also contains this object
		List<List<Vector3>> positionLists = mulProps.Select(mp => mp.CalculatePositions().ToList()).ToList();

		for (int i = 0; i < mulProps.Count; i++) {
			for (int j = 0; j < positionLists [i].Count; j++) {
				positionLists [i] [j] = mulProps[].transform.position - positionLists [i] [j];
			}
		}

		List<Vector3> completeList = new List<Vector3> (positionLists[0]);
		for (int i = 1; i < positionLists.Count; i++) {
			List<Vector3> workingList = new List<Vector3> (completeList);
			completeList.Clear ();
			List<Vector3> newPositions = new List<Vector3> ();
			for (int j = 0; j < positionLists [i].Count; j++) {
				workingList.ForEach (p => newPositions.Add (positionLists [i][j] + p));
			}
			completeList.AddRange (newPositions);
		}

		//completeList.ForEach (p => p += transform.position);
		*/
		//return completeList;
	}

	public override float ExecutionOrder{
		get { return 1f; }
	}
}