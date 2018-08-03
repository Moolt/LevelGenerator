using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(HallwayPrototype))]
public class HallwayPrototypeEditor : Editor {
	private HallwayPrototype hallway;

	void OnEnable(){
		hallway = target as HallwayPrototype;
	}

	public void OnSceneGUI(){
		hallway.PositionUpdate ();

		int[] gridSize = hallway.GridSize;

		for (int i = 0; i < gridSize[0]; i++) {
			for (int j = 0; j < gridSize[1]; j++) {
				Handles.color = StateColor (i, j);
				Vector3 absolutePosition = hallway.GetPosition (i, j);

				if (hallway.HasAdjacents(i,j) && Handles.Button (absolutePosition, Quaternion.identity, 0.5f, 0.5f, Handles.CubeHandleCap)) {
					hallway.SwitchState (i, j, MaskState.FILL);
					hallway.DrawGeometry ();
					SceneUpdater.UpdateScene ();
				}

                var state = hallway.GetState(i, j);
				if (NeedsToDrawCube(state)) {
                    Handles.color = ColorForState(state);
					Vector3 cubePosition = new Vector3 (absolutePosition.x, DoorDefinition.GlobalSize / 2f, absolutePosition.z);
					Handles.CubeHandleCap (0, cubePosition, Quaternion.identity, DoorDefinition.GlobalSize, EventType.Repaint);
				}
			}
		}
	}

    private bool NeedsToDrawCube(MaskState state)
    {        
        return state == MaskState.EMPTY || state == MaskState.DOOR;
    }

    private Color ColorForState(MaskState state)
    {
        switch(state)
        {
            case MaskState.DOOR: return new Color(0f, 0f, 1f, 0.5f); ;
            case MaskState.EMPTY: return new Color(1f, 0f, 0f, 0.5f); ;
            default: return Color.white;
        }
    }

    private Color StateColor(int i, int j){
		int[] center = hallway.CenterIndices;
		MaskState state = hallway.GetState (i, j);
		if (i == center [0] && j == center [1]) {
			return Color.green;
		} else {
			switch (state) {
			case MaskState.UNUSED:
				return Color.white;
			case MaskState.FILL:
				return Color.cyan;
			case MaskState.EMPTY:
				return Color.red;
            case MaskState.DOOR:
                return Color.blue;
            }
		}
		return Color.white;
	}
}
