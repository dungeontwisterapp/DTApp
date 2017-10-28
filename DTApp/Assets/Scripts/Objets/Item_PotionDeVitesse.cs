using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Item_PotionDeVitesse : Item {

	public override void Start () {
		base.Start();
		consommable = true;
	}

	// Termine l'action, puis ajoute quatre points d'action au personnage utilisant la potion avant de consommer l'item
	public void applySpeedPotion() {
		// Signale au GameManager que des points d'action supplémentaires ont été donnés
		tokenHolder.actionPoints += 4;
    }
}
