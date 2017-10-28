using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TileBehavior : MonoBehaviour {

	public bool hidden = true;
	// Si une salle peut etre révélée par une action
	public bool canShow = false;
	// Si une salle peut etre tournée par une action
	public bool canRotate = false;
	// Est-ce que la salle tourne dans le sens des aiguilles d'une montre (TRUE) ou non (FALSE)
	public bool clockwiseRotation = true;
	// Rotation actuelle de la salle, valeurs allant de 0 (état normal) à 3
	public int tileRotation = 0;
	// Position de la salle sur le plateau
	public int index = -1;
	// Numéro de la salle
	public int tilePairing;
    // Tutoriel : Référence de position de la tile sur la matrice de cases du Game Manager
    public int[] topLeftCasePositionInMatrix;

	//public List<GameObject> tokensAPlacer = new List<GameObject>();

	// Use this for initialization
    void Start()
    {
        if (GameManager.gManager.app.gameToLaunch.isTutorial && tileRotation != 0) Invoke("nonStandardStartingRotation", 0.5f);
	}

    void nonStandardStartingRotation()
    {
        int cpt = 0;
        int intendedStartingRotation = tileRotation;
        tileRotation = 0;
        do
        {
            rotateTile();
            cpt++;
            if (cpt > 3) break;
        }
        while (tileRotation != intendedStartingRotation);
    }

    // Renvoie l'autre salle portant le même numéro
    public GameObject getSisterTile()
    {
        //Debug.Log("Tile Behavior, getSisterTile: Search for sister of " + getTileName() + " of pairing " + tilePairing);
        foreach (var tileObject in GameObject.FindGameObjectsWithTag("Tile"))
        {
            TileBehavior tile = tileObject.GetComponent<TileBehavior>();
            if (tile.getTileName() != getTileName() && tile.tilePairing == tilePairing)
            {
                return tileObject;
            }
        }
        Debug.LogWarning("Tile Behavior, assignSisterTile: Tile associée non trouvée");
        return null;
	}

	public bool canRotateSisterTile () {
        GameObject sisterTile = getSisterTile();
		return (sisterTile == null) ? false : !sisterTile.GetComponent<TileBehavior>().hidden;
	}

    // Faire tourner la salle d'un cran dans son sens prédéfini avec affichage
    public void rotateTile()
    {
        rotateTile(true);
    }

	// Faire tourner la salle d'un cran dans son sens prédéfini
    public void rotateTile(bool displayFeedback)
    {
        rotateTile(false, displayFeedback);
	}

	// Faire tourner la salle d'un cran dans un sens donné
    public void rotateTile(bool sensInverse, bool displayFeedback)
    {
		// Si on souhaite faire tourner la salle dans son sens inverse, on inverse momentanément son sens
		if (sensInverse) clockwiseRotation = !clockwiseRotation;
		if (clockwiseRotation) {
			if (tileRotation < 3) tileRotation++;
			else tileRotation = 0;
		}
		else {
			if (tileRotation > 0) tileRotation--;
			else tileRotation = 3;
		}
        GameManager.gManager.rotateTile(gameObject, displayFeedback);
		// Une fois la rotation effetuée, on rétabli le sens de rotation initial s'il a été modifié
        if (sensInverse) clockwiseRotation = !clockwiseRotation;
	}

	// Rend la rotation possible pour cette salle
	public void rotationPossible () {
		canRotate = true;
        GameObject sisterTile = getSisterTile();
        if (sisterTile != null) sisterTile.GetComponent<TileBehavior>().canRotate = true;
	}

	// Mettre à jour la position des tokens sur la salle après rotation
	void refreshTokenPositions () {
		for (int i=0 ; i < transform.childCount ; i++) {
            if (transform.GetChild(i).gameObject.name != "Herse" && transform.GetChild(i).gameObject.name != "Rotation Arrow") transform.GetChild(i).gameObject.SendMessage("refreshTokenPositions");
		}
    }

    public string getTileName()
    {
        return name.Replace("BB_", "").Replace("(Clone)", "");
    }

    // Lister chaque case valide de la salle pour poser un pion
    public List<CaseBehavior> getAvailableCells()
    {
        List<CaseBehavior> availableCells = new List<CaseBehavior>();
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).GetComponent<CaseBehavior>() != null)
            {
                CaseBehavior currentCase = transform.GetChild(i).GetComponent<CaseBehavior>();
                if (currentCase.isAvailable()) availableCells.Add(currentCase);
            }
        }
        return availableCells;
    }
    
    // Update Tile cells
    public void updateTileCells()
    {
        GameManager.gManager.updateTileCells(this);
    }
}
