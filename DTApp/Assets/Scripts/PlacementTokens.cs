using UnityEngine;
using System.Collections;

public class PlacementTokens : MonoBehaviour {

	// Donne l'information si le placement du token associé a été validé (TRUE) ou non (FALSE)
	public bool locked = false;
	// La case à laquelle est associé la cible de placement de token, reste vide si la cible est associée à une salle
	public CaseBehavior caseActuelle;
	// Le token avec lequel est associé la cible
	public GameObject tokenAssociated;

}
