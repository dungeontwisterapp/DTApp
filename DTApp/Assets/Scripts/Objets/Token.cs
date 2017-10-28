using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Token : MonoBehaviour {
	
	// Nombre de Points de Victoire donné par le token en sortant du plateau
	public int ESCAPE_VP_VALUE = 0;

	public int referenceIndex;
	public GameObject caseActuelle;
	public GameObject affiliationJoueur;
	public CharacterBehavior tokenHolder; // carrier

	protected GameManager gManager;
	protected GameObject tokenInfo;
	protected GameObject tokenHighlight;
    public bool tokenPlace = false;
    public bool horsJeu = false;
	public int indexToken;
	public List<GameObject> ciblesTokens = new List<GameObject>();
	public PlacementTokens cibleToken;
	// Nombre de cases de déplacement restantes
	public int deplacementRestant;
	public bool selected = false;

    public PlayerBehavior ownerOpponent { get { return gManager.players[1 - indexToken].GetComponent<PlayerBehavior>(); } }

    // Use this for initialization
    public virtual void Start () {
        gManager = GameManager.gManager;
        if (affiliationJoueur == null)
        {
            // Association au Joueur approprié
            GameObject[] players = gManager.players;
            if (players[0].GetComponent<PlayerBehavior>().index == indexToken)
                affiliationJoueur = players[0];
            else affiliationJoueur = players[1];
        }
		// Association du fond de couleur approprié
		tokenInfo = transform.Find("Token_Info").gameObject;
		tokenHighlight = transform.Find("Token_Highlight").gameObject;
	}

    public bool isCharacter() {
        return (GetComponent<CharacterBehavior>() != null);
    }

    public bool isItem()
    {
        return (GetComponent<Item>() != null);
    }

	// Mettre à jour les emplacements de token disponibles
	public virtual void refreshPlacementToken () {
        ciblesTokens.Clear();
        ciblesTokens.AddRange(GameObject.FindGameObjectsWithTag("PlacementToken"));
	}
	
	// Place le token sur une case donnée
	public void placeToken (PlacementTokens cible) {
        // On lie le token et l'emplacement
        cible.tokenAssociated = gameObject;
		cibleToken = cible;
		// Si placement de départ sur les salles, on valide automatiquement le placement et on change de joueur
        if (gManager.gameMacroState == GameProgression.TokenPlacement)
        {
            Debug.Assert(cible.transform.parent != null);
            gManager.onlineGameInterface.RecordPlacement(this, cible);
            cible.locked = true;
            if (gManager.isActivePlayer(affiliationJoueur)) gManager.SendMessage("oneTokenPlaced");
		}
	}

    public void placeTokenOnCell(CaseBehavior cell)
    {
        tokenPlace = true;
        caseActuelle = cell.gameObject;
        cibleToken = null;
        ciblesTokens.Clear();
    }

    public string getTokenName()
    {
        //return (gameObject.name.Split('_')[1].Split('(')[0]);
        return name.Replace("IT_", "").Replace("CT_", "").Replace("(Clone)", ""); // safer way working with tutorial
    }

    public int getOwnerIndex()
    {
        Debug.Assert(affiliationJoueur != null);
        return affiliationJoueur.GetComponent<PlayerBehavior>().index;
    }

    public string getOwnerId()
    {
        Debug.Assert(affiliationJoueur != null);
        return affiliationJoueur.GetComponent<PlayerBehavior>().onlineID;
    }

    public virtual bool isRemovedFromGame()
    {
        return horsJeu;
    }

    public bool isTokenOnBoard()
    {
        return tokenPlace && !isRemovedFromGame();
    }

    public void setPosition(Vector3 newPosition)
    {
        // set new position but keep the coordinate z to 0
        transform.position = new Vector3(newPosition.x, newPosition.y, 0);
    }

    #region movement
	// Vérifie si l'on peut s'arreter sur la case
	public virtual List<GameObject> calculDeplacement (int nbCases, int row, int column) {
		return calculDeplacement(nbCases, row, column, false);
	}

    public bool surLaRegletteAdverse(CaseBehavior currentCase)
    {
        return (affiliationJoueur != currentCase.affiliationJoueur && currentCase.affiliationJoueur != null);
    }

	// Vérifie si l'on peut s'arreter sur la case, éventuellement dans le cadre d'un saut
	public virtual List<GameObject> calculDeplacement (int nbCases, int row, int column, bool jump) {
        // TODO
		return new List<GameObject>();
	}

    public void moveTargetActivation(int deplacementRestant, CaseBehavior cell, bool jump)
    {
        CibleDeplacement cible = cell.cibleAssociated.GetComponent<CibleDeplacement>();
        if (!gManager.casesAtteignables.Contains(cible.gameObject))
        {
            gManager.casesAtteignables.Add(cible.gameObject);
        }
        cible.tokenAssociated = gameObject;
        cible.nbDeplacementRestant = deplacementRestant;
        if (jump) cible.GetComponent<CibleDeplacementIHM>().jumpingTarget = true;
        if (!gManager.freezeDisplay) cible.SendMessage("displayTarget");
    }

    public virtual int sortiePlateau()
    {
        // Give affiliated player VP and remove the token from the board
        affiliationJoueur.GetComponent<PlayerBehavior>().victoryPoints += ESCAPE_VP_VALUE;
        caseActuelle = null;
        horsJeu = true;
        return ESCAPE_VP_VALUE;
    }

	// Remettre le système à l'état initial et retirer un point d'action du joueur actuel
	public virtual void actionDone ()
    {
        Debug.LogWarning("Token: Action Done");
        if (gManager.actionPointCost <= 0)
        {
            Debug.LogWarning("ActionPointCost is not up to date for current Action. Change from " + gManager.actionPointCost + " to " + 1);
            gManager.actionPointCost = 1;
        }
        while (gManager.actionPointCost > 0) gManager.decreaseActionPoints();
		gManager.selectionEnCours = false;
		selected = false;
	}

	// Annule le déplacement en cours
	public virtual void deplacementCancelled () {
		clearDeplacementHUD();
	}

	// Retirer du plateau les cibles de déplacement
	public void clearDeplacementHUD () {
		if (!gManager.deplacementEnCours) gManager.resetMoveFlags();
		else gManager.resetPartialMoveFlags();
	}

    #endregion

}
