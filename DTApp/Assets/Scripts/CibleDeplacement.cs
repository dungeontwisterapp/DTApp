using UnityEngine;
using System.Collections;

public class CibleDeplacement : MonoBehaviour {

	public GameObject caseAssociated;
	public GameObject tokenAssociated;
	public int nbDeplacementRestant = 0;


	public void resetMovePossibility () {
		tokenAssociated = null;
		nbDeplacementRestant = 0;
	}

}
