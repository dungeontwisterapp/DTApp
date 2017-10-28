using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Item : Token {

	public bool consommable = false;
	// Est-ce que l'objet donne un bonus en combat
	public bool combatItem = false;

	// Valeur du bonus en combat de l'objet
	public int attackItemBonus = 0;
	public int defenseItemBonus = 0;

	// Use this for initialization
	public override void Start () {
		base.Start();
	}

	// Donne le bonus de combat de l'objet, en attaque ou en défense
	public int getCombatBonus (bool attackBonusAsked) {
		// Si l'objet n'a pas d'utilité en combat, on renvoit un bonus de 0
		if (!combatItem) return 0;
		if (attackBonusAsked) return attackItemBonus;
		else return defenseItemBonus;
	}

	// Appelé à chaque fin d'action, utilisé par certains objets
	public virtual void clearUnresolvedActions () {
		Debug.Log("Item, clearUnresolvedActions: Pas de variable à réinitialiser");
	}

	// Retire l'objet du plateau
	public void destroyItem () {
		// Si l'objet est tenu par un personnage, ce dernier lache l'objet
        if (tokenHolder != null)
        {
            tokenHolder.GetComponent<CharacterBehaviorIHM>().deposerToken(false);
        }
        caseActuelle = null;
        horsJeu = true;
	}
}
