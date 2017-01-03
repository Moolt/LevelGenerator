using UnityEngine;
using System.Collections;

public interface ITransformable {

	void NotifyBoundsChanged(AbstractBounds newBounds);
}
