using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum DoorAction { CREATE, DELETE }
public enum DoorCheck { AND, OR }

public class LinkedToDoor : ConditionalProperty {
	public DoorAction action;
	public DoorCheck check;
	public string[] doorNames;

	public override void Preview(){}

	public override void Generate(){
		DoorManager doorManager = GetComponentInParent<DoorManager> ();
		if (doorManager != null) {
			List<DoorDefinition> doors = doorManager.RandomDoors;
			string[] allNames = doors.Select (d => d.Name).ToArray ();

			bool and = check == DoorCheck.AND && doorNames.All (d => allNames.Contains (d));
			bool or = check == DoorCheck.OR && doorNames.Any (d => allNames.Contains (d));
			bool conditionTrue = !(and || or);

			if (action == DoorAction.CREATE && conditionTrue) {
				Remove ();
			}

			if (action == DoorAction.DELETE && !conditionTrue) {
				Remove ();
			}
		}
	}
}
