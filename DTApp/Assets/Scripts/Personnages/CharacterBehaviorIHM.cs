using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

public class CharacterBehaviorIHM : TokenIHM, IPointerDownHandler, IPointerUpHandler {
	
	protected CharacterBehavior associatedCharacter;
	protected GameObject woundedInfo;
    [HideInInspector]
    public Sprite fullCharacterSprite;
    public Sprite[] fullCharacterSpriteCollection;

    [HideInInspector]
	bool alreadySelected = false;
	//bool tokenPickedUp = false;

	public AudioClip deathSound;
    public AudioClip[] selectedSound;
    public AudioClip attackSound;
    public AudioClip[] footstepsSound;
    public AudioClip abilitySound;

    public GameObject actionPointFX;
    List<GameObject> bonusActionPointsFX = new List<GameObject>();
	
	protected float xPosHUD = Screen.width*0.86f;

    float doubleTouchTimer = 0;
    const float DOUBLE_CLICK_MAX_INTERVAL = 0.3f;

    int moveIndex = 0;

    void Awake () {
		initialization();
	}

	public override void initialization () {
		base.initialization();
		associatedCharacter = GetComponent<CharacterBehavior>();
		if (associatedCharacter == null) {
			Debug.LogError("CharacterBehaviorIHM, Awake: Le script CharacterBehavior n'a pas été trouvé sur le même Game Object");
			this.enabled = false;
        }
        woundedInfo = transform.Find("Wounded_Char_Info").gameObject;
	}

	// Use this for initialization
	public override void Start () {
		base.Start();
        woundedInfo.SetActive(false);
        fullCharacterSprite = fullCharacterSpriteCollection[gManager.app.data.getCharactersFullSprites(gManager.app.gameToLaunch.spritesChosen[associatedCharacter.indexToken])];
	}
	
	public bool canDisplayCharacterGUI () {
		return (gManager.canDisplayTokenGUI() && associatedCharacter.gameObject == gManager.actionCharacter && !gManager.deplacementEnCours);
	}
	
	public bool canDisplayCharacterRotationGUI () {
		return (gManager.canDisplayRotationGUI() && associatedCharacter.gameObject == gManager.actionCharacter);
	}

	// DEPRECATED Gère la rotation de salle au moyen de boutons (temporaire)
	public virtual void manageRotationGUI () {
        Debug.Assert(false);
	}
	
	// Update is called once per frame
    void Update()
    {
        if (!gManager.freezeDisplay)
        {
            manageWoundsDisplay();
            // Gérer l'apparence des tokens et la mise en place du plateau
            placementDesTokens();
            manageCharacterDisplay();
            if (associatedCharacter.isRemovedFromGame())
            {
                GetComponent<Collider>().enabled = false;
            }

            // Add action point particules if needed
            for (int i = bonusActionPointsFX.Count; i < associatedCharacter.actionPoints; i++)
            {
                GameObject temp = (GameObject)Instantiate(actionPointFX, transform.position, Quaternion.identity);
                bonusActionPointsFX.Add(temp);
                ParticleSystem[] particles = temp.GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem p in particles)
                {
                    p.GetComponent<Renderer>().sortingLayerName = "HUD";
                    Color newColor = associatedCharacter.affiliationJoueur.GetComponent<PlayerBehavior>().playerColor;
                    p.startColor = new Color(newColor.r, newColor.g, newColor.b, p.startColor.a);
                }
            }
            // Remove action point particules if needed
            while (bonusActionPointsFX.Count > associatedCharacter.actionPoints)
            {
                GameObject fx = bonusActionPointsFX[bonusActionPointsFX.Count - 1];
                bonusActionPointsFX.Remove(fx);
                Destroy(fx);
            }
            // Update action point particules animation
            Vector3 currentCharacterPosition = transform.position;
            for (int i = 0; i < bonusActionPointsFX.Count; i++)
            {
                bonusActionPointsFX[i].transform.rotation = Quaternion.identity;
                bonusActionPointsFX[i].transform.Rotate(Vector3.forward, (float)i * (360.0f / (float)bonusActionPointsFX.Count) + (-40.0f * Time.time));
                bonusActionPointsFX[i].transform.position = currentCharacterPosition + (bonusActionPointsFX[i].transform.right * 0.8f);
            }
        }
	}
	
	// Gestion de l'affichage de la tete du personnage et de ses valeurs de mouvement et de combat
    public void manageWoundsDisplay()
    {
        tokenInfo.SetActive(!associatedCharacter.wounded);
        woundedInfo.SetActive(associatedCharacter.wounded);
	}
	
	// Gère l'apparence du token : son placement vis à vis du reste du plateau et l'activation ou non de son halo
	public void manageCharacterDisplay () {
		if (associatedCharacter.isTokenOnBoard()) {
			// Si le personnage peut etre sélectionné ou est en cours de sélection, on affiche son halo
			if ((!gManager.selectionEnCours || associatedCharacter.selected) && gManager.isActivePlayer(associatedCharacter.affiliationJoueur) && associatedCharacter.totalCurrentActionPoints() > 0 && !associatedCharacter.wounded) highlightToken(true);
			else highlightToken(false);
		}
	}

    public void characterSelection()
    {
		// Désélection du personnage
        if (gManager.actionCharacter == gameObject)
        {
            changeSortingLayer("TokensOnBoard");
            gManager.actionWheel.resetButtonsActions();
        }
        else
        {
            changeSortingLayer("Selection");
            gManager.actionPointCost = 1;
        }
        associatedCharacter.selectionPersonnage();

        characterSelectionDisplay();
    }

    public void characterSelectionOnLoad()
    {
        changeSortingLayer("Selection");
        gManager.actionPointCost = 1;

        moveTargets.Add(GetComponent<Token>().caseActuelle.GetComponent<CaseBehavior>().cibleAssociated.gameObject);

        associatedCharacter.characterSelectionOnLoad();

        characterSelectionDisplay();
    }

    private void characterSelectionDisplay()
    {
        if (associatedCharacter.selected) StartSelectAnimation();
        else StartUnselectAnimation();

        Transform tokenInfo = gManager.exchangeUI.transform.Find("Token Info");
        tokenInfo.Find("Sprite").GetComponent<Image>().sprite = fullCharacterSprite;
        tokenInfo.Find("Movement").GetComponent<Text>().text = associatedCharacter.MOVE_VALUE.ToString();
        tokenInfo.Find("Combat").GetComponent<Text>().text = associatedCharacter.COMBAT_VALUE.ToString();
        string encodedString = gManager.app.data.rulesFile.text;
        JSONNode jsonData = JSONNode.Parse(encodedString);
        tokenInfo.Find("Name").GetComponent<Text>().text = ((string)jsonData["BaseGame"][associatedCharacter.getTokenName()]["Name"][gManager.app.gameLanguage.ToString()]).ToUpper();
    }

    public void deplacementCancelled()
    {
        StartUnselectAnimation();
    }
	
	// Renvoit le sprite de l'image du token (item ou tete du personnage)
	public override GameObject getTokenIcon () {
		GameObject info;
		if (!associatedCharacter.wounded) info = tokenInfo;
		else info = woundedInfo;
		for (int i=0 ; i < info.transform.childCount ; i++) {
			Transform child = info.transform.GetChild(i);
			if (!child.name.Contains("Combat") && !child.name.Contains("Movement"))
				return info.transform.GetChild(i).gameObject;
		}
		Debug.LogError("Character Behavior IHM, getTokenIcon: Aucun game object correspondant trouvé");
		return null;
	}

    // Réaction à un clic / touch sur le personnage
    public void OnPointerDown(PointerEventData e)
    {
        if (validInput()) OnInputDown();
    }

	public override void OnInputDown() {
		selectionTimer = Time.time;
        if (!associatedCharacter.tokenPlace) base.OnInputDown();
        if (gManager.gameMacroState == GameProgression.Playing)
        {
            if (!gManager.selectionEnCours)
            {
                // On bloque le déplacement de la caméra
                //gManager.cameraMovementLocked = true;
                alreadySelected = true;
                gManager.playSound(selectedSound[UnityEngine.Random.Range(0, selectedSound.Length)]);
                characterSelection();
            }
            else if (gManager.isInformationAndExchangeUIOpen()) gManager.closeInformationAndExchangeUI();
        }
	}

    // Quand on relache le clic sur le token
    public void OnPointerUp(PointerEventData e)
    {
        if (validInput()) OnInputUp();
    }

	public override void OnInputUp() {
        base.OnInputUp();
        if (gManager.gameMacroState == GameProgression.Playing)
        {
            if (Time.time - doubleTouchTimer < DOUBLE_CLICK_MAX_INTERVAL)
            {
                // Combat si click sur un ennemi atteignable
                if (!associatedCharacter.isRemovedFromGame() && associatedCharacter.caseActuelle.GetComponent<CaseBehavior>().cibleAssociated.GetComponent<CibleDeplacementIHM>().adjacentFighter &&
                    gManager.actionCharacter.GetComponent<Token>().affiliationJoueur != associatedCharacter.affiliationJoueur
                    && !gManager.usingSpecialAbility)
                    gManager.combatManager.combat(gameObject);
            }
            else
            {
                if (Time.time - selectionTimer < SIMPLE_CLICK_DURATION)
                {
                    if (!alreadySelected)
                    {
                        // Encore utilisée ?
                        if (moveTargets.Count > 0)
                        {
                            if (associatedCharacter.canStayOnCell(moveTargets[moveTargets.Count - 1].GetComponent<CibleDeplacement>().caseAssociated.GetComponent<CaseBehavior>()))
                            {
                                GameObject target = moveTargets[moveTargets.Count - 1];
                                target.GetComponent<CibleDeplacement>().nbDeplacementRestant = 0;
                                associatedCharacter.deplacementConfirmed(target);
                                endDeplacementIHM();
                            }
                        }
                        else characterSelection();
                    }
                    doubleTouchTimer = Time.time;
                }
                // Dans le cas d'un appui long
                else if (moveTargets.Count > 0)
                {
                    pickUpOverflownToken();
                    recomputePossibleActionsIHM(true, true);
                }
            }
            alreadySelected = false;
        }
	}

    public void updateCurrentCell()
    {
        if (moveTargets.Count > 0)
        {
            GameObject targetCell = moveTargets[moveTargets.Count - 1].GetComponent<CibleDeplacement>().caseAssociated;
            if (targetCell != associatedToken.caseActuelle)
            {
                Debug.LogWarning("update current cell");
                recordPreviousState();
                associatedToken.caseActuelle = targetCell;
                CharacterBehavior character = associatedToken.GetComponent<CharacterBehavior>();
                if (character != null && character.tokenTranporte != null)
                {
                    TokenIHM token = character.tokenTranporte.GetComponent<TokenIHM>();
                    token.recordPreviousState();
                    token.GetComponent<Token>().caseActuelle = targetCell;
                }
            }
        }
    }

	public void pickUpOverflownToken () {
		//Debug.Log("pickUpOverflownToken");
        if (gManager.forceExchangeUI || moveTargets.Count > 0)
        {
            updateCurrentCell();
            tokenExchange();
		}
		else Debug.LogWarning("CharacterBehaviorIHM, pickUpOverflownToken: Aucune cible de déplacement repertoriée par le personnage actif");
	}

	public void openCharacterExchangeUI () {
		if (associatedCharacter.MAX_TOKENS_CARRY > 0) tokenExchange();
		else Debug.LogWarning("Character Behavoir IHM, openCharacterExchangeUI: Le personnage ne peut tenir de tokens");
	}

    void tokenExchange()
    {
        // Si le joueur a appuyé sur le bouton pour ouvrir le panneau d'échange
        if (gManager.forceExchangeUI)
        {
            manualTokenExchange();
        }
        else
        {
            bool automaticPick = associatedCharacter.tryAutomaticPick(); // try to autopick a token on the current cell
            
            if (!automaticPick && associatedCharacter.getPickableTokens().Count > 0) // if autopick failed and there are pickable tokens, open the exchange ui
            {
                manualTokenExchange();
            }
        }
    }

    private GameObject getCarriedSpot() { return gManager.exchangeUI.transform.Find("Carried Token Spot").gameObject; }
    private GameObject getSingleGroundSpot() { return gManager.exchangeUI.transform.Find("Ground Token Spot Single").gameObject; }
    private GameObject getMultipleGroundSpot(int index) { return gManager.exchangeUI.transform.Find("Ground Token Spot " + (index + 1)).gameObject; }

    private void displayGroundSpot(int spotCount)
    {
        bool onlyOneGroundSpotNeeded = spotCount < 2;
        getSingleGroundSpot().GetComponent<ExchangePointUI>().displayExchangePoint(onlyOneGroundSpotNeeded);
        for (int i=0; i<2; ++i) getMultipleGroundSpot(i).GetComponent<ExchangePointUI>().displayExchangePoint(!onlyOneGroundSpotNeeded);
    }

    private void instantiateSpotToken(CaseBehavior cell, GameObject spot, Token token)
    {
        if (token != null) instantiateExchangeableTokens(cell, token.gameObject, spot);
        else gManager.exchangePoints.Add(spot);
    }

    void manualTokenExchange()
    {
        CaseBehavior currentCell = associatedCharacter.caseActuelle.GetComponent<CaseBehavior>();

        gManager.exchangeUI.GetComponent<InfoCharacterPanel>().panelToOpenPosition();

        // On ground there can be :
        // 1) nothing. 1 slot on ground needed.
        // 2) 1 item. 1 slot needed if I carry an item, 2 slots if I carry a wounded ally
        // 3) 1 wounded ally. 1 slot needed if I carry a wounded ally, 2 slots if I carry an item.
        // 4) a non wounded ally or an enemy. 2 slots needed (the right one containing the non pickable character)
        // 5) 2 tokens. 2 slots needed.
        int nbSpotOnGround = 0;
        Token[] tokenOnGround = new Token[2];
        foreach(Token token in currentCell.tokens_)
        {
            if (token.gameObject != gameObject && token.tokenHolder != associatedCharacter) // not the moving character or its carried token
            {
                Debug.Assert(nbSpotOnGround < 2, "Too many tokens on ground");
                tokenOnGround[nbSpotOnGround++] = token;
            }
        }

        // if only one token check if another spot is needed
        if (nbSpotOnGround == 1)
        {
            CharacterBehavior chara = tokenOnGround[0].GetComponent<CharacterBehavior>();
            if (chara != null && (chara.affiliationJoueur != associatedToken.affiliationJoueur || !chara.wounded)) // case 4: non pickable character on ground
            {
                ++nbSpotOnGround; // another spot needed
            }
            else if (associatedCharacter.carriedToken != null)
            {
                bool carriedTokenIsCharacter = associatedCharacter.carriedToken.GetComponent<CharacterBehavior>() != null;
                bool groundTokenIsCharacter = chara != null;
                if (carriedTokenIsCharacter != groundTokenIsCharacter) // case 2 and 3: an item and a wounded character can be on the ground at the same time
                {
                    ++nbSpotOnGround; // another spot needed
                }
            }
        }

        displayGroundSpot(nbSpotOnGround);

        instantiateSpotToken(currentCell, getCarriedSpot(), associatedCharacter.carriedToken);

        if (nbSpotOnGround <= 1)
            instantiateSpotToken(currentCell, getSingleGroundSpot(), tokenOnGround[0]);
        else
            for (int i = 0; i < nbSpotOnGround; ++i) instantiateSpotToken(currentCell, getMultipleGroundSpot(i), tokenOnGround[i]);
    }

	void instantiateExchangeableTokens (CaseBehavior overflownCase, GameObject token, GameObject point) {
		GameObject temp, tempIcon;
		Quaternion rotation = gManager.tempTokenExchangePrefab.transform.rotation;
        bool interactableToken = true;

		temp = (GameObject) Instantiate(gManager.tempTokenExchangePrefab, point.transform.position, rotation);
        temp.SetActive(true);
        temp.GetComponent<Image>().sprite = token.GetComponent<SpriteRenderer>().sprite;
        temp.transform.SetParent(gManager.exchangeUI.transform);
        temp.transform.localScale = new Vector3(1, 1, 1);
		TokenExchangeUI exchangeUI = temp.GetComponent<TokenExchangeUI>();
		exchangeUI.enabled = true;
		exchangeUI.token = token;
		exchangeUI.originalPoint = point;
		exchangeUI.currentCase = overflownCase;

        tempIcon = (GameObject)Instantiate(gManager.tempTokenExchangePrefab, point.transform.position, rotation);
        tempIcon.SetActive(true);
        tempIcon.GetComponent<Button>().enabled = false;
        tempIcon.GetComponent<TokenExchangeUI>().enabled = false;
        tempIcon.transform.SetParent(temp.transform);
        tempIcon.transform.localScale = new Vector3(1, 1, 1);
        tempIcon.GetComponent<Image>().sprite = token.GetComponent<TokenIHM>().getTokenIcon().GetComponent<SpriteRenderer>().sprite;
		tempIcon.transform.Find("Token Highlight").gameObject.SetActive(false);

        if (overflownCase.type == CaseBehavior.typeCase.caseFosse && token.GetComponent<Item_Corde>() == null) interactableToken = false;
        else
        {
            CharacterBehavior chara = token.GetComponent<CharacterBehavior>();
            if (chara != null)
            {
                if (chara.affiliationJoueur != associatedToken.affiliationJoueur || !chara.wounded) interactableToken = false;
            }
        }
        // Si le token est porté par un personnage
        if (token.GetComponent<Token>().tokenHolder != null)
        {
            Token t = token.GetComponent<Token>();
            bool holdingWoundedAlly = false;
            if (associatedCharacter.tokenTranporte != null)
            {
                if (associatedCharacter.tokenTranporte.GetComponent<CharacterBehavior>() != null)
                {
                    holdingWoundedAlly = associatedCharacter.tokenTranporte.GetComponent<CharacterBehavior>().wounded;
                }
            }
            // Si on survole un adversaire non blessé, on ne peut pas lui prendre son token
            // OU
            // Si le token est une corde au sol au dessus d'une fosse, on ne peut pas le récupérer (cela tuerait le personnage qui la tient)
            // OU
            // Si le personnage allié qui tient le token est blessé et que le personnage actif porte déjà un blessé, il ne peut qu'échanger les blessés
            if ((!gManager.isActivePlayer(t.tokenHolder.affiliationJoueur) && !t.tokenHolder.wounded)
                || (!point.name.Contains("Carried") && token.GetComponent<Item_Corde>() != null && overflownCase.type == CaseBehavior.typeCase.caseFosse)
                || (gManager.isActivePlayer(t.tokenHolder.affiliationJoueur) && holdingWoundedAlly && t.tokenHolder.wounded))
            {
                interactableToken = false;
            }
        }

        if (!interactableToken)
        {
            temp.GetComponent<Button>().enabled = false;
            temp.GetComponent<TokenExchangeUI>().enabled = false;
            temp.transform.Find("Token Highlight").gameObject.SetActive(false);
        }
        point.GetComponent<ExchangePointUI>().occupyingToken = temp;
        gManager.exchangePoints.Add(point);
    }

	// A chaque fin de tour
	public void changementTourJoueur () {
		// Si le personnage venait d'etre soigné, on retire le marqueur qui indiquait cet état
        if (!associatedCharacter.wounded)
        {
            SpriteRenderer tokenIcon = getTokenIcon().GetComponent<SpriteRenderer>();
            tokenIcon.color = new Color(1, 1, 1, tokenIcon.color.a);
        }
	}
	
	// Le personnage ramasse un token
	public void ramasserToken (GameObject token, bool record = true) {
		// Afficher l'icone du token sur le personnage et faire disparaitre le token
		TokenIHM t = token.GetComponent<TokenIHM>();
        if (record) t.recordPreviousState();
		t.hideToken();
        t.fadeInTokenIcon();
		
		associatedCharacter.ramasserToken(token);
        t.transform.localPosition = new Vector3(1.0f, 1.0f, -1.0f);

        token.GetComponent<Collider>().enabled = false;
	}
	
	// Le personnage dépose un token
	public void deposerToken (bool record = true) {
		// Retirer l'icone du token sur le personnage, puis faire réapparaitre le token
		TokenIHM t = associatedCharacter.tokenTranporte.GetComponent<TokenIHM>();
        if (record) t.recordPreviousState();
        t.getTokenIcon().GetComponent<SpriteRenderer>().enabled = false;
        t.transform.position = new Vector3(transform.position.x, transform.position.y, 0);

        associatedCharacter.tokenTranporte.GetComponent<Collider>().enabled = true;
        associatedCharacter.tokenTranporte.GetComponent<TokenIHM>().changeSortingLayer("TokensOnBoard");

        associatedCharacter.deposerToken();

        t.applyNewScale(t.NORMAL_SCALE_VALUE);
		t.displayToken();
	}

	// Le personnage est soigné
	public void characterHealedIHM() {
		woundedInfo.SetActive(false);
		tokenInfo.SetActive(true);
        getTokenIcon().GetComponent<SpriteRenderer>().color = Color.green;
		associatedCharacter.characterHealed();
	}

    void playKillAnimation()
    {
        gManager.playSound(deathSound);
        // On désactive le clic sur le personnage et on le fait disparaitre
        GetComponent<Collider>().enabled = false;
        fadeOutToken();
    }

    // kill character and display its total victory point value
    public void killCharacterIHM(bool displayVP = true)
    {
        Token carriedToken = associatedCharacter.killCharacter();

        int VPEarned = associatedCharacter.KILL_VP_VALUE;

        playKillAnimation();

        if (carriedToken != null && carriedToken is Item)
        {
            carriedToken.GetComponent<ItemIHM>().destroyItem();
        }
        else if(carriedToken != null && carriedToken is CharacterBehavior)
        {
            VPEarned += carriedToken.GetComponent<CharacterBehavior>().KILL_VP_VALUE;
            carriedToken.GetComponent<CharacterBehaviorIHM>().killCharacterIHM();
        }

        if (displayVP) displayVictoryPointsFeedback(VPEarned);
	}

    public void boardEntry()
    {
        hideToken();
        StartCoroutine(boardEntryCoroutine());
    }

    // Le personnage est téléporté sur le plateau
    IEnumerator boardEntryCoroutine()
    {
        GameObject fx = (GameObject)Instantiate(gManager.teleportFXPrefab, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(fx.GetComponent<TeleportCharacterAnimation>().waitDuringFull);
        fadeInToken();
    }
	
	IEnumerator sortiePlateau (float wait, int VPEarned) {
		GetComponent<Collider>().enabled = false;
		
        GameObject fx = (GameObject)Instantiate(gManager.teleportFXPrefab, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(fx.GetComponent<TeleportCharacterAnimation>().waitDuringFull);

        GameObject tokenTransporte = associatedCharacter.tokenTranporte;
        if (tokenTransporte != null)
        { // deposerToken hack
            tokenTransporte.GetComponent<Token>().tokenHolder = null;
            associatedCharacter.tokenTranporte = null;
        }

        fadeOutToken();
        if (tokenTransporte != null) tokenTransporte.GetComponent<TokenIHM>().fadeOutToken();

        displayVictoryPointsFeedback(VPEarned);

        yield return new WaitForSeconds(wait);
		hideToken();
        if (tokenTransporte != null) tokenTransporte.GetComponent<TokenIHM>().hideToken();
    }

    // Faire apparaitre un +X indiquant le nombre X de points de victoire gagnés
    void displayVictoryPointsFeedback(int victoryPoints)
    {
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(transform.position);
        Vector2 screenPosition = new Vector2(screenPoint.x / Screen.width, screenPoint.y / Screen.height);
        GameObject textFeedback = (GameObject)Instantiate(gManager.victoryPointsTextPrefab, Vector3.zero, gManager.victoryPointsTextPrefab.transform.rotation);
        textFeedback.transform.SetParent(GameObject.Find("GlobalUILayout").transform);
        textFeedback.transform.SetAsLastSibling();
        textFeedback.transform.localScale = new Vector3(1, 1, 1);
        textFeedback.GetComponent<FadingText>().displayVPEarnedFeedback(victoryPoints);

        screenPosition = Camera.main.WorldToViewportPoint(transform.position);
        RectTransform rTransform = textFeedback.GetComponent<RectTransform>();
        rTransform.anchorMin = rTransform.anchorMax = screenPosition;
        rTransform.offsetMin = rTransform.offsetMax = Vector2.zero;
    }

    // Faire apparaitre un -X indiquant le nombre X de points d'action dépensés
    void displayActionPointsUsedFeedback(int actionPointsUsed)
    {
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(new Vector3(transform.position.x +5f, transform.position.y +10, transform.position.z));
        Vector2 screenPosition = new Vector2(screenPoint.x / Screen.width, screenPoint.y / Screen.height);
        GameObject textFeedback = (GameObject)Instantiate(gManager.victoryPointsTextPrefab, Vector3.zero, gManager.victoryPointsTextPrefab.transform.rotation);
        textFeedback.transform.SetParent(GameObject.Find("GlobalUILayout").transform);
        textFeedback.transform.SetAsLastSibling();
        textFeedback.transform.localScale = new Vector3(1, 1, 1);
        textFeedback.GetComponent<FadingText>().displayActionPointsUsedFeedback(actionPointsUsed);

        screenPosition = Camera.main.WorldToViewportPoint(new Vector3(transform.position.x + 0.25f, transform.position.y + 0.5f, transform.position.z));
        RectTransform rTransform = textFeedback.GetComponent<RectTransform>();
        rTransform.anchorMin = rTransform.anchorMax = screenPosition;
        rTransform.offsetMin = rTransform.offsetMax = Vector2.zero;
    }

    // end movement for game engine and then IHM
    public void endDeplacementIHM()
    {
        int boardExitVP = associatedCharacter.endDeplacement();
        endDeplacementIHMOnly(boardExitVP);
    }

    // end deplacement only for IHM
    public void endDeplacementIHMOnly(int boardExitVP = 0) {
        transform.position = new Vector3(transform.position.x, transform.position.y, 0);
		changeSortingLayer("TokensOnBoard");
        gManager.displayCancelButton = false;
		cancelMovementPossible = false;
        if (gManager.app.gameToLaunch.isTutorial) displayActionPointsUsedFeedback(1);
		//tokenPickedUp = false;
		if (moveTargets.Count > 0) moveTargets.Clear();

        if (!associatedCharacter.isRemovedFromGame())
            Debug.Assert(associatedCharacter.endCellIsSafe(associatedCharacter.caseActuelle.GetComponent<CaseBehavior>()), "Invalid cell (suicide not allowed)");

        StartUnselectAnimation();

		// Si le personage est sorti du plateau
		if (associatedCharacter.isRemovedFromGame())
			StartCoroutine(sortiePlateau(gManager.VICTORY_ANIM_DURATION, boardExitVP));
		
		actionDone();
    }

    public void recomputePossibleActionsIHM(bool onlyIfNeeded, bool clearTargets)
    {
        gManager.resetPartialMoveFlags();

        if (GetComponent<TokenIHM>().moveTargets.Count > 0)
        {
            CibleDeplacement target = moveTargets[moveTargets.Count - 1].GetComponent<CibleDeplacement>();

            if (!onlyIfNeeded || target.nbDeplacementRestant > 0)
            {
                if (clearTargets)
                {
                    moveTargets.Clear();
                    moveTargets.Add(target.gameObject);
                    associatedCharacter.deplacementRestant = target.nbDeplacementRestant;
                }

                associatedCharacter.computePossibleActions(target.caseAssociated.GetComponent<CaseBehavior>());
            }
        }
        else
        {
            Debug.Assert(!clearTargets, "There should be at least one target remaining if clearTargets is required");

            associatedCharacter.computePossibleActions(); // current cell
        }
    }

    public void glideToNextPosition()
    {
        if (moveIndex < moveTargets.Count)
        {
            float moveDuration = 0.15f;
            iTween.EaseType movementType = iTween.EaseType.linear;
            if (moveIndex + 1 < moveTargets.Count)
            {
                if (moveTargets[moveIndex + 1].GetComponent<CibleDeplacementIHM>().jumpingTarget)
                {
                    // Debug.Log("jump");
                    moveDuration *= 2;
                    iTween.ScaleTo(gameObject, iTween.Hash("scale", notPlacedScale, "time", moveDuration, "easetype", iTween.EaseType.easeOutQuart, "oncomplete", "restoreScaleAfterJump"));
                }
            }
            else if (moveTargets[moveIndex].GetComponent<CibleDeplacementIHM>().jumpingTarget) moveDuration *= 2;
            if (moveTargets[moveIndex].GetComponent<CibleDeplacementIHM>().wallwalkTarget)
            {
                moveDuration *= 2;
                movementType = iTween.EaseType.easeInOutSine;
            }
            iTween.MoveTo(gameObject, iTween.Hash("position", moveTargets[moveIndex].transform.position, "time", moveDuration, "easetype", movementType, "oncomplete", "glideToNextPosition"));
            moveIndex++;
        }
        // Une fois le token arrivé sur la case de destination
        else
        {
            pickUpOverflownToken();

            if (associatedToken.selected) transform.position = new Vector3(transform.position.x, transform.position.y, -1);
            else transform.position = new Vector3(transform.position.x, transform.position.y, 0);

            if (gManager.activePlayer.isScriptedPlayer())
            {
                waitBeforeEndingMove();
            }
            else if (gManager.onlineGameInterface.isOnlineOpponent(gManager.activePlayer.index) && moveTargets[moveIndex - 1].GetComponent<CibleDeplacementIHM>().jumpingTarget)
            {
                waitBeforeEndingMove();
            }
            else
            {
                moveIndex = 0;
                gliding = false;
                Debug.Assert(gManager.actionCharacter.GetComponent<CharacterBehaviorIHM>() == this, "the character gliding should be actionCharacter");
                recomputePossibleActionsIHM(false, true);
                gManager.onlineGameInterface.EndReplayAction();
            }
        }
    }

    public void waitBeforeEndingMove() { Debug.Assert(moveTargets.Count > 0); StartCoroutine(waitBeforeEndingMove(moveTargets[moveTargets.Count - 1], 0.3f)); }

    public IEnumerator waitBeforeEndingMove(GameObject cible, float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        moveIndex = 0;
        gliding = false;
        cible.GetComponent<CibleDeplacement>().nbDeplacementRestant = 0;
        transform.position = new Vector3(cible.transform.position.x, cible.transform.position.y, 0); // Update character position => necessary ?
        associatedCharacter.deplacementConfirmed(cible);
        endDeplacementIHM();
        gManager.onlineGameInterface.EndReplayAction();
    }

    public void actionDone () {
        Debug.LogWarning("CharacterIHM: Action Done");
		moveTargets.Clear();
        gManager.actionWheel.resetButtonsActions();
    }

    public void clearTokensPreviousSate()
    {
        foreach (Token token in gManager.GetAllTokens())
            token.GetComponent<TokenIHM>().clearPreviousState();
    }

    public void restoreTokensPreviousState()
    {
        foreach (Token token in gManager.GetAllTokens())
            token.GetComponent<TokenIHM>().restorePreviousState();
    }

    public override void cancelMovement () {
        gManager.onlineGameInterface.RecordCancelMove();

        restoreTokensPreviousState();

		if (gManager.isInformationAndExchangeUIOpen()) gManager.closeInformationAndExchangeUI();
		changeSortingLayer("TokensOnBoard");
		base.cancelMovement();
	}

    public void playCharacterFootstep()
    {
        gManager.playSound(footstepsSound[UnityEngine.Random.Range(0, footstepsSound.Length)]);
    }

}
