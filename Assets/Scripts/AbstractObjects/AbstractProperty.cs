using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum PropertyType { INSTANTIATING, TRANSFORMING, MESHGENERATION };

abstract public class AbstractProperty : MonoBehaviour {
	private bool isDirty = false;
	private ICollection<GameObject> generatedObjects = new List<GameObject> ();

	//Dirty flag used for components with delayed removal.
	//Helps the generator to never execute a component twice
	public bool IsDirty { get { return isDirty; } set { isDirty = value; } }
	public abstract int ExecutionOrder { get; }
	//If true, the component only gets removed at the end of the generator process
	//After execution the dirty flag will be set to true
	public abstract bool DelayRemoval { get; }
	//Auto Property storing any Objects generated during generation process
	//Null if it is unused by the inherited component
	public ICollection<GameObject> GeneratedObjects { get{ return generatedObjects; } }

	public abstract void Preview();

	public abstract void Generate();

	public AbstractBounds AbstractBounds{
		get { return GetComponentInParent<AbstractBounds> (); }
	}
	public AbstractBounds ParentsAbstractBounds{
		get { return transform.parent.GetComponentInParent<AbstractBounds> (); }
	}

	public MeshFilter PreviewMesh{
		get{
			MeshFilter meshFilter = GetComponent<MeshFilter> ();
			if (meshFilter == null) {
				WildcardAsset wildcard = gameObject.GetComponent<WildcardAsset> ();
				if (wildcard != null) {
					meshFilter = wildcard.IndexedPreviewMesh;
				}
			}
			return meshFilter;
		}
	}
}

abstract public class InstantiatingProperty : AbstractProperty{
	public override int ExecutionOrder{
		get { return 2; }
	}

	public override bool DelayRemoval{
		get { return false; }
	}
}

abstract public class TransformingProperty : AbstractProperty{
	private AbstractBounds abstractBounds;

	public override int ExecutionOrder{
		get { return 1; }
	}

	public override bool DelayRemoval{
		get { return false; }
	}
}

abstract public class MeshProperty : AbstractProperty{
	public override int ExecutionOrder{
		get { return 3; }
	}

	public override bool DelayRemoval{
		get { return false; }
	}
}