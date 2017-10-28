using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CaseBehavior : MonoBehaviour {

	public enum typeCase {caseVide, caseFosse, caseRotation};
	public enum cheminCaseAdjacente {vide, mur, herse};

	public typeCase type;

	public cheminCaseAdjacente versHaut;
	public cheminCaseAdjacente versGauche;
    public cheminCaseAdjacente versBas;
    public cheminCaseAdjacente versDroite;

    public cheminCaseAdjacente vers(int dir)
    {
        //Debug.Log("dir " + dir + " mod " + mod4(dir));
        switch( mod4(dir) )
        {
            default: 
            case 0: return versHaut;
            case 1: return versGauche;
            case 2: return versBas;
            case 3: return versDroite;
        }
    }

    private static int modulo(int a, int b) { return ((a % b) + b) % b; }
    private static int mod4(int a) { return modulo(a, 4); }

    private int index;
	public bool selectionnePourDeplacement = false;
	public GameObject cibleAssociated;
	public GameObject herse;
	public GameObject affiliationJoueur;
	public bool caseDeDepart = false;
	public List<GameObject> tokens = new List<GameObject>();

    public List<Token> tokens_ { get
        {
            List<Token> result = new List<Token>();
            foreach (var token in GameObject.FindGameObjectsWithTag("Token"))
                if (token.GetComponent<Token>().caseActuelle == gameObject)
                    result.Add(token.GetComponent<Token>());
            return result;
        }
    }
    public List<CharacterBehavior> characters { get
        {
            List<CharacterBehavior> result = new List<CharacterBehavior>();
            foreach (var token in tokens_)
                if (token.GetComponent<CharacterBehavior>() != null)
                    result.Add(token.GetComponent<CharacterBehavior>());
            return result;
        }
    }
    public List<Item> items { get
        {
            List<Item> result = new List<Item>();
            foreach (var token in tokens_)
                if (token.GetComponent<Item>() != null)
                    result.Add(token.GetComponent<Item>());
            return result;
        }
    }

    public CharacterBehavior getNonWoundedCharacter()
    {
        foreach (var character in characters)
            if (!character.wounded)
                return character;
        return null;
    }

    public CharacterBehavior getMainCharacter()
    {
        var character = getNonWoundedCharacter();
        if (character != null) return character;
        else if (characters.Count > 0) return characters[0];
        else return null;
    }

    public CharacterBehavior getOtherMainCharacter(CharacterBehavior currentCharacter)
    {
        CharacterBehavior woundedCharacter = null;
        foreach (var character in characters)
            if (character != currentCharacter)
            {
                if (!character.wounded)
                    return character;
                else
                    woundedCharacter = character;
            }
        return woundedCharacter;
    }

    // Optimisation IA
    bool isPartOfARoom = false;
    TileBehavior parentTile = null;

	GameManager gManager;

    public int row { get { return gManager.rowMatrixFromIndex(index); } }
    public int column { get { return gManager.columnMatrixFromIndex(index); } }

    void Start()
    {
        gManager = GameManager.gManager;
        isPartOfARoom = (transform.parent.GetComponent<TileBehavior>() != null);
        if (isPartOfARoom) parentTile = transform.parent.GetComponent<TileBehavior>();
		// Création de la cible de déplacement associée à la case
		GameObject temp = (GameObject) Instantiate(gManager.glowPrefab, new Vector3(transform.position.x, transform.position.y, 0), gManager.glowPrefab.transform.rotation);
		CibleDeplacement cible = temp.GetComponent<CibleDeplacement>();
		cible.caseAssociated = gameObject;
		cibleAssociated = temp;
		temp.transform.SetParent(transform);
	}

	// Remet à jour la postion des tokens sur la case (notamment après une rotation)
	void refreshTokenPositions ()
    {
        foreach (Token token in tokens_)
            token.GetComponent<TokenIHM>().refreshPosition();
    }

	// Renvoie TRUE si le chemin est libre dans la direction donnée, FALSE sinon
	public bool cheminDegage (cheminCaseAdjacente direction) {
		if (direction == cheminCaseAdjacente.vide) return true;
		else if (direction == cheminCaseAdjacente.herse) {
			try {
				return herse.GetComponent<HerseBehavior>().herseOuverte;
			}
			catch (UnassignedReferenceException) {
				Debug.LogError("La herse de la tile "+transform.parent.name+" n'a pas été initialisée");
				return false;
			}
		}
		else return false;
	}

	public bool fosseNonBloquante (bool testOnlyForSelectedCharacter = true) {
		// Si la case est une fosse
		if (type == typeCase.caseFosse) {
			// If the selected character is carrying a rope
			if (testOnlyForSelectedCharacter && gManager.actionCharacter != null) {
				CharacterBehavior currentChar = gManager.actionCharacter.GetComponent<CharacterBehavior>();
				if (currentChar.tokenTranporte != null) {
					if (currentChar.tokenTranporte.GetComponent<Item_Corde>() != null) return true;
				}
			}
			// If there is a rope or a non wounded thief on the pit
            foreach (Token token in tokens_) {
				if (token.GetComponent<CB_Voleuse>() != null && !token.GetComponent<CB_Voleuse>().wounded) return true;
				if (token.GetComponent<Item_Corde>() != null) return true;
			}
			return false;
		}
		else return true;
	}

	// Renvoie TRUE si un personnage peut déposer un token sur la case, FALSE sinon
	public bool canDropToken () {
		// Si la case est une fosse
		if (type == typeCase.caseFosse) {
			// Si un personnage est sélectionné et qu'il transporte une corde, alors on peut déposer un token sur la case
			if (gManager.actionCharacter != null) {
				CharacterBehavior currentChar = gManager.actionCharacter.GetComponent<CharacterBehavior>();
				if (currentChar.tokenTranporte != null) {
					if (currentChar.tokenTranporte.GetComponent<Item_Corde>() != null) return true;
				}
			}
            // S'il y a une Voleuse ou une Corde sur la fosse, alors on peut déposer un token sur la case
            foreach (Token token in tokens_) {
				if (token.GetComponent<Item_Corde>() != null && token.tokenHolder == null) return true;
				if (token.GetComponent<CB_Voleuse>() != null && token.gameObject != gManager.actionCharacter) return true;
			}
			return false;
		}
		else return true;
	}

	// Renvoie TRUE si la salle de la case a été ouverte, ou si aucune salle n'est associée à la case
	public bool isCaseRevealed () {
		bool result = false;
        if (isPartOfARoom) result = !parentTile.hidden;
		else {
			//Debug.Log("CaseBehavior isCaseRevealed: NoParent "+gameObject.ToString());
			result = true;
		}
		return result;
	}

	// Renvoie TRUE si un personnage est présent sur la case (différent du personnage sélectionné), FALSE sinon
	public bool isOtherCharacterPresent (GameObject characterSelected)
    {
        foreach (CharacterBehavior character in characters)
            if (character.gameObject != characterSelected)
                return true;
		return false;
	}
	
	public bool isOtherNonWoundedCharacterPresent (GameObject characterSelected)
    {
        foreach (CharacterBehavior character in characters)
            if (character.gameObject != characterSelected && !character.wounded)
                return true;
		return false;
    }

    public bool isOtherAllyPresent(CharacterBehavior character)
    {
        foreach (CharacterBehavior otherCharacter in characters)
            if (otherCharacter != character && otherCharacter.affiliationJoueur == character.affiliationJoueur)
                return true;
        return false;
    }

    // Renvoie TRUE si un allié est présent sur la case, FALSE sinon
    public bool isAllyPresent(GameObject characterSelected)
    {
        GameObject currentPlayer = characterSelected.GetComponent<CharacterBehavior>().affiliationJoueur;
        foreach (CharacterBehavior character in characters)
            if (character.affiliationJoueur == currentPlayer)
                return true;
		return false;
	}

	// Renvoie TRUE si un ennemi est présent sur la case, FALSE sinon
	public bool isOpponentPresent (GameObject characterSelected)
    {
        GameObject currentPlayer = characterSelected.GetComponent<CharacterBehavior>().affiliationJoueur;
        foreach (CharacterBehavior character in characters)
            if (character.affiliationJoueur != currentPlayer)
                return true;
        return false;
    }

	// Renvoie TRUE s'il n'y a aucun ennemi blessé sur la case, FALSE sinon
	public bool noWoundedEnemyPresent (GameObject characterSelected)
    {
        GameObject currentPlayer = characterSelected.GetComponent<CharacterBehavior>().affiliationJoueur;
        foreach (CharacterBehavior character in characters)
            if (character.affiliationJoueur != currentPlayer && character.wounded)
                return false;
		return true;
    }
    
    // Renvoie TRUE s'il y a un ennemi blessé sur la case, FALSE sinon
    public bool isNonWoundedEnemyPresent(GameObject characterSelected)
    {
        GameObject currentPlayer = characterSelected.GetComponent<CharacterBehavior>().affiliationJoueur;
        foreach (CharacterBehavior character in characters)
            if (character.affiliationJoueur != currentPlayer && !character.wounded)
                return true;
        return false;
    }

    // Renvoie TRUE si un ennemi valide (pour un combat) est présent sur la case, FALSE sinon
    public bool enemyFighterPresent (GameObject characterSelected) {
		GameObject currentPlayer = characterSelected.GetComponent<CharacterBehavior>().affiliationJoueur;
        foreach (CharacterBehavior character in characters)
			if (character.affiliationJoueur != currentPlayer && !character.freshlyWounded && !character.freshlyHealed)
				return true;
		return false;
	}

	// Renvoie TRUE si une corde est présente sur la case, FALSE sinon
	public bool isRopePresent ()
    {
        foreach (Item item in items)
            if (item.GetComponent<Item_Corde>() != null)
                return true;
		return false;
	}

	// Renvoie TRUE s'il un personnage blessé sur cette case et aucun autre personnage, FALSE sinon
	public bool woundedCharacterReadyForHealing ()
    {
		bool woundedCharacterPresent = false;
		bool noOtherCharacterPresent = true;
        foreach (CharacterBehavior character in characters)
        {
            if (character.wounded) woundedCharacterPresent = true;
			else noOtherCharacterPresent = false;
		}
		return (woundedCharacterPresent && noOtherCharacterPresent);
    }

    public cheminCaseAdjacente versDirection(int dir)
    {
        //int prevdir = dir;
        dir = mod4(dir);
        // Si la case fait partie d'une salle, on prend on compte la rotation
        int tileR = (isPartOfARoom) ? parentTile.tileRotation : 0;
        if (tileR < 0 || tileR > 3) Debug.LogError("CaseBehavior.vers(" + dir + ") : Valeur de tileRotation non conforme (<0 ou >3)");
        //Debug.Log("dir: " + prevdir + " mod4: " + dir + " tileR: " + tileR);
        return vers(dir + tileR);
    }

    public cheminCaseAdjacente versLeHaut() { return versDirection(0); }
    public cheminCaseAdjacente versLaGauche() { return versDirection(1); }
    public cheminCaseAdjacente versLeBas() { return versDirection(2); }
    public cheminCaseAdjacente versLaDroite() { return versDirection(3); }

    public CaseBehavior getCaseVers(int dir)
    {
        int i, j;
        switch (mod4(dir))
        {
            default:
            case 0: // HAUT
                i = row - 1;
                j = column;
                break;
            case 1: // GAUCHE
                i = row;
                j = column - 1;
                break;
            case 2: // BAS
                i = row + 1;
                j = column;
                break;
            case 3: // DROITE
                i = row;
                j = column + 1;
                break;
        }
        return gManager.getCase(i, j).GetComponent<CaseBehavior>();
    }

    public bool debordement(int dir)
    {
        switch (mod4(dir))
        {
            default:
            case 0: // HAUT
                return row <= 0;
            case 1: // GAUCHE
                return column <= 0;
            case 2: // BAS
                return row >= gManager.hauteurPlateau() - 1;
            case 3: // DROITE
                return column >= gManager.longueurPlateau() - 1;
        }
    }

    public static int opposee(int dir)
    {
        return mod4(dir + 2);
    }

	public int getIndex() {
		return index;
	}

	public void setIndex(int newIndex) {
		index = newIndex;
	}

    public bool isAvailable()
    {
        return (type != typeCase.caseFosse);
    }
}
