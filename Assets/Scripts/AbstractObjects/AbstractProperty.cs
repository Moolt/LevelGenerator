using UnityEngine;
using System.Collections;

public enum PropertyType { INSTANTIATING, TRANSFORMING, MESHGENERATION };

abstract public class AbstractProperty : MonoBehaviour {

	public abstract int ExecutionOrder { get; }
	public abstract bool DelayRemoval { get; }

	public abstract void Preview();

	public abstract GameObject[] Generate();

	public AbstractBounds AbstractBounds{
		get { return GetComponentInParent<AbstractBounds> (); }
	}
}

abstract public class InstantiatingProperty : AbstractProperty{
	public override int ExecutionOrder{
		get { return 1; }
	}

	public override bool DelayRemoval{
		get { return false; }
	}
}

abstract public class TransformingProperty : AbstractProperty{
	private AbstractBounds abstractBounds;

	public override int ExecutionOrder{
		get { return 2; }
	}

	public override bool DelayRemoval{
		get { return true; }
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