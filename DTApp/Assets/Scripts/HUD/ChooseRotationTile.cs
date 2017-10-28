using UnityEngine;
using System.Collections;

public class ChooseRotationTile : MonoBehaviour {

	[HideInInspector]
	public TileBehaviorIHM tile;
	bool tileChosen = false;

	void OnMouseDown () {
		if (!tileChosen) {
			tileChosen = true;
			tile.enableTileRotation();
		}
	}

}
