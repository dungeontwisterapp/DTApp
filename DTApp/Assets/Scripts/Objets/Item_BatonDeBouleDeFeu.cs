using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Item_BatonDeBouleDeFeu : Item {

	public List<GameObject> ciblesBouleDeFeu = new List<GameObject>();
	public bool targetAcquired = false;
	// Game Object temporaire pour regrouper les Sprites au-dessus des cibles de la boule de feu
    public GameObject iconHolder;
    [HideInInspector]
    public GameObject associatedFireball;
	
	public override void Start () {
		base.Start();
		consommable = true;
    }

    public void endfireballaction()
    {
        tokenHolder.endDeplacement(); // the corresponding endDeplacementIHMOnly has been called in fireball
    }
	
	// Préparer le lancement de la boule de feu
	public void readyFireball() {
		gManager.usingSpecialAbility = true;
		targetAcquired = true;
	}

	// Lister les cibles potentielles de la boule de feu
	public void fireballGetTargets() {
		// Si aucune cible n'a été listée
		if (ciblesBouleDeFeu.Count == 0) {
            List<CaseBehavior> cellsHoldingEnemies = tokenHolder.pathfinder.enemyCharactersOnSight(caseActuelle.GetComponent<CaseBehavior>());
            foreach (CaseBehavior cell in cellsHoldingEnemies)
            {
                CharacterBehavior character = cell.getMainCharacter();
                Debug.Assert(character != null);
                ciblesBouleDeFeu.Add(character.gameObject);
            }
		}
		else {
			Debug.LogWarning("Item BatonDeBouleDeFeu, fireballGetTargets: Des cibles sont déjà acquises");
		}
	}

	// Vide la liste de cible dès qu'une action a été complétée par le personnage tenant la boule de feu
	public override void clearUnresolvedActions () {
		if (iconHolder != null) Destroy(iconHolder);
		gManager.usingSpecialAbility = false;
		targetAcquired = false;
        if (associatedFireball != null) associatedFireball.SendMessage("removeFX");
		ciblesBouleDeFeu.Clear();
	}
}
