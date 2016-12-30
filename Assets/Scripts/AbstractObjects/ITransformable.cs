using UnityEngine;
using System.Collections;

public interface ITransformable {

	void NotifyBoundsChanged(VariableBounds newBounds);
}
