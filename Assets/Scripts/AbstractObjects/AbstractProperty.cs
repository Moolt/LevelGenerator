using UnityEngine;
using System.Collections;

public enum PropertyType { INSTANTIATING, TRANSFORMING, MESHGENERATION };

abstract public class AbstractProperty : MonoBehaviour {

	public abstract int ExecutionOrder { get; }

	public abstract void Preview();

	public abstract GameObject[] Generate();

}

abstract public class InstantiatingProperty : AbstractProperty{
	public override int ExecutionOrder{
		get { return 1; }
	}
}

abstract public class TransformingProperty : AbstractProperty{
	public override int ExecutionOrder{
		get { return 2; }
	}
}

abstract public class MeshProperty : AbstractProperty{
	public override int ExecutionOrder{
		get { return 3; }
	}
}