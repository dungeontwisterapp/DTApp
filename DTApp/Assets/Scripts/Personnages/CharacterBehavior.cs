using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CharacterBehavior : Token {

	// Token que le personnage transporte
	public GameObject tokenTranporte;
	// Token(s) que le personnage transporte
	//List<GameObject> carriedTokens = new List<GameObject>();
	// Nombre de tokens maximum que le personnage peut transporter
	public int MAX_TOKENS_CARRY = 1;
	// Valeur par défaut de déplacement
	public int MOVE_VALUE = 1;
	// Valeur par défaut de combat
	public int COMBAT_VALUE = 1;
	// Points de victoire donné si le personnage se fait tuer
	public int KILL_VP_VALUE = 1;
	// Points d'action du personnage
	public int pointsAction = 0; // use actionPoints wrapper instead
	public bool wounded = false;
	// TRUE si le personnage vient d'etre soigné, FALSE sinon
	public bool freshlyHealed = false;
	// TRUE si le personnage vient d'etre blessé, FALSE sinon
	public bool freshlyWounded = false;
	// TRUE si le personnage est un jeteur de sorts, FALSE sinon
	public bool jeteurDeSorts = false;

	// Si le personnage est tué
	public bool killed = false;

    public PathFinding pathfinder = null;

    public int actionPoints { get { return pointsAction; } set { /*Debug.LogWarning("Change " + getTokenName() + " action Points: " + pointsAction + " => " + value);*/ pointsAction = value; } }
    public Token carriedToken { get { return tokenTranporte != null ? tokenTranporte.GetComponent<Token>() : null; } }

    // Use this for initialization
    public override void Start () {
		base.Start();
	}

	void Update () {
	}

    public virtual bool isTrapped()
    {
        return isTokenOnBoard() && tokenHolder == null &&
            caseActuelle.GetComponent<CaseBehavior>().type == CaseBehavior.typeCase.caseFosse &&
            !caseActuelle.GetComponent<CaseBehavior>().fosseNonBloquante(false);
    }

	public void manageTrapsConsequences () {
		if (isTrapped()) GetComponent<CharacterBehaviorIHM>().killCharacterIHM();
    }

    public void characterSelectionOnLoad()
    {
        selected = true;
        computePossibleActions();
    }

    // Réaction à un clic / appui sur le personnage
    public virtual void selectionPersonnage () {
		// Si le personnage est sur le plateau
		if (tokenPlace && !gManager.combatEnCours && !gManager.rotationEnCours) {
            Debug.Assert(isTokenOnBoard(), "It should not be possible to click on a removed token");
			// Si c'est au tour du joueur qui controle le personnage ET que le personnage est déjà sélectionné, ou qu'aucune sélection n'est en cours
			if ((!gManager.selectionEnCours || selected) && gManager.isActivePlayer(affiliationJoueur)) {
				// Si le joueur dispose d'au moins un point d'action pour le personnage 
				if (totalCurrentActionPoints() > 0) {
					// Si le personnage n'était pas sélectionné
                    if (!selected)
                    {
                        gManager.actionCharacter = gameObject;
                        selected = true;
                        gManager.selectionEnCours = true;
                        deplacementRestant = MOVE_VALUE;

                        computePossibleActions();

                        automaticTokenPickUp();
                    }
                    // Si le joueur clique sur le même personnage à nouveau, il est désélectionné
                    else
                    {
                        automaticTokenPickUp();
                        deselectCharacter();
                    }
				}
			}
			// 
			else {
				// Si aucune capacité spéciale n'est en cours d'utilisation
				if (!gManager.usingSpecialAbility) {
					if (!gManager.isActivePlayer(affiliationJoueur)) {
						if (!gManager.adversaireAdjacent) gManager.actionCharacter = gameObject;
					}
					// Si un autre personnage du même joueur était sélectionné, mais qu'aucun déplacement n'avait encore été effectué
					else if (!gManager.deplacementEnCours) {
						// Si un autre personnage était bel et bien sélectionné
						if (gManager.actionCharacter != null) {
							// On annule la sélection
							gManager.actionCharacter.GetComponent<CharacterBehavior>().selected = false;
							gManager.selectionEnCours = false;
                            gManager.actionCharacter.SendMessage("deplacementCancelled");
							// Et on sélectionne ce personnage
							selectionPersonnage();
						}
						else Debug.LogWarning("Character Behavior, selectionPersonnage: Tentative de sélection d'un autre personnage échoue car aucun personnage sélectionné");
					}
				}
				// Sinon, assigner ce personnage comme la cible de la capacité
				else gManager.targetCharacter = gameObject;
			}
		}
	}

    void automaticTokenPickUp()
    {
        // WARNING: This method can only be called on a character selection if the character has not moved !
        //          During move, use tryAutomaticPick instead

        // Si un item seul est sur sa case, le récupérer
        if (tokenTranporte == null && caseActuelle.GetComponent<CaseBehavior>().tokens_.Count == 2)
            foreach (Token token in caseActuelle.GetComponent<CaseBehavior>().tokens_)
                if (token.gameObject != gameObject)
                {
                    Debug.Assert(
                        token.GetComponent<CharacterBehavior>() == null ||
                        (token.GetComponent<CharacterBehavior>().wounded && token.GetComponent<CharacterBehavior>().affiliationJoueur == affiliationJoueur),
                        "Cannot pick a non wounded or ennemy character");
                    GetComponent<CharacterBehaviorIHM>().ramasserToken(token.gameObject, false);
                    break;
                }
    }

	public void deselectCharacter () {
		selected = false;
		gManager.selectionEnCours = false;
		gManager.deplacementEnCours = false;
		gManager.actionPointCost = 0;
		deplacementCancelled();
	}

	// A chaque fin de tour, on réinitialise certaines valeurs
	public void changementTourJoueur () {
		freshlyHealed = false;
		freshlyWounded = false;
	}

	// Le personnage ramasse un token
	public void ramasserToken (GameObject token) {
		tokenTranporte = token;
        tokenTranporte.GetComponent<Token>().tokenHolder = this;
        //if (gameObject == gManager.actionCharacter) tokenTranporte.GetComponent<Token>().caseActuelle.GetComponent<CaseBehavior>().tokens.Remove(tokenTranporte);

		tokenTranporte.transform.parent = transform;
	}
	
	// Le personnage dépose un token sur la case actuelle
    public void deposerToken()
    {
        deposerToken(tokenTranporte.GetComponent<Token>().caseActuelle.GetComponent<CaseBehavior>());
    }
	
	// Le personnage dépose un token une case donnée
    public void deposerToken(CaseBehavior ground)
    {
		if (tokenTranporte.GetComponent<CharacterBehavior>() != null) tokenTranporte.transform.parent = GameObject.Find("Personnages").transform;
		else tokenTranporte.transform.parent = GameObject.Find("Items").transform;

        //ground.tokens.Add(tokenTranporte);
		tokenTranporte.GetComponent<Token>().tokenHolder = null;

		tokenTranporte = null;
	}

	// Le personnage deient blessé
	public void characterWounded() {
        Debug.Assert(!wounded, "Character is already wounded!");
		freshlyWounded = true;
		wounded = true;
    }

    // Le personnage est soigné
    public void characterHealed()
    {
        wounded = false;
        freshlyHealed = true;
    }

    // kill the character and return transported token if it needs to be destroyed
    public Token killCharacter()
    {
        Debug.Assert(!killed, "Character already killed");
        Token token = null;
        if (tokenTranporte != null)
        {
            token = tokenTranporte.GetComponent<Token>();
            GetComponent<CharacterBehaviorIHM>().deposerToken(false);

            if (token.GetComponent<CharacterBehavior>() != null)
            {
                // kill carried wounded character
            }
            else if (caseActuelle.GetComponent<CaseBehavior>().type == CaseBehavior.typeCase.caseFosse && token.GetComponent<Item_Corde>() == null)
            {
                // destroy carried item (fall on pit)
            }
            else
            {
                // do not destroy the item
                token = null;
            }
        }
        caseActuelle = null;
        killed = true;
        ownerOpponent.victoryPoints += KILL_VP_VALUE;
        return token;
    }

	public override int sortiePlateau () {
        int extiBoardVP = 0;
        actionPoints = 0;
        if (tokenTranporte != null)
        {
            extiBoardVP += tokenTranporte.GetComponent<Token>().sortiePlateau();
        }
        if (!wounded)
        {
            affiliationJoueur.GetComponent<PlayerBehavior>().victoryPoints += ESCAPE_VP_VALUE;
            extiBoardVP += ESCAPE_VP_VALUE;
        }
        caseActuelle = null;
        horsJeu = true;
        return extiBoardVP;
    }

	// Renvoie la somme du nombre de points d'action du joueur et du nombre de points d'action sur ce personnage
	public int totalCurrentActionPoints() {
		return (actionPoints + gManager.actionPoints);
    }

    public override bool isRemovedFromGame()
    {
        return horsJeu || killed;
    }

    #region movement
    // Compute and activate all possible actions
    // by default (null) caseDepart will be character's current cell
    public void computePossibleActions(CaseBehavior caseDepart = null)
    {
        if (caseDepart == null) caseDepart = caseActuelle.GetComponent<CaseBehavior>();

        int row = caseDepart.row;
        int column = caseDepart.column;

        pathfinder = new PathFinding(gManager, row, column, this);
        
        if (!surLaRegletteAdverse(caseDepart)) // the movement must stop once on the opponent zone.
        {
            caseDepart.selectionnePourDeplacement = true;

            // Changer le SortingLayer +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

            pathfinder.SearchPossibleActions();

            activatePossibleActions();

            caseDepart.cibleAssociated.GetComponent<Collider>().enabled = false;
        }
        
	}

    public void activatePossibleActions()
    {
        foreach (ActionType type in System.Enum.GetValues(typeof(ActionType)))
        {
            foreach (CaseBehavior cell in pathfinder.GetActivatedCells(type))
            {
                int priority = pathfinder.GetActionPriority(type, cell);
                switch (type)
                {
                    case ActionType.ATTACK:
                        gManager.adversaireAdjacent = true;
                        cell.cibleAssociated.GetComponent<CibleDeplacementIHM>().adjacentFighter = true;
                        break;

                    case ActionType.HEAL:
                        {
                            CharacterBehavior character = cell.getMainCharacter();
                            Debug.Assert(character != null);
                            GetComponent<CB_Clerc>().personnagesSoignables.Add(character.gameObject);
                        }
                        break;

                    case ActionType.OPENDOOR:
                        break;
                    case ActionType.CLOSEDOOR:
                        break;
                    case ActionType.DESTROYDOOR:
                        break;
                    case ActionType.REGENERATE:
                        break;

                    case ActionType.JUMP:
                        if (!pathfinder.HasAction(ActionType.WALK, cell) && priority == 2) // cell reachable by jump, but not by walk
                        {
                            int remaining = 0;
                            cell.selectionnePourDeplacement = true;
                            if (!gManager.freezeDisplay) moveTargetActivation(remaining, cell, true);
                        }
                        break;
                    case ActionType.WALK:
                        {
                            int remaining = deplacementRestant - priority;
                            bool validMove = true; // starting cell always valid: why ? -> maybe for green highlighting ?
                            if (priority != 0) // not starting cell
                            {
                                cell.selectionnePourDeplacement = true;

                                if (remaining > 0) // movement remaining
                                {
                                    if (gManager.onlineGameInterface.isOnlineOpponent(gManager.activePlayer.index))
                                        validMove = canCrossCell(cell);
                                    else
                                        validMove = canStopOnCell(cell);
                                }
                                else
                                {
                                    validMove = canStayOnCell(cell);
                                }
                            }
                            if (validMove && !gManager.freezeDisplay) moveTargetActivation(remaining, cell, false);
                        }
                        break;
                    case ActionType.WALLWALK:
                        if (!pathfinder.HasAction(ActionType.WALK, cell)) // cell reachable by wallwalk, but not by walk
                        {
                            int remaining = 0;
                            cell.cibleAssociated.GetComponent<CibleDeplacementIHM>().wallwalkTarget = true;
                            if (!gManager.freezeDisplay) moveTargetActivation(remaining, cell, false);
                        }
                        break;

                    case ActionType.REVEAL:
                        cell.transform.parent.GetComponent<TileBehavior>().canShow = true;
                        break;
                    case ActionType.ROTATE:
                        gManager.rotationPossible = true;
                        cell.transform.parent.GetComponent<TileBehavior>().rotationPossible();
                        break;

                    case ActionType.FIREBALL:
                        // TODO
                        break;
                    case ActionType.SPEEDPOTION:
                        // TODO
                        break;
                }
            }
        }
    }

    public bool MoveToTarget(ActionType type, CaseBehavior target)
    {
        List<CaseBehavior> path = pathfinder.GetActionPath(type, target);

        if (path.Count > 0)
        {
            gManager.onlineGameInterface.RecordMove(type, this, target);

            CharacterBehaviorIHM characterIHM = GetComponent<CharacterBehaviorIHM>();
            if (!characterIHM.gliding)
            {
                characterIHM.gliding = true;
                characterIHM.moveTargets.Clear();
                foreach (CaseBehavior cell in path)
                {
                    characterIHM.moveTargets.Add(cell.cibleAssociated);
                    cell.cibleAssociated.GetComponent<SpriteRenderer>().color = Color.green;
                    if (cell.affiliationJoueur != null && cell.affiliationJoueur != affiliationJoueur) break;
                }
                characterIHM.glideToNextPosition();
            }
            return true;
        }

        return false;
    }

    public virtual bool canCrossCell(CaseBehavior currentCase)
    {
        return currentCase.fosseNonBloquante() && !currentCase.isNonWoundedEnemyPresent(gameObject);
    }

    public virtual bool canStopOnCell(CaseBehavior currentCase)
    {
        return currentCase.fosseNonBloquante() && !currentCase.isNonWoundedEnemyPresent(gameObject);
    }

    public virtual bool canStayOnCell(CaseBehavior currentCase)
    {
		if (surLaRegletteAdverse(currentCase))
        {
            return !currentCase.isOtherNonWoundedCharacterPresent(gameObject);
        }
        else
        {
            return (!currentCase.isOtherNonWoundedCharacterPresent(gameObject) && !currentCase.isOpponentPresent(gameObject) && !tooManyTokensToStayOnCell(currentCase) && currentCase.fosseNonBloquante());
        }
    }

    public virtual bool endCellIsSafe(CaseBehavior currentCase)
    {
        return currentCase.fosseNonBloquante();
    }

    public bool tooManyTokensToStayOnCell(CaseBehavior currentCase)
    {
        int nbTokens = currentCase.tokens_.Count;
        if (caseActuelle != currentCase.gameObject)
        {
            nbTokens++;
            if (tokenTranporte != null) nbTokens++;
        }
        return nbTokens >= 3;
    }

    // Déplacement vers une case
    public void deplacementConfirmed (GameObject cible)
    {
        if (gManager.deplacementEnCours)
        {
            // DONE: RecordStartMove is automatically called before a wallwalk, a jump or a move
            // TODO: RecordPickToken/RecordDropToken/RecordExchangeToken is automatically called on RecordStartMove and RecordEndMove
            // DONE: for walk, wallwalk or jump, call RecordMove at the start with the ending cell and the corresponding action
            // DONE: at the end if action is cancelled call RecordCancelMove
            // DONE:            if action is confirmed call RecordEndMove
            gManager.onlineGameInterface.RecordEndMove();
        }

        //int casesRestantes = cible.GetComponent<CibleDeplacement>().nbDeplacementRestant;
        CaseBehavior ancienneCase = caseActuelle.GetComponent<CaseBehavior>();

        var characterIHM = GetComponent<CharacterBehaviorIHM>();
        if (characterIHM.isInteractedWith) // new method
        {
            ancienneCase = characterIHM.previousCase.GetComponent<CaseBehavior>();
        }
        characterIHM.clearTokensPreviousSate();

        if (!gManager.casesAtteignables.Contains(ancienneCase.cibleAssociated)) gManager.casesAtteignables.Add(ancienneCase.cibleAssociated);

        caseActuelle = cible.GetComponent<CibleDeplacement>().caseAssociated;
        if (tokenTranporte != null) tokenTranporte.GetComponent<Token>().caseActuelle = caseActuelle;
        
        Debug.Assert(endCellIsSafe(caseActuelle.GetComponent<CaseBehavior>()), "Invalid cell (suicide not allowed)");

        // Automatic pick if possible
        tryAutomaticPick();
	}

    public List<Token> getAutoPickableTokens()
    {
        CaseBehavior currentCell = caseActuelle.GetComponent<CaseBehavior>();
        if (currentCell.isOtherNonWoundedCharacterPresent(gameObject)) return new List<Token>();
        else return getPickableTokens();
    }

    public List<Token> getPickableTokens()
    {
        CaseBehavior currentCell = caseActuelle.GetComponent<CaseBehavior>();
        if (currentCell.isNonWoundedEnemyPresent(gameObject)) return new List<Token>(); // cannot pick an object when a non wounded enemy is present

        List<Token> result = new List<Token>();
        foreach (Token token in currentCell.tokens_)
        {
            if (token.GetComponent<CharacterBehavior>() != null)
            {
                CharacterBehavior otherCharacter = token.GetComponent<CharacterBehavior>();
                if (otherCharacter != this && otherCharacter.affiliationJoueur == affiliationJoueur && otherCharacter.wounded && otherCharacter.tokenHolder != this) result.Add(token); // can autopick wounded ally that the character doesn't already carry
            }
            else // item
            {
                // TODO: Fix the case where an ally thief is standing on the pit
                if (token.GetComponent<Item_Corde>() != null && currentCell.type == CaseBehavior.typeCase.caseFosse && currentCell.isOtherAllyPresent(this))
                    continue; // cannot pick a rope on a pit where an ally is standing

                if (token.tokenHolder != this)
                    result.Add(token); // can pick an item that the character doesn't already carry
            }
        }
        return result;
    }

    // Pendant un déplacement, vérifie s'il y a des tokens à ramasser, s'il y en a un seul on le ramasse automatiquement
    public bool tryAutomaticPick() {

        if (tokenTranporte != null) return false; // autopick only if not already carrying a token

        List<Token> autoPickableTokens = getAutoPickableTokens();

		// If there is only one pickable token, pick it
		if (autoPickableTokens.Count == 1) {
            if (autoPickableTokens[0].tokenHolder != null) autoPickableTokens[0].tokenHolder.GetComponent<CharacterBehaviorIHM>().deposerToken();
			GetComponent<CharacterBehaviorIHM>().ramasserToken(autoPickableTokens[0].gameObject);
			return true;
		}

		return false;
	}

	// Fin de l'action de déplacement
	public int endDeplacement() {
        int exitBoardVP = 0;
		gManager.deplacementEnCours = false;
		// Si le personnage a affectué un saut, retirer un saut à son joueur
		if (caseActuelle.GetComponent<CaseBehavior>().cibleAssociated.GetComponent<CibleDeplacementIHM>().jumpingTarget) affiliationJoueur.GetComponent<PlayerBehavior>().nbSauts--;

         // Si le retrait des cibles de déplacement n'a pas encore été effectué, le faire
        if (gManager.casesAtteignables != null) clearDeplacementHUD();
        
        Debug.Assert(endCellIsSafe(caseActuelle.GetComponent<CaseBehavior>()), "Invalid cell (suicide not allowed)");

		deplacementRestant = MOVE_VALUE;
		selected = false;
		if (checkSortiePlateau()) exitBoardVP = sortiePlateau();

        foreach(CharacterBehavior character in gManager.GetAllCharacters())
            character.manageTrapsConsequences();

		// Terminer l'action
		actionDone();
		return exitBoardVP;
	}

	public bool checkSortiePlateau () {
		// Si la case est affiliée à un joueur
		if (caseActuelle.GetComponent<CaseBehavior>().affiliationJoueur != null) {
			// Si elle est affiliée au joueur adverse, le personnage sort du plateau
			if (affiliationJoueur != caseActuelle.GetComponent<CaseBehavior>().affiliationJoueur) return true;
		}
		return false;
	}
	
	// Remettre le système à l'état initial et retirer un point d'action du joueur actuel
	public override void actionDone () {
		base.actionDone();
		// Remettre à zéro les variables indiquant les actions possibles
		clearUnresolvedActions();
		gManager.actionCharacter = null;
	}
	
	// Annule le déplacement en cours
	public override void deplacementCancelled () {
		clearDeplacementHUD();
		clearUnresolvedActions();
		gManager.actionCharacter = null;
	}
	
	// Remettre à zéro les variables indiquant les actions possibles
    public virtual void clearUnresolvedActions()
    {
        gManager.actionWheel.resetButtonsActions();
        gManager.tilePourRotation = null;
		gManager.rotationPossible = false;
		gManager.sautPossible = false;
		gManager.adversaireAdjacent = false;
		gManager.targetCharacter = null;

		if (tokenTranporte != null) {
			if (tokenTranporte.GetComponent<Item>() != null) {
				tokenTranporte.GetComponent<Item>().clearUnresolvedActions();
			}
		}

		GameObject[] tiles = GameObject.FindGameObjectsWithTag("Tile");
		for (int i=0 ; i < tiles.GetLength(0) ; i++) {
			tiles[i].GetComponent<TileBehavior>().canShow = false;
			tiles[i].GetComponent<TileBehavior>().canRotate = false;
		}
		//gManager.actionCharacter = null;
	}

    #endregion
    #region combat

    public bool hasCombatItem()
    {
        if (tokenTranporte != null)
        {
            if (tokenTranporte.GetComponent<Item>() != null) return tokenTranporte.GetComponent<Item>().combatItem;
        }
        return false;
    }
    public List<CharacterBehavior> getFighters(CharacterBehavior ennemy) { return pathfinder.SearchFighters(this, ennemy); }

    #endregion

}
