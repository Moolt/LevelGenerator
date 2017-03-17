using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LinkedToDoor : ConditionalProperty {
	public string doorName = "";

	public override void Preview(){}

	public override void Generate(){
		DoorManager doorManager = GetComponentInParent<DoorManager> ();
		if (doorManager != null) {
			List<DoorDefinition> doors = doorManager.RandomDoors;

			if (!doors.Any (d => d.Name == doorName)) {
				List<AbstractProperty> props = GetComponents<AbstractProperty> ().ToList();
				props.ForEach (p => p.HasBeenDeleted = true);
				DestroyImmediate (transform.gameObject);
			}
		}
	}
}
