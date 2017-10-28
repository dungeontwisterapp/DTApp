using UnityEngine;
using System.Collections;

public class HerseBehavior : MonoBehaviour {

	public bool herseBrisee = false;
	public bool herseOuverte = false;

	// La herse passe à l'état Ouvert et son Sprite apparait
	public void ouvrirHerse () {
		herseOuverte = true;
	}
	
	// La herse passe à l'état Fermée et son Sprite disparait
	public void fermerHerse () {
		herseOuverte = false;
	}
	
	// La herse passe à l'état Brisée et son Sprite apparait
	public void briserHerse () {
		herseOuverte = true;
		herseBrisee = true;
	}

}
