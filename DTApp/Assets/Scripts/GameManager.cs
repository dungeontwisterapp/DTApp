using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.SceneManagement;

public enum GameProgression { GameStart, TokenPlacement, Playing, GameOver };

public enum ActionType { WALK, ATTACK, JUMP, WALLWALK, HEAL, DESTROYDOOR, OPENDOOR, CLOSEDOOR, ROTATE, FIREBALL, SPEEDPOTION, REVEAL, REGENERATE, // regular actions
                        COMBAT_CARD, ACTION_CARD // card choices
};

public class GameManager : MonoBehaviour {

    public static GameManager gManager;
    [HideInInspector]
    public static new AudioSource audio;

    [HideInInspector]
    public int boardLength = 10;
    public int NOMBRE_TILES = 4;
    public float VICTORY_ANIM_DURATION = 2.0f;
    [HideInInspector]
    public int ACTION_BUTTON_UP = 1;
    [HideInInspector]
    public int ACTION_BUTTON_LEFT = 2;
    [HideInInspector]
    public int ACTION_BUTTON_RIGHT = 3;
    public static float COMBAT_ANIMATION_SPEED = 20.0f;
    public int VICTORY_POINTS_LIMIT = 5;

    List<GameObject> tilesPrefabs = new List<GameObject>();
    List<GameObject> zoneDepartPrefabs = new List<GameObject>();

    [Header("Prefabs")]
    public GameObject[] tilesBacks;
    public GameObject glowPrefab;

    public GameObject tempTokenExchangePrefab;
    public GameObject teleportFXPrefab;
    public GameObject victoryFXPrefab;
    public GameObject victoryPointsTextPrefab;
    public GameObject jumpsRemainingPrefab;
    public GameObject wallwalkIconPrefab;
    public GameObject sfxPrefab;
    public GameObject fighterPrefab;
    public GameObject rotationArrowFeedback;
    private GameObject[][] caseMatrix;

    [Header("Jeu")]
    public GameObject[] players;
    public GameObject[] combatCards;
    public GameObject[] cartesCombat;
    public int pointsAction = 0; // use wrapper actionPoints instead
    public int valeurMaxCarteAction = 2;
    public int actionPointCost = 0;
    // DurÃƒÂ©e d'un tour en secondes
    public float TURN_DURATION = 120.0f;
    // DurÃƒÂ©e d'une action courte (choisir une carte combat, placer des pions dans une salle révélée) en secondes
    public float QUICK_ACTION_DURATION = 10.0f;

    [Header("Booléens")]
    public bool selectionEnCours = false;
    public bool deplacementEnCours = false;
    public bool rotationPossible = false;
    public bool sautPossible = false;
    public bool usingSpecialAbility = false;
    public bool adversaireAdjacent = false;
    public bool rotationEnCours = false;
    public bool placerTokens = false;
    public bool otherPlayerTurnToPlace = false;
    public bool startTurn = false;
    public bool turnStarted = false;
    public bool timedGame = true;
    public bool displayCancelButton = false;
    public bool forceExchangeUI = false;
    public bool cameraMovementLocked = false;
    [HideInInspector]
    public bool longTouch = false;
    public bool updatedRulesAvailable = false;
    public bool freezeDisplay = false;
    bool debugMode = false;

    public GameObject ciblePlacementToken;
    public GameObject jumpIcon;

    [Header("Boutons")]
    public Sprite rotationActionSprite;
    public Texture2D boutonSoin;
    public Texture2D boutonDeposer;
    public Texture2D boutonRotation;

    [HideInInspector]
    public AppManager app;

    [Header("Variables & references")]
    public Image sceneTransitionFade;
    public Image pickActionCardSign;
    public CombatUI combatDisplay;
    [HideInInspector]
    public List<GameObject> casesAtteignables = new List<GameObject>();
    public GameObject tilePourRotation;
    public GameObject informationUI;
    public GameObject infoUIButton;
    public GameObject exchangeUI;
    public List<GameObject> exchangePoints = new List<GameObject>();

    public GameObject actionCharacter;
    public GameObject targetCharacter;
    [HideInInspector]
    public GameObject playerSwitchHUD;
    public Text leftTimer;
    public Text rightTimer;
    public GameObject actionCards;
    public GameObject cancelButton;
    public GameObject validationButton;
    public GameObject hideTokensButton;
    public GameObject actionCardsButton;
    public GameObject actionButtons;
    public AudioClip[] tokenPlaceSound;
    public GameObject messageUI;

    public GameProgression gameMacroState = GameProgression.GameStart;

    public bool debugGameState = false;
    public GameState saveState;

    private GameObject joueurActif;

    bool gameReady = false;

    float stdButtonHeight = Screen.height / 6;
    float stdButtonWidth = Screen.height / 6;

    Color transparentBlack = new Color(0, 0, 0, 0);

    public bool selectCharacterOnLoad = false; // hack for allowing selection on load

    public GameProgressionManager progression;
    public CombatManager combatManager;

    // fin de definition des variables

    public Multi.Interface onlineGameInterface { get { return app.onlineGameInterface; } }
    public bool onlineGame { get { return onlineGameInterface.isOnline; } }
    public bool scriptedGame { get { return players[1].GetComponent<PlayerBehavior>().isScriptedPlayer(); } }

    public bool combatEnCours { get { return combatManager.fightOnGoing; } }

    public ActionWheel actionWheel { get { Debug.Assert(actionButtons != null && actionButtons.GetComponent<ActionWheel>() != null); return actionButtons.GetComponent<ActionWheel>(); } }

    public Token GetTokenByNameAndOwnerIndex(string tokenName, int ownerIndex)
    {
        foreach (GameObject g in GameObject.FindGameObjectsWithTag("Token"))
        {
            Token token = g.GetComponent<Token>();
            if (token.getTokenName() == tokenName && token.indexToken == ownerIndex) return token;
        }
        return null;
    }

    public bool SelectCharacterOnLoadIfNeeded()
    {
        if (selectCharacterOnLoad) // hack to allow selecting character on load from bga
        {
            actionCharacter.GetComponent<CharacterBehaviorIHM>().characterSelectionOnLoad();
            if (combatEnCours)
            {
                combatManager.loadFight();
            }
            else if (deplacementEnCours && !onlineGameInterface.isOnlineOpponent(activePlayer.index))
            {
                displayCancelButton = true;
            }
            selectCharacterOnLoad = false;
            return true;
        }
        return false;
    }

    void Update()
    {
        onlineGameInterface.Update();

        // Recharger le jeu
        if (Input.GetKey(KeyCode.Escape))
        {
            GameObject.Find("Options menu").GetComponent<SmallMenuAnimation>().openMenu();
        }

        if (!freezeDisplay)
        {
            //infoUIButton.SetActive(actionCharacter != null);
            if (actionCharacter != null && !combatEnCours && !rotationEnCours && playerInteractionAvailable()) infoUIButton.transform.parent.SendMessage("panelToDisplayPosition");
            else infoUIButton.transform.parent.SendMessage("panelToHiddenPosition");
        }

        switch (gameMacroState)
        {
            case GameProgression.GameStart:
                Debug.Assert(!app.gameToLaunch.loadExistingGame);
                // In online mode, we need to handle here the ending of multiple active player state CharacterChoice
                if (onlineGame && otherPlayerTurnToPlace && progression.IsCharacterPlacementDone())
                {
                    if (activePlayer.index != progression.GetFirstPlayerToPlaceInSecondPart())
                        switchPlayer();
                    else
                        startPlayerTurn();

                    initiateTokenPlacement();

                    if (onlineGame)
                    {
                        progression.EndProcess(); // NW: reenable notifications
                        onlineGameInterface.EnableAction(); // deprecated
                    }
                }
                break;

            case GameProgression.TokenPlacement:
                Debug.Assert(!app.gameToLaunch.loadExistingGame);
                // When each players have place all of their tokens, start game
                if (progression.IsTokenPlacementDone())
                {
                    if (activePlayer.index != progression.GetFirstPlayerToPlay())
                        switchPlayer();
                    else
                        startPlayerTurn();

                    gameMacroState = GameProgression.Playing;
                    //hideTokensButton.SetActive(true);
                    activePlayer.startGame();

                    cameraMovementLocked = false;
                    selectionEnCours = true;

                    if (onlineGame)
                    {
                        //progression.EndProcess(); // NW: 
                        onlineGameInterface.EnableAction(); // deprecated
                    }
                }
                break;

            case GameProgression.Playing:
                if (progression.IsGameOver()) // If high enough score obtained, end the game
                {
                    gameMacroState = GameProgression.GameOver;
                    validationButton.SendMessage("hideButton");
                    // La première fois que le jeu est lancé
                    if (app.gameToLaunch.isTutorial)
                    {
                        if (players[0].GetComponent<PlayerBehavior>().victoryPoints >= VICTORY_POINTS_LIMIT) StartCoroutine(endMission(app.firstLaunchTutorialComplete));
                        else StartCoroutine(tutorialMissionFailed());
                    }
                    else
                    {
                        app.deleteSavedGame();
                        StartCoroutine(endGame());
                    }
                }
                else
                {
                    if (combatManager.combatCardsReadyToReveal)
                    {
                        StartCoroutine(combatManager.resolveCombat(1.8f));
                    }

                    // Changer de joueur quand le nombre de points d'action est de 0
                    if (!hasActionsLeft && !selectionEnCours && turnStarted)
                    {
                        bool noActionCardsLeft = true;
                        Transform actionCardsContainer = GameObject.Find("GlobalUILayout").transform.Find("Action Cards").transform;
                        for (int i = 0; i < 4; i++)
                        {
                            if (actionCardsContainer.GetChild(i).gameObject.activeSelf)
                            {
                                noActionCardsLeft = noActionCardsLeft && activePlayer.usedActionCards[i];
                            }
                        }

                        if (app.gameToLaunch.isTutorial && noActionCardsLeft)
                        {
                            selectionEnCours = true;
                            StartCoroutine(tutorialMissionFailed());
                        }
                        else
                        {
                            if (boardHoldsActiveEnemyCharacters())
                            {
                                if (!onlineGame && !app.gameToLaunch.isTutorial)
                                {
                                    validationButton.SetActive(true);
                                    if (validationButton.GetComponent<ValidationButton>().validated)
                                    {
                                        validationButton.SendMessage("hideButton");
                                        changementTourJoueur(true);
                                    }
                                }
                                else changementTourJoueur(true);
                            }
                            else
                            {
                                //startTurn = true;
                                turnStarted = false;
                                selectionEnCours = true;
                                GameObject[] tokens = GameObject.FindGameObjectsWithTag("Token");
                                for (int i = 0; i < tokens.GetLength(0); i++)
                                {
                                    if (tokens[i].GetComponent<CharacterBehavior>() != null) tokens[i].SendMessage("changementTourJoueur");
                                }
                            }
                        }
                    }
                }
                break;

            case GameProgression.GameOver:
                // nothing to do
                break;
        }

        // TODO: Tourner toute la GUI dans le sens tenu par le joueur
    }


    public void changementTourJoueur(bool displayFeedback)
    {
        GameObject charactersContainer = GameObject.Find("Personnages");
        if (displayFeedback) charactersContainer.BroadcastMessage("changementTourJoueur");
        else
        {
            int childCount = charactersContainer.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                if (charactersContainer.transform.GetChild(i).gameObject.activeInHierarchy) charactersContainer.transform.GetChild(i).SendMessage("changementTourJoueur");
            }
        }
        switchPlayerTurn(displayFeedback);
        if (timedGame) activePlayer.tempsRestant = TURN_DURATION;
        selectionEnCours = true;
        turnStarted = false;
    }

    // Tourner les pions dans le sens de l'appareil
    public void straightenTokens() {
        GameObject[] tokens = GameObject.FindGameObjectsWithTag("Token");
        for (int i = 0; i < tokens.GetLength(0); i++) {
            tokens[i].transform.rotation = Quaternion.Euler(Vector3.up);
            if (tokens[i].GetComponent<CharacterBehavior>() != null)
            {
                if (tokens[i].GetComponent<CharacterBehavior>().tokenTranporte != null)
                {
                    tokens[i].GetComponent<CharacterBehavior>().tokenTranporte.transform.position = new Vector3(tokens[i].transform.position.x + 0.5f, tokens[i].transform.position.y + 0.5f, 0);
                }
            }
        }
    }

    void OnGUI() {

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (debugGameState)
        {
            if (GUI.Button(new Rect(Screen.width * 0.03f, Screen.height * 0.05f, stdButtonWidth, stdButtonHeight), boutonSoin))
            {
                saveState.saveGameState();
            }
            if (GUI.Button(new Rect(Screen.width * 0.03f, Screen.height * 0.07f + stdButtonHeight, stdButtonWidth, stdButtonHeight), boutonRotation))
            {
                if (saveState.isGameSaved()) saveState.loadGameState();
            }
            if (GUI.Button(new Rect(Screen.width * 0.03f, Screen.height * 0.09f + 2 * stdButtonHeight, stdButtonWidth, stdButtonHeight), boutonDeposer))
            {
                app.deleteSavedGame();
            }
        }
#endif

        // Si la partie est en asynchrone, on affiche l'heure actuelle
        if (!timedGame) {
            leftTimer.text = System.DateTime.Now.ToString("HH");
            rightTimer.text = System.DateTime.Now.ToString("mm");
        }

        switch (gameMacroState)
        {
            case GameProgression.GameStart: // CharacterChoice // Placement de 4 personnages sur la réglette de départ
                Debug.Assert(!selectionEnCours, "No selection possible during CharacterChoice");
                Debug.Assert(app.gameToLaunch.doBoardSetup, "Should not enter CharacterChoice State when playing with automatic board setup");
                {
                    cameraMovementLocked = true;
                    GameObject[] cibles = GameObject.FindGameObjectsWithTag("PlacementToken");
                    // S'il n'y a pas encore de cibles disponibles, on ne fait rien
                    if (cibles.Length > 0)
                    {
                        Debug.Assert(!onlineGame || !onlineGameInterface.isOnlineOpponent(activePlayer.index),
                            "The target should not be built for online opponents whom tokens will be placed automatically");
                        Debug.Assert(cibles.Length == 4);
                        // count how many characters of current player are placed on target. If all target are assigned, or all characters are placed we can confirm placement.
                        int characterToPlaceCount = 0;
                        int characterPlacedCount = 0;
                        foreach (GameObject token in GameObject.FindGameObjectsWithTag("Token"))
                        {
                            if (token.GetComponent<CharacterBehavior>() != null && isActivePlayer(token.GetComponent<Token>().affiliationJoueur))
                            {
                                characterToPlaceCount++;
                                if (token.GetComponent<Token>().cibleToken != null)
                                    characterPlacedCount++;
                            }
                        }
                        bool allCharactersInPlace = (characterPlacedCount == Math.Min(cibles.Length, characterToPlaceCount));

                        if (allCharactersInPlace)
                        {
                            validationButton.SetActive(true);
                            if (validationButton.GetComponent<ValidationButton>().validated)
                            {
                                validationButton.SendMessage("hideButton");
                                placeStartingCharacters();
                            }
                        }
                        else if (validationButton.activeInHierarchy) validationButton.SendMessage("hideButton");
                    }
                    if (debugMode)
                    {
                        //Bouton de DEBUG pour placer 4 personnages aléatoirement sur la réglette de départ  
                        if (GameObject.FindGameObjectsWithTag("PlacementToken").GetLength(0) > 0)
                        {
                            if (GUI.Button(new Rect(Screen.width * 0.83f, Screen.height * 0.05f, stdButtonWidth, stdButtonHeight), boutonRotation))
                            {
                                activePlayer.endStartingCharactersPlacement();
                            }
                        }
                    }
                }
                break;

            case GameProgression.TokenPlacement:
                Debug.Assert(!selectionEnCours, "No selection possible during TokenPlacement");
                Debug.Assert(app.gameToLaunch.doBoardSetup, "Should not enter TokenPlacement State when playing with automatic board setup");
                if (debugMode)
                {
                    // Fonction de DEBUG pour placer automatiquement les pions
                    if (GUI.Button(new Rect(Screen.width * 0.83f, Screen.height * 0.05f, stdButtonWidth, stdButtonHeight), boutonRotation))
                    {
                        playerSwitchHUD.SendMessage("OnMouseDown");
                        foreach (GameObject token in GameObject.FindGameObjectsWithTag("Token"))
                        {
                            Token t = token.GetComponent<Token>();
                            if (!t.tokenPlace && t.cibleToken == null)
                            {
                                putTokenOnRandomFreeTokenTarget(t);
                            }
                        }
                        StartCoroutine(debugStartGame());
                    }
                }
                break;

            case GameProgression.Playing:
                // Afficher la GUI si on sÃƒÂ©lectionne quelque chose
                if (selectionEnCours)
                {
                    // Si on n'est pas en mode combat
                    if (actionCharacter != null && !combatEnCours)
                    {
                        // Afficher la GUI pour un personnage
                        if (!rotationEnCours)
                        {
                            // S'il le personnage sÃƒÂ©lectionnÃƒÂ© n'a pas commencÃƒÂ© ÃƒÂ  se dÃƒÂ©placer
                            if (deplacementEnCours)
                            {
                                actionWheel.resetButtonsActions();
                            }
                            else
                            {
                                // Faire tourner une salle
                                if (rotationPossible)
                                {
                                    actionWheel.activateOneButtonIfNeeded(ActionType.ROTATE, rotationActionSprite, () => { initiateRotation(); });
                                }
                            }
                        }
                    }
                    // Placement des tokens sur une salle
                    else if (!combatEnCours && placerTokens)
                    {
                        if (activePlayer.isScriptedPlayer())
                        {
                            startTurn = true;
                            foreach (GameObject token in GameObject.FindGameObjectsWithTag("Token"))
                            {
                                Token t = token.GetComponent<Token>();
                                if (!t.tokenPlace && t.cibleToken == null && token.GetComponent<TokenIHM>().canMoveToken())
                                {
                                    putTokenOnRandomFreeTileTarget(token.GetComponent<TokenIHM>());
                                }
                            }
                            startTurn = false;
                            //Debug.LogError("Scripted player places tokens");
                            completeTokenPlacement(true);
                        }
                        else
                        {
                            if (tokensToPlaceCount(activePlayer) == 0) // if all tokens to place by the active player are placed on the revealed tile
                            {
                                bool validation = true;
                                if (playerInteractionAvailable())
                                {
                                    validationButton.SetActive(true);
                                    validation = validationButton.GetComponent<ValidationButton>().validated;
                                    if (validation) validationButton.SendMessage("hideButton");
                                }

                                if (validation) // if validation button is pressed, lock the tokens placed
                                {
                                    foreach (PlacementTokens target in tokensPlacedOnRevealedTile())
                                    {
                                        if (target.tokenAssociated != null && !target.locked)
                                        {
                                            target.locked = true;
                                            onlineGameInterface.RecordPlacementOnRoomDiscovered(target.tokenAssociated.GetComponent<Token>(), target);
                                        }
                                    }
                                }

                                if (tokensPlacedOnRevealedTile().Count == 0) // if all tokens to place by the active player are placed and locked
                                {
                                    if (!otherPlayerTurnToPlace)
                                    {
                                        //Debug.LogError("Phase I - All tokens of active player " + activePlayer.index + " are placed");
                                        if (tokensToPlaceCount(nonActivePlayer) > 0)
                                        {
                                            //Debug.LogError("Phase I => Phase II. Switch to player " + (1 - activePlayer.index));
                                            otherPlayerTurnToPlace = true;
                                            switchPlayer();
                                            if (timedGame) activePlayer.setShortTimer(QUICK_ACTION_DURATION);
                                        }
                                        else
                                        {
                                            //Debug.LogError("No tokens to place: skip phase II");
                                            if (gManager.onlineGame) { gManager.actionPoints--; gManager.actionPointCost = 0; } // temporary hack to adapt to bga's behavior
                                            completeTokenPlacement(false);
                                            if (timedGame) activePlayer.restoreStandardTimer();
                                        }
                                    }
                                    else
                                    {
                                        //Debug.LogError("Phase II - All tokens of active player " + activePlayer.index + " are placed");
                                        if (gManager.onlineGame) { gManager.actionPoints--; gManager.actionPointCost = 0; } // temporary hack to adapt to bga's behavior
                                        completeTokenPlacement(true);
                                        if (timedGame) activePlayer.restoreStandardTimer();
                                    }
                                }
                            }
                            else if (validationButton.activeInHierarchy) validationButton.SendMessage("hideButton");
                        }
                    }
                    // Quand on doit choisir une carte action
                    else if (startTurn)
                    {
                        if (app.gameToLaunch.isTutorial && !combatEnCours) pickActionCardSign.enabled = true;

                        if (!hasActionsLeft && !onlineGameInterface.isOnlineOpponent(activePlayer.index)) actionCardsButton.SetActive(true);
                        else actionCardsButton.SetActive(false);
                    }
                }
                break;

            case GameProgression.GameOver:
                // do nothing
                break;
        }
    }

    public void toggleDebugMode()
    {
        debugMode = !debugMode;
    }

    public int actionPoints { get { return pointsAction; } set { /*Debug.LogWarning("Change action Points: " + pointsAction + " => " + value);*/ pointsAction = value; } }

    public int getPlayerActionPoints(int playerIndex) {
        bool isActivePlayer = (activePlayer.index == playerIndex);
        bool isCurrentPlayer = (isActivePlayer != otherPlayerTurnToPlace); // if otherPlayerTurnToPlace, then the activePlayer is not the currentPlayer
        return isCurrentPlayer ? actionPoints : 0;
    }

    private bool hasBonusActionPoints { get {
            foreach (GameObject token in GameObject.FindGameObjectsWithTag("Token"))
                if (token.GetComponent<CharacterBehavior>() != null && token.GetComponent<CharacterBehavior>().actionPoints > 0)
                    return true;
            return false;
        }
    }

    private bool hasActionsLeft { get { return hasBonusActionPoints || actionPoints > 0; } }

    private int GetCurrentTotalActionPoints()
    {
        if (actionCharacter != null) return actionCharacter.GetComponent<CharacterBehavior>().totalCurrentActionPoints();
        else return actionPoints;
    }

    private int tokensToPlaceCount(PlayerBehavior player)
    {
        int count = 0;
        foreach (GameObject token in GameObject.FindGameObjectsWithTag("Token"))
            if (!token.GetComponent<Token>().tokenPlace && token.GetComponent<Token>().cibleToken == null && token.GetComponent<TokenIHM>().canMoveToken(player))
                count++;
        return count;
    }

    public List<PlacementTokens> tokensPlacedOnRevealedTile()
    {
        List<PlacementTokens> targets = new List<PlacementTokens>();
        foreach (GameObject cible in GameObject.FindGameObjectsWithTag("TileRevealedTarget"))
        {
            PlacementTokens target = cible.GetComponent<PlacementTokens>();
            if (target.tokenAssociated != null && !target.locked)
                targets.Add(target);
        }
        return targets;
    }

    private void putTokenOnRandomFreeTileTarget(TokenIHM token)
    {
        List<GameObject> freeTargets = new List<GameObject>();
        foreach (var cible in GameObject.FindGameObjectsWithTag("TileRevealedTarget")) if (cible.GetComponent<PlacementTokens>().tokenAssociated == null) freeTargets.Add(cible);
        int randomTargetIndex = UnityEngine.Random.Range(0, freeTargets.Count);

        Debug.Assert(randomTargetIndex < freeTargets.Count, "picked random index " + randomTargetIndex + " out of range " + freeTargets.Count);
        token.placeToken(freeTargets[randomTargetIndex]);
        freeTargets[randomTargetIndex].GetComponent<PlacementTokens>().locked = true;
        token.tokenPutOnBoard();
    }

    private void putTokenOnRandomFreeTokenTarget(Token token)
    {
        List<GameObject> freeTargets = new List<GameObject>();
        foreach (var cible in token.ciblesTokens) if (cible.GetComponent<PlacementTokens>().tokenAssociated == null) freeTargets.Add(cible);
        int randomTargetIndex = UnityEngine.Random.Range(0, freeTargets.Count);

        Debug.Assert(randomTargetIndex < freeTargets.Count, "picked random index " + randomTargetIndex + " out of range " + freeTargets.Count);
        PlacementTokens target = freeTargets[randomTargetIndex].GetComponent<PlacementTokens>();
        token.transform.position = target.transform.position;
        token.cibleToken = target;
        target.tokenAssociated = token.gameObject;
        target.locked = true;
    }

	// Active l'action de rotation
	public void initiateRotation () {
		if (actionCharacter != null) {
            if (Camera.main.GetComponent<ZoomControl>().isActiveAndEnabled) Camera.main.SendMessage("dezoom");
            rotationEnCours = true;
            displayCancelButton = true;
			actionCharacter.GetComponent<CharacterBehavior>().clearDeplacementHUD();
            if (multipleTilesRotationAvailable(actionCharacter)) actionCharacter.GetComponent<CharacterBehavior>().caseActuelle.transform.parent.GetComponent<TileBehaviorIHM>().chooseTileToRotate();
            else actionCharacter.GetComponent<CharacterBehavior>().caseActuelle.transform.parent.GetComponent<TileBehaviorIHM>().enableTileRotation();
		}
		else Debug.LogError("Game Manager, initiateRotation: Aucun personnage n'est sÃƒÂ©lectionnÃƒÂ©");
    }

    bool multipleTilesRotationAvailable(GameObject actionCharacter)
    {
        return (actionCharacter.GetComponent<Token>().caseActuelle.transform.parent.GetComponent<TileBehavior>().canRotateSisterTile());
    }

    IEnumerator debugStartGame () {
		yield return new WaitForSeconds(0.1f);
        //startTurn = true;
	}

	public void placeStartingCharacters () {
        if (validationButton.activeSelf) validationButton.SendMessage("hideButton");
		GameObject[] cibles = GameObject.FindGameObjectsWithTag("PlacementToken");
		foreach (GameObject cible in cibles) {
			PlacementTokens target = cible.GetComponent<PlacementTokens>();
			if (target.tokenAssociated != null) {
				Token token = target.tokenAssociated.GetComponent<Token>();
				token.caseActuelle = target.caseActuelle.gameObject;
				token.tokenPlace = true;
				token.cibleToken = null;
                token.ciblesTokens.Clear();
                onlineGameInterface.RecordPlacementCharacterChoice(target.tokenAssociated.GetComponent<Token>(), target);
            }
			else Debug.LogError("Game Manager, OnGUI: Le second joueur n'a pas rempli sa ligne de dÃƒÂ©part");
			cible.SetActive(false);
			Destroy(cible);
		}
        if (!otherPlayerTurnToPlace)
        {
            // when first player ready, switch player
            otherPlayerTurnToPlace = true;
            if (onlineGame) progression.StartProcess(); // NW: in order to block online notifications beyond character placement until game is ready
        }
        else
        {
            // when second player ready, go to next game state
            otherPlayerTurnToPlace = false;
            initiateTokenPlacement();
        }
		activePlayer.tempsRestant = 60.0f;
		switchPlayer();
	}

    public void initiateTokenPlacement()
    {
        gameMacroState = GameProgression.TokenPlacement;
        initiateHiddenTilesPlacementTokens();
    }

    public void initiateHiddenTilesPlacementTokens()
    {
        GameObject[] tiles = GameObject.FindGameObjectsWithTag("HiddenTile");
        for (int i = 0; i < tiles.GetLength(0); i++)
        {
            tiles[i].SendMessage("placementTokensSurSalle");
        }
        refreshPlacementDesTokens();
    }

	void completeTokenPlacement (bool playerSwitch) {
        /*string debugMessage = "CompleteTokenPlacement : ";
        debugMessage += otherPlayerTurnToPlace ? "Phase II - " : "Phase I - ";
        debugMessage += playerSwitch ? "switch - " : " no switch - ";
        int currentPlayerIndex = activePlayer.index;
        debugMessage += "turn of player " + (playerSwitch ? 1-currentPlayerIndex : currentPlayerIndex);
        Debug.LogError(debugMessage);*/

		GameObject[] cibles = GameObject.FindGameObjectsWithTag("TileRevealedTarget");
		for (int i=0 ; i < cibles.GetLength(0) ; i++) {
			PlacementTokens target = cibles[i].GetComponent<PlacementTokens>();
			if (target.tokenAssociated != null) {
				Token token = target.tokenAssociated.GetComponent<Token>();
				token.caseActuelle = target.caseActuelle.gameObject;
				token.tokenPlace = true;
				token.cibleToken = null;
                token.ciblesTokens.Clear();
                TokenIHM t = token.GetComponent<TokenIHM>();
                t.applyNewScale(t.stdScaleValue);
			}
			Destroy(cibles[i]);
        }
        // onlineGameInterface.EndReplayAction();
        placerTokens = false;
		selectionEnCours = false;
		if (playerSwitch) {
			otherPlayerTurnToPlace = false;
			switchPlayer();
        }
    }

    void initGameInfo ()
    {
        app = GameObject.FindGameObjectWithTag("App").GetComponent<AppManager>();
        GameSpecs specs = app.gameToLaunch;
        timedGame = specs.timedGame;
        
        if (specs.isTutorial) SceneManager.LoadScene(specs.tutorialName, LoadSceneMode.Additive);
        else
        {

            tilesPrefabs.Clear();
            tilesPrefabs.AddRange(specs.roomsToLoad);
            // A modifier plus tard ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            NOMBRE_TILES = tilesPrefabs.Count / 2;
            List<Vector3> roomsLocations = new List<Vector3>();
            // Placement des salles standard
            if (specs.standardGameLayout)
            {
                float standardZvalue = -1;
                // Si le nombre de salles est pair
                if (tilesPrefabs.Count % 2 == 0)
                {
                    Debug.Assert(tilesPrefabs.Count >= 2 && tilesPrefabs.Count <= 8, "Game Manager, initGameInfo: Trop de salles ajoutÃ©es Ã  la partie");
                    for (int x = -2 * (NOMBRE_TILES - 1); x <= 2 * (NOMBRE_TILES - 1); x += 4)
                        for (int y = 2; y >= -2; y -= 4)
                            roomsLocations.Add(new Vector3(x, y, standardZvalue));
                }
                else
                {
                    Debug.LogWarning("Game Manager, initGameInfo: TO DO");
                }
            }
            // Placement des salles spÃ©cifique
            else
            {
                Debug.LogWarning("Game Manager, initGameInfo: le placement non standard reste Ã  faire");
                // TODO
            }

            GameObject tileBack = app.data.tileBack;
            Transform tileContainer = GameObject.Find("Tiles").transform;
            int tokenSpacesNeeded = specs.charactersToLoad.Count + specs.itemsToLoad.Count;
            // S'il y a au moins 4 personnages pour remplir la rÃ©glette de dÃ©part, on les retire pour compter le nombre d'emplacement nÃ©cessaire sur les salles
            if (specs.charactersToLoad.Count >= 4) tokenSpacesNeeded -= 4;
            for (int i = 0; i < roomsLocations.Count; i++)
            {
                GameObject temp = (GameObject)Instantiate(tileBack, roomsLocations[i], Quaternion.identity);
                temp.transform.parent = tileContainer;

                if (tilesPrefabs.Count == 2)
                {
                    if (i == 0) temp.GetComponent<HiddenTileBehavior>().tokenSpaces = tokenSpacesNeeded / 2;
                    else temp.GetComponent<HiddenTileBehavior>().tokenSpaces = Mathf.CeilToInt((float)tokenSpacesNeeded / 2.0f);
                }
                else if (tilesPrefabs.Count == 4)
                {
                    if (i == 1 || i == 2) temp.GetComponent<HiddenTileBehavior>().tokenSpaces = Mathf.CeilToInt((float)tokenSpacesNeeded / 2.0f);
                    else temp.GetComponent<HiddenTileBehavior>().tokenSpaces = tokenSpacesNeeded / 2;
                }
                else if (tilesPrefabs.Count == 8)
                {
                    if (i == 2 || i == 3 || i == 4 || i == 5) temp.GetComponent<HiddenTileBehavior>().tokenSpaces = Mathf.CeilToInt((float)tokenSpacesNeeded / 4.0f);
                    else temp.GetComponent<HiddenTileBehavior>().tokenSpaces = tokenSpacesNeeded / 4;
                }
            }
            float closestRoomsFromStart = 0;
            foreach (Vector3 pos in roomsLocations)
            {
                if (Mathf.Abs(pos.x) > closestRoomsFromStart) closestRoomsFromStart = Mathf.Round(Mathf.Abs(pos.x));
            }
            GameObject yellow = (GameObject)Instantiate(app.data.startingLines[0], new Vector3(-closestRoomsFromStart - 2.61f, 0, 0), Quaternion.identity);
            GameObject blue = (GameObject)Instantiate(app.data.startingLines[1], new Vector3(closestRoomsFromStart + 2.61f, 0, 0), Quaternion.identity);
            yellow.transform.parent = blue.transform.parent = GameObject.Find("Game").transform;
            zoneDepartPrefabs.Clear();
            zoneDepartPrefabs.Add(yellow);
            zoneDepartPrefabs.Add(blue);

            if (!specs.loadExistingGame)
            {
                List<Sprite> tokenBacks = app.data.tokenBacks;
                Transform characterContainer = GameObject.Find("Personnages").transform;
                Transform itemContainer = GameObject.Find("Items").transform;
                float currentRatio = (float)Screen.width / (float)Screen.height;
                float expectedRatio = 16.0f / 9.0f;
                float multiplier = 1f;
                if (currentRatio < expectedRatio) multiplier = 0.9f * (expectedRatio / currentRatio);

                for (int index = 0; index < 2; index++)
                {
                    for (int i = 0; i < specs.charactersToLoad.Count; i++)
                    {
                        GameObject temp = (GameObject)Instantiate(specs.charactersToLoad[i], Vector3.zero, Quaternion.identity);
                        temp.GetComponent<Token>().indexToken = index;
                        temp.GetComponent<TokenIHM>().couleurDos = tokenBacks[index];
                        temp.transform.position = new Vector3(-7 + 2 * i, -4.85f * multiplier, 0);
                        temp.transform.parent = characterContainer;
                    }
                    for (int i = 0; i < specs.itemsToLoad.Count; i++)
                    {
                        GameObject temp = (GameObject)Instantiate(specs.itemsToLoad[i], Vector3.zero, Quaternion.identity);
                        temp.GetComponent<Token>().indexToken = index;
                        temp.GetComponent<TokenIHM>().couleurDos = tokenBacks[index];
                        temp.transform.position = new Vector3(-6 + 2.5f * i, 4.25f * multiplier, 0);
                        temp.transform.parent = itemContainer;
                    }
                }
            }
            // Mise Ã  jour de la liste des dos de salle (nÃ©cessaire pour instancier les salles)
            List<GameObject> tilesBacksToSort = new List<GameObject>();
            List<GameObject> tilesTop = new List<GameObject>();
            List<GameObject> tilesBottom = new List<GameObject>();
            tilesBacksToSort.AddRange(GameObject.FindGameObjectsWithTag("HiddenTile"));
            foreach (GameObject tileB in tilesBacksToSort)
            {
                if (Mathf.Approximately(tileB.transform.position.y, 2)) tilesTop.Add(tileB);
                else if (Mathf.Approximately(tileB.transform.position.y, -2)) tilesBottom.Add(tileB);
                else Debug.LogError("Game Manager, initGameBoard: le dos de salle n'est ni au dessus ni en dessous");
            }
            tilesBacksToSort.Clear();
            foreach (GameObject tileT in tilesTop)
            {
                if (tilesBacksToSort.Count == 0) tilesBacksToSort.Add(tileT);
                else
                {
                    for (int i = 0; i < tilesBacksToSort.Count; i++)
                    {
                        if (tileT.transform.position.x < tilesBacksToSort[i].transform.position.x) tilesBacksToSort.Insert(i, tileT);
                    }
                    if (!tilesBacksToSort.Contains(tileT)) tilesBacksToSort.Add(tileT);
                }
            }
            tilesTop.Clear();
            tilesTop.AddRange(tilesBacksToSort);
            tilesBacksToSort.Clear();
            foreach (GameObject tileB in tilesBottom)
            {
                if (tilesBacksToSort.Count == 0) tilesBacksToSort.Add(tileB);
                else
                {
                    for (int i = 0; i < tilesBacksToSort.Count; i++)
                    {
                        if (tileB.transform.position.x < tilesBacksToSort[i].transform.position.x) tilesBacksToSort.Insert(i, tileB);
                    }
                    if (!tilesBacksToSort.Contains(tileB)) tilesBacksToSort.Add(tileB);
                }
            }
            tilesBottom.Clear();
            tilesBottom.AddRange(tilesBacksToSort);
            tilesBacksToSort.Clear();

            tilesBacksToSort.AddRange(tilesTop);
            tilesBacksToSort.AddRange(tilesBottom);
            tilesBacks = tilesBacksToSort.ToArray();
        }
    }
	
	// Use this for initialization
	void Awake () {
        DestroyImmediate(GameObject.Find("Canvas Main Menu"));
        gManager = this;
        progression = new GameProgressionManager(this);
        combatManager = new CombatManager(this); // gameObject.AddComponent<CombatManager>();
        audio = GetComponent<AudioSource>();
        
        initGameInfo();
        initGameBoard();

		//initPlateau();
		initPlayers();

        if (app.gameToLaunch.loadExistingGame)
        {
            for (int i = 0; i < NOMBRE_TILES * 2; i++)
            {
                if (tilesBacks[i].GetComponent<Renderer>().enabled) tilesBacks[i].SendMessage("placementTokensSurSalle");
            }

            List<Sprite> tokenBacks = app.data.tokenBacks;
            Transform characterContainer = GameObject.Find("Personnages").transform;
            Transform itemContainer = GameObject.Find("Items").transform;
            if (onlineGame && app.gameToLaunch.loadExistingGame)
            {
                // Instancier les cibles de placement sur les salles fermÃƒÂ©es
                // TODO

                JSONObject tokens = app.bgaData.GetField("tokens");
                for (int i = 0; i < tokens.Count; i++)
                {
                    GameObject prefabToUse = app.data.characters[0];
                    if (tokens[i].GetField("type").str != "hidden") prefabToUse = app.data.getTokenPrefab(tokens[i].GetField("type").str);
                    GameObject temp = (GameObject)Instantiate(prefabToUse, Vector3.zero, Quaternion.identity);
                    int tokenIndex = 0;
                    if (tokens[i].GetField("player_id").str != onlineGameInterface.viewerId) tokenIndex = 1;
                    temp.transform.position = new Vector3(-7 + 2 * i, -4.85f, 0);
                    temp.GetComponent<TokenIHM>().couleurDos = tokenBacks[tokenIndex];
                    Token t = temp.GetComponent<Token>();
                    t.indexToken = tokenIndex;
                    t.referenceIndex = int.Parse(tokens[i].GetField("id").str);
                    if (tokens[i].GetField("location").str == "ingame")
                    {
                        t.caseActuelle = caseMatrix[int.Parse(tokens[i].GetField("y").str)][int.Parse(tokens[i].GetField("x").str)];
                        t.tokenPlace = true;
                        temp.transform.position = t.caseActuelle.transform.position;
                    }
                    else if (tokens[i].GetField("location").str == "ontile")
                    {
                        Transform transform = tilesBacks[int.Parse(tokens[i].GetField("location_arg").str)].transform;
                        PlacementTokens emplacement = null;
                        for (int j=0; j<transform.childCount; ++j)
                        {
                            emplacement = transform.GetChild(j).GetComponent<PlacementTokens>();
                            if (emplacement != null && emplacement.tokenAssociated == null)
                                break;
                            else
                                emplacement = null;
                        }
                        Debug.Assert(emplacement != null, "WTF: No emplacement found");
                        //t.placeToken(emplacement);
                        emplacement.tokenAssociated = temp;
                        emplacement.locked = true;
                        t.cibleToken = emplacement;
                        temp.transform.position = emplacement.transform.position;
                    }

                    if (tokens[i].GetField("category").str == "character")
                    {
                        temp.transform.parent = characterContainer;
                        CharacterBehavior chara = temp.GetComponent<CharacterBehavior>();
                        if (tokens[i].GetField("wounded").str == "1") chara.wounded = true;
                        if (tokens[i].GetField("wounded_this_turn").str == "1") chara.freshlyWounded = true;
                        if (tokens[i].GetField("blocked_this_turn").str == "1") chara.freshlyHealed = true;
                        chara.actionPoints = int.Parse(tokens[i].GetField("additional_actions").str);
                    }
                    else temp.transform.parent = itemContainer;
                }
            }
            gameMacroState = GameProgression.TokenPlacement; // WARNING : useless affection
            gameReady = true;
            gameMacroState = GameProgression.Playing;
        }
	}

	void Start () {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
		GameObject[] tiles = GameObject.FindGameObjectsWithTag("Tile");
        if (!onlineGame && !app.gameToLaunch.isTutorial)
        {
            if (tiles.Length != NOMBRE_TILES * 2) Debug.LogError("Game Manager, Start: Nombre de tiles incorrect: " + tiles.Length + " au lieu de " + NOMBRE_TILES * 2);

            // Appliquer la rotation alÃƒÂ©atoire initiale
		    foreach (var tile in tiles)
            {
                int rotationInitiale = UnityEngine.Random.Range(0, 4);
                for (int j=0 ; j < rotationInitiale ; j++) {
                    tile.GetComponent<TileBehaviorIHM>().rotateTile();
                }
            }
        }
		gameReady = true;

        if (!app.gameToLaunch.isTutorial)
        {
            // Lancer le placement des personnages du joueur actif
            playerSwitchHUD.GetComponent<SwitchPlayerBehavior>().setCurrentActivePlayer();
        }
        else
        {
            GameObject.Find("PlayerVS").SetActive(false);
            GameObject.Find("Header").SetActive(false);
            GameObject.Find("Historic button").SetActive(false);
            GameObject.Find("Left side swipe").transform.Find("open-swipe-button").gameObject.SetActive(false);
            GameObject.Find("Right side swipe").SetActive(false);
            //if (!boardHoldsActiveEnemyCharacters()) playerSwitchHUD.SetActive(false);
            gameReady = true;
            gameMacroState = GameProgression.Playing;
            refreshPlacementDesTokens();
            //startTurn = true;
            selectionEnCours = true;
            activePlayer.myTurn = true;
            // Variable détournée comme flag pour empêcher l'apparition non nécessaire du bouton de validation
        }

        // UI
        validationButton.SendMessage("hideButton");

        onlineGameInterface.Start(this);

        // En partie en ligne, afficher les pseudos des deux joueurs
        if (onlineGame)
        {
            players[0].GetComponent<PlayerBehavior>().playerName = app.onlineGameInterface.playerName(0);
            players[1].GetComponent<PlayerBehavior>().playerName = app.onlineGameInterface.playerName(1);
        }
        else
        {
            // En partie locale, afficher le pseudo du compte connecté, s'il y en a un
            if (app.bgaid.Length > 0)
            {
                players[0].GetComponent<PlayerBehavior>().playerName = app.bgaid;
                players[1].GetComponent<PlayerBehavior>().playerName = "Invité";
            }
            // Sinon, afficher simplement Joueur Jaune VS Joueur Bleu
            else
            {
                players[0].GetComponent<PlayerBehavior>().playerName = "JOUEUR JAUNE";
                players[1].GetComponent<PlayerBehavior>().playerName = "JOUEUR BLEU";
            }
        }

        GameObject[] tokens = GameObject.FindGameObjectsWithTag("Token");
        for (int i = 0; i < tokens.GetLength(0); i++)
        {
            tokens[i].GetComponent<Token>().referenceIndex = i;
        }

        if (app.musicOn) GameObject.FindGameObjectWithTag("BGM").GetComponent<AudioSource>().Play();

        saveState = new GameState();
        //againstAI = GameObject.FindGameObjectWithTag("App").GetComponent<AppManager>().gameToLaunch.opponentIsAI;

        try
        {
            GameObject.Find("Left player name").GetComponent<Text>().text = players[0].GetComponent<PlayerBehavior>().playerName;
            GameObject.Find("Right player name").GetComponent<Text>().text = players[1].GetComponent<PlayerBehavior>().playerName;
        }
        catch (NullReferenceException e)
        {
            Debug.LogWarning("Game Manager, Start: le format de l'interface ne permet pas d'afficher les noms des joueurs\n"+e);
        }

        if (!app.gameToLaunch.isTutorial) app.LoadGame();
    }

	// Instancier les deux joueurs
    void initPlayers()
    {
        AppManager app = GameObject.FindGameObjectWithTag("App").GetComponent<AppManager>();
        GameSpecs specs = app.gameToLaunch;

        players = new GameObject[2];
        combatManager.initCards();

        for (int i = 0; i < 2; i++)
        {
            PlayerBehavior player;
            if (onlineGameInterface.isOnlineOpponent(i))
            {
                players[i] = (GameObject)Instantiate(app.data.getOnlinePlayer(), transform.position, transform.rotation);
                player = players[i].GetComponent<OnlinePlayer>();
                player.enabled = true;
            }
            else
            {
                if (app.gameToLaunch.isTutorial && i != 0) players[i] = (GameObject)Instantiate(app.data.getScriptedPlayer(), transform.position, transform.rotation);
                else players[i] = (GameObject)Instantiate(app.data.getStandardPlayer(), transform.position, transform.rotation);
                player = players[i].GetComponent<PlayerBehavior>();
            }

            if (onlineGame)
            {
                players[i].GetComponent<PlayerBehavior>().onlineID = onlineGameInterface.playerId(i);
                players[i].name = onlineGameInterface.playerName(i);
            }
            else
            {
                players[i].name = "Player " + (i + 1);
            }

            player.index = i;
            for (int j = 0; j < cartesCombat.GetLength(0); j++)
            {
                player.combatCards.Add(cartesCombat[j]);
                player.combatCards[j].GetComponent<CombatCards>().indexCard = 1;
            }
        }

        setActivePlayer( players[progression.GetFirstPlayerToPlaceInFirstPart()] );
        updateGameBackground(activePlayer);

        if (specs.isTutorial)
        {
            StartCoroutine(setToturialStartLines());
        }
        else
        {
            GameObject blueStartingLine, yellowStartingLine;
            blueStartingLine = GameObject.Find("Blue Starting Line(Clone)");
            yellowStartingLine = GameObject.Find("Yellow Starting Line(Clone)");

            for (int j = 0; j < hauteurPlateau(); j++)
            {
                yellowStartingLine.transform.GetChild(j).GetComponent<CaseBehavior>().affiliationJoueur = players[0];
            }
            for (int j = 0; j < hauteurPlateau(); j++)
            {
                blueStartingLine.transform.GetChild(j).GetComponent<CaseBehavior>().affiliationJoueur = players[1];
            }
        }
	}

    public void updateGameBackground(PlayerBehavior player)
    {
        Renderer background = GameObject.Find("Background").GetComponent<Renderer>();
        background.sortingLayerName = "Background";
        if (player.index == 0) background.material.color = new Color(0.8f, 0.4f, 0.2f);
        else background.material.color = new Color(0.5f, 0.5f, 0.9f);
    }

    public void resetCiblesToken()
    {
        GameObject[] ciblesTokens = GameObject.FindGameObjectsWithTag("PlacementToken");
        foreach (GameObject cible in ciblesTokens)
        {
            GameObject.DestroyImmediate(cible);
        }
    }
    
    IEnumerator setToturialStartLines()
    {
        GameObject board = GameObject.Find("Board");
        if (board == null)
        {
            yield return new WaitForSeconds(0.1f);
            StartCoroutine(setToturialStartLines());
        }
        else
        {
            GameObject blueStartingLine, yellowStartingLine;
            blueStartingLine = board.transform.Find("Blue Tutorial Starting Line").gameObject;
            yellowStartingLine = board.transform.Find("Yellow Tutorial Starting Line").gameObject;
            for (int j = 0; j < hauteurPlateau(); j++)
            {
                CaseBehavior currentCase = yellowStartingLine.transform.GetChild(j).GetComponent<CaseBehavior>();
                currentCase.affiliationJoueur = players[0];
                currentCase.setIndex(j * longueurPlateau());
            }
            for (int j = 0; j < hauteurPlateau(); j++)
            {
                CaseBehavior currentCase = blueStartingLine.transform.GetChild(j).GetComponent<CaseBehavior>();
                currentCase.affiliationJoueur = players[1];
                currentCase.setIndex(j * longueurPlateau() + longueurPlateau() -1);
            }
        }
    }

    // Distribuer les salles alÃ©atoirement et en rÃ©cupÃ©rer les cases pour les stocker dans un tableau Ã  double entrÃ©e
    void initGameBoard()
    {
        if (app.gameToLaunch.isTutorial) StartCoroutine(setupTutorialBoard());
        else
        {
            // Initialisation du tableau des cases de jeu
            caseMatrix = new GameObject[hauteurPlateau()][];
            for (int i = 0; i < hauteurPlateau(); i++)
            {
                caseMatrix[i] = new GameObject[longueurPlateau()];
            }

            if (onlineGame && app.gameToLaunch.loadExistingGame)
            {
                JSONObject serverTilesData = app.bgaData.GetField("map_tiles");

                GameObject instance, tilePrefab;
                Vector3 pos;
                for (int i = 0; i < NOMBRE_TILES * 2; i++)
                {
                    tilePrefab = tilesPrefabs[0];
                    bool revealedTile = false;
                    int parseResult;
                    if (int.TryParse(serverTilesData[i].GetField("type").str, out parseResult))
                    {
                        tilePrefab = tilesPrefabs[int.Parse(serverTilesData[i].GetField("type").str) - 1];
                        revealedTile = true;
                    }
                    pos = tilesBacks[i].transform.position;

                    instance = (GameObject)Instantiate(tilePrefab, new Vector3(pos.x, pos.y, 10), tilePrefab.transform.rotation);
                    tilesBacks[i].GetComponent<HiddenTileBehavior>().tileAssociated = instance;
                    instance.GetComponent<TileBehavior>().index = i;
                    if (revealedTile)
                    {
                        tilesBacks[i].GetComponent<Collider>().enabled = false;
                        tilesBacks[i].GetComponent<Renderer>().enabled = false;
                        instance.GetComponent<TileBehavior>().hidden = false;
                    }

                    updateTileCells(instance.GetComponent<TileBehavior>());

                    int orientation = 0;
                    if (int.TryParse(serverTilesData[i].GetField("orientation").str, out parseResult)) orientation = parseResult;
                    bool changerSens = !instance.GetComponent<TileBehavior>().clockwiseRotation;
                    TileBehaviorIHM tile = instance.GetComponent<TileBehaviorIHM>();
                    for (int j = 0; j < orientation; j++)
                    {
                        tile.rotateTile(changerSens);
                    }
                }
            }
            else
            {
                // MÃ©lange des diffÃ©rentes tiles
                int temp;
                int[] indexes = new int[NOMBRE_TILES * 2];
                bool noDuplicate = true;
                for (int i = 0; i < NOMBRE_TILES * 2; i++)
                {
                    do
                    {
                        temp = UnityEngine.Random.Range(0, NOMBRE_TILES * 2);
                        noDuplicate = true;
                        for (int j = 0; j < i; j++)
                        {
                            if (temp == indexes[j]) noDuplicate = false;
                        }
                    } while (!noDuplicate);
                    indexes[i] = temp;
                }

                GameObject instance, tilePrefab;
                Vector3 pos;
                for (int i = 0; i < NOMBRE_TILES * 2; i++)
                {
                    tilePrefab = tilesPrefabs[indexes[i]];
                    pos = tilesBacks[i].transform.position;

                    instance = (GameObject)Instantiate(tilePrefab, new Vector3(pos.x, pos.y, 10), tilePrefab.transform.rotation);
                    tilesBacks[i].GetComponent<HiddenTileBehavior>().tileAssociated = instance;
                    instance.GetComponent<TileBehavior>().index = i;

                    updateTileCells(instance.GetComponent<TileBehavior>());
                }
            }

            List<Transform> casesZone1 = new List<Transform>();
            List<Transform> casesZone2 = new List<Transform>();
            // Ajout des zones de dÃ©part 1 et 2
            for (int j = 0; j < hauteurPlateau(); j++)
            {
                //Debug.Log(zoneDepartPrefabs[0].transform.GetChild(j));
                casesZone1.Add(zoneDepartPrefabs[0].transform.GetChild(j));
            }
            for (int j = 0; j < hauteurPlateau(); j++)
            {
                //Debug.Log(zoneDepartPrefabs[1].transform.GetChild(j));
                casesZone2.Add(zoneDepartPrefabs[1].transform.GetChild(j));
            }
            sortCasesOfStartZone(casesZone1, 0);
            sortCasesOfStartZone(casesZone2, longueurPlateau() - 1);
        }
    }

    IEnumerator setupTutorialBoard()
    {
        GameObject board = GameObject.Find("Board");
        if (board == null)
        {
            yield return new WaitForSeconds(0.1f);
            StartCoroutine(setupTutorialBoard());
        }
        else
        {
            PremadeBoardSetupParameters boardParameters = board.GetComponent<PremadeBoardSetupParameters>();
            boardLength = boardParameters.boardLength;
            NOMBRE_TILES = boardParameters.nbTiles;
            actionPoints = boardParameters.actionPoints;
            if (actionPoints != 0)
            {
                selectionEnCours = false;
                turnStarted = true;
                actionCardsButton.SetActive(false);
            }
            if (!boardParameters.useActionCards) actionCardsButton.SetActive(false);

            caseMatrix = new GameObject[hauteurPlateau()][];
            for (int i = 0; i < hauteurPlateau(); i++)
            {
                caseMatrix[i] = new GameObject[longueurPlateau()];
            }

            GameObject yellowStartZone = boardParameters.getLeftStartZone();
            GameObject blueStartZone = boardParameters.getRightStartZone();
            List<Transform> casesZone1 = new List<Transform>();
            List<Transform> casesZone2 = new List<Transform>();
            // Ajout des zones de dÃ©part 1 et 2
            for (int j = 0; j < hauteurPlateau(); j++)
            {
                casesZone1.Add(yellowStartZone.transform.GetChild(j));
            }
            for (int j = 0; j < hauteurPlateau(); j++)
            {
                casesZone2.Add(blueStartZone.transform.GetChild(j));
            }
            sortCasesOfStartZone(casesZone1, 0);
            sortCasesOfStartZone(casesZone2, longueurPlateau() - 1);

            GameObject[] tutorialTiles = GameObject.FindGameObjectsWithTag("Tile");
            foreach (GameObject tile in tutorialTiles)
            {
                int[] anchor = tile.GetComponent<TileBehavior>().topLeftCasePositionInMatrix;
                try
                {
                    int anchorX = anchor[0];
                    int anchorY = anchor[1];
                    for (int x = 0; x < 5; x++)
                    {
                        for (int y = 0; y < 5; y++)
                        {
                            GameObject currentCase = tile.transform.Find("Case " + (x + 1).ToString() + (y + 1).ToString()).gameObject;
                            caseMatrix[anchorX + x][anchorY + y] = currentCase;
                            currentCase.GetComponent<CaseBehavior>().setIndex((anchorX + x) * longueurPlateau() + anchorY + y);
                            //Debug.Log("(" + (anchorX + x) + "," + (anchorY + y) + ")" + currentCase + " (index " + currentCase.GetComponent<CaseBehavior>().getIndex()+ ")");
                        }
                    }
                }
                catch (System.IndexOutOfRangeException e)
                {
                    Debug.LogError("Game Manager, setupTutorialBoard: Tile " + tile.name + " anchor has not been set\nStack: "+e);
                }
            }
            // Remettre les paramètres en état pour démarrer le niveau de tutoriel
            if (boardParameters.opponentStartFirst) switchPlayer(true);
        }
    }

    public void updateTileCells(TileBehavior tile)
    {
        int indexTile = tile.index;
        int matrixRow = 5 * (indexTile / NOMBRE_TILES);
        int matrixColumn = 5 * (indexTile % NOMBRE_TILES) + 1;
        for (int row = 0; row < 5; row++)
        {
            for (int column = 0; column < 5; column++)
            {
                GameObject currentCase = tile.transform.Find("Case " + (row + 1).ToString() + (column + 1).ToString()).gameObject;
                caseMatrix[matrixRow + row][matrixColumn + column] = currentCase;
                currentCase.GetComponent<CaseBehavior>().setIndex((matrixRow + row) * longueurPlateau() + matrixColumn + column);
            }
        }
    }

    void sortCasesOfStartZone (List<Transform> casesColumn, int column) {
        float max = int.MinValue;
		int row = -1, taille = hauteurPlateau();
		Transform[] temp = new Transform[taille];
		//Debug.Log(casesColumn.Count);
		for (int i=0 ; i < taille ; i++) {
			for (int j=0 ; j < casesColumn.Count ; j++) {
				if (casesColumn[j].position.y > max) {
					max = casesColumn[j].position.y;
					row = j;
				}
			}
			//Debug.Log("row : "+row+" ; value : "+max);
			//Debug.Log(casesColumn[row]);
			temp[i] = casesColumn[row];
			casesColumn.RemoveAt(row);
			max = Int32.MinValue;
		}
		
		for (int i=0 ; i < taille ; i++) {
			//Debug.Log(temp[i].parent.name+" "+temp[i]+" : "+temp[i].position.x);
			caseMatrix[i][column] = temp[i].gameObject;
			//Debug.Log(i+" : "+caseMatrix[i][column]);
			caseMatrix[i][column].GetComponent<CaseBehavior>().setIndex(i*longueurPlateau() + column);
		}
		//Debug.LogWarning("OK");
	}

	void sortCasesOfTiles(List<Transform> casesRow, int row) {
        float min = int.MaxValue;
		int column = -1, taille = casesRow.Count;
		Transform[] temp = new Transform[taille];
		for (int i=0 ; i < taille ; i++) {
			for (int j=0 ; j < casesRow.Count ; j++) {
				if (casesRow[j].position.x < min) {
					min = casesRow[j].position.x;
					column = j;
				}
			}
			temp[i] = casesRow[column];
			casesRow.RemoveAt(column);
            min = Int32.MaxValue;
		}

		for (int i=1 ; i < taille+1 ; i++) {
			//Debug.Log(temp[i].parent.name+" "+temp[i]+" : "+temp[i].position.x);
			caseMatrix[row][i] = temp[i-1].gameObject;
			caseMatrix[row][i].GetComponent<CaseBehavior>().setIndex(row*longueurPlateau() + i);
		}
	}

	// Placement des cibles sur la rÃƒÂ©glette pour les 4 personnages de dÃƒÂ©part
	public void placementPersonnagesDepart () {
		GameObject temp;
		int column = 0;
		if (activePlayer.index != 0) column = longueurPlateau()-1;
		// On parcours chaque case de la rÃƒÂ©glette et on crÃƒÂ©e une cible sur chaque case de dÃƒÂ©part
		for (int i=0 ; i < hauteurPlateau() ; i++) {
			if (caseMatrix[i][column].GetComponent<CaseBehavior>().caseDeDepart) {
				Vector3 positionDeDepart = caseMatrix[i][column].transform.position;
				temp = (GameObject) Instantiate(ciblePlacementToken, new Vector3(positionDeDepart.x, positionDeDepart.y, 0.1f), ciblePlacementToken.transform.rotation);
				temp.GetComponent<PlacementTokens>().caseActuelle = caseMatrix[i][column].GetComponent<CaseBehavior>();
				temp.GetComponent<SpriteRenderer>().enabled = true;
			}
		}
		refreshPlacementDesTokens();
	}
	
	// Mise ÃƒÂ  jour des cibles disponibles
	void refreshPlacementDesTokens () {
		GameObject[] tokens = GameObject.FindGameObjectsWithTag("Token");
		for (int i=0 ; i < tokens.GetLength(0) ; i++) {
			tokens[i].GetComponent<Token>().refreshPlacementToken();
		}
    }

    public void resetMoveFlags()
    {
        foreach (GameObject c in gManager.casesAtteignables)
            c.GetComponent<CibleDeplacementIHM>().resetMovePossibility();
        casesAtteignables.Clear();
        resetCellMoveFlags();
    }

    public void resetPartialMoveFlags()
    {
        foreach (GameObject c in casesAtteignables)
			c.GetComponent<CibleDeplacementIHM>().pastMovePossibility();
        resetCellMoveFlags();
    }

    void resetCellMoveFlags()
    {
        for (int i = 0; i < hauteurPlateau(); i++)
            for (int j = 0; j < longueurPlateau(); j++)
                getCase(i, j).GetComponent<CaseBehavior>().selectionnePourDeplacement = false;
    }

    // Animation de sortie d'un personnage
    IEnumerator victoryParticles () {
		ParticleEmitter fireworks = GameObject.Find("Particles").GetComponent<ParticleEmitter>();
		//fireworks.emit = true;
		yield return new WaitForSeconds(VICTORY_ANIM_DURATION);
		fireworks.emit = false;
	}

    // Applique une rotation sur toute les cases d'une salle donnÃƒÂ©e
	public void rotateTile(GameObject tile, bool displayFeedback) {
		GameObject[][] tempTile = new GameObject[5][];
		int tileIndex = tile.GetComponent<TileBehavior>().index;
		for (int i=0 ; i < 5 ; i++) {
			tempTile[i] = new GameObject[5];
		}

		int iAdjustment, jAdjustment, tempI=0, tempJ=0;
		if (tileIndex < NOMBRE_TILES) iAdjustment = 0;
		else iAdjustment = 5;
		jAdjustment = 5 * (tileIndex%NOMBRE_TILES) + 1;
		//Debug.Log("iAdjustment : "+iAdjustment+ " ; jAdjustment : "+jAdjustment);
		if (tile.GetComponent<TileBehavior>().clockwiseRotation) {
			for (int j = jAdjustment ; j < 5+jAdjustment ; j++) {
				for (int compteurInverse = 4+iAdjustment ; compteurInverse >= iAdjustment ; compteurInverse--) {
					//Debug.Log("j : "+j+ " ; compteurInverse : "+compteurInverse);
					tempTile[tempI][tempJ] = caseMatrix[compteurInverse][j];
					tempJ++;
				}
				tempJ = 0;
				tempI++;
			}
		}
		else {
			for (int compteurInverse = 4+jAdjustment ; compteurInverse >= jAdjustment ; compteurInverse--) {
				for (int i = iAdjustment ; i < 5+iAdjustment ; i++) {
					//Debug.Log("j : "+j+ " ; compteurInverse : "+compteurInverse);
					tempTile[tempI][tempJ] = caseMatrix[i][compteurInverse];
					tempJ++;
				}
				tempJ = 0;
				tempI++;
			}
		}
		tempI = 0;
		tempJ = 0;
		for (int i = iAdjustment ; i < 5+iAdjustment ; i++) {
			for (int j = jAdjustment ; j < 5+jAdjustment ; j++) {
				//Debug.Log(i+" / "+j);
				caseMatrix[i][j] = tempTile[tempI][tempJ];
				//Debug.Log(caseMatrix[i][j].ToString() + " ; "+indexFromMatrix(i, j));
				caseMatrix[i][j].GetComponent<CaseBehavior>().setIndex(indexFromMatrix(i, j));
				//Debug.Log(caseMatrix[i][j].GetComponent<CaseBehavior>().getIndex());
				tempJ++;
			}
			tempJ = 0;
			tempI++;
		}
        if (gameReady && displayFeedback) tile.SendMessage("refreshTokenPositions");
	}

	// Met ÃƒÂ  jour le nombre de points d'action disponibles et rend indisponible la carte utilisÃƒÂ©e par la joueur
    public void actionCardChosen(int value)
    {
        displayInformationMessage("Vous avez joué une carte " + value + " actions");
        fadeOutImage(pickActionCardSign);
		actionPoints = value;
        // Si la carte choisie est la valeur maximale d'action disponible : on augmente la valeur maximale
        if (value == valeurMaxCarteAction) valeurMaxCarteAction++;
        activePlayer.usedActionCards[value-2] = true;
        activePlayer.updatePanelAppearance();
        selectionEnCours = false;
        turnStarted = true;
        onlineGameInterface.RecordCard(ActionType.ACTION_CARD, value);
        onlineGameInterface.EndReplayAction();
    }

	// Diminue d'un le nombre de points d'action
	public void decreaseActionPoints () {
        if (actionPointCost > 0) actionPointCost--;
        else Debug.LogWarning("ActionPointCost is not up to date for spending. Current value is " + actionPointCost);

        // Diminue en prioritÃƒÂ© les actions sur le personnage, s'il possÃƒÂ¨de des points d'action supplÃƒÂ©mentaire
        if (actionCharacter != null && actionCharacter.GetComponent<CharacterBehavior>().actionPoints > 0) {
            actionCharacter.GetComponent<CharacterBehavior>().actionPoints--;
		}
		else actionPoints--;
	}

	void oneTokenPlaced () {
		if (gameMacroState == GameProgression.TokenPlacement)
        {
			GameObject[] cibles = GameObject.FindGameObjectsWithTag("PlacementToken");
			bool someTargetsRemaining = false;
			foreach (GameObject cible in cibles) {
				someTargetsRemaining = someTargetsRemaining || (cible.GetComponent<PlacementTokens>().tokenAssociated == null);
			}
			if (someTargetsRemaining) {
				if (nonActivePlayer.tempsRestant <= 0) {
					if (activePlayer.tempsRestant > 0) nonActivePlayer.placeOneTokenAtRandom();
					else {
                        nonActivePlayer.placeOneTokenAtRandom();
						activePlayer.placeOneTokenAtRandom();
					}
				}
				else switchPlayer();
			}
		}
		else switchPlayer();
	}

    void switchPlayerTurn(bool displayFeedback = true)
    {
        activePlayer.myTurn = false;
        switchPlayer(displayFeedback);
    }

    // Changement de joueur
	public void switchPlayer (bool displayFeedback = true) {
		setActivePlayer(nonActivePlayer.gameObject);
        startPlayerTurn(displayFeedback);
    }

    public void startPlayerTurn(bool displayFeedback = true)
    {
        if (playerSwitchHUD.GetComponent<SwitchPlayerBehavior>().isDisplayEnabled()) Debug.LogWarning("SwitchPlayer already activated !");
        startTurn = false;
        if (gameMacroState == GameProgression.Playing)
        {
            bool tokensToPlace = false;
            GameObject[] tokens = GameObject.FindGameObjectsWithTag("Token");
            foreach (GameObject token in tokens)
            {
                Token t = token.GetComponent<Token>();
                if (!t.tokenPlace && t.cibleToken == null) tokensToPlace = true;
            }
            if (displayFeedback && !tokensToPlace && !combatEnCours && !app.gameToLaunch.isTutorial) Camera.main.SendMessage("dezoom");
        }
        if (displayFeedback) playerSwitchHUD.GetComponent<SwitchPlayerBehavior>().setCurrentActivePlayer();
    }

    public void fadeOutImage(Image image)
    {
        StartCoroutine(imageColorTransition(Time.time, 0.4f, image, image.color, new Color(image.color.r, image.color.g, image.color.b, 0)));
    }

    IEnumerator imageColorTransition(float startTime, float duration, Image image, Color from, Color to)
    {
        float valueProgression = (Time.time - startTime) / duration;
        image.color = Color.Lerp(from, to, valueProgression);
        yield return new WaitForSeconds(0.01f);
        if (Time.time - startTime < duration) StartCoroutine(imageColorTransition(startTime, duration, image, from, to));
        else
        {
            image.color = to;
            if (to.a == 0)
            {
                image.enabled = false;
                image.color = from;
            }
        }
    }

	bool canDisplayStandardGUI () {
		return (selectionEnCours && actionCharacter != null && !combatEnCours);
	}
	
	public bool canDisplayRotationGUI () {
		return (canDisplayStandardGUI() && rotationEnCours);
	}

	public bool canDisplayTokenGUI () {
		return (canDisplayStandardGUI() && !rotationEnCours);
	}
	
	public void openCurrentCharacterExchangeUI () {
		if (actionCharacter != null) {
			forceExchangeUI = true;
			if (deplacementEnCours) actionCharacter.GetComponent<CharacterBehaviorIHM>().pickUpOverflownToken();
			else actionCharacter.GetComponent<CharacterBehaviorIHM>().openCharacterExchangeUI();
		}
		else Debug.LogWarning("Game Manager, openCurrentCharacterExchangeUI: Aucun personnage sÃƒÂ©lectionnÃƒÂ©");
	}

	public bool isInformationAndExchangeUIOpen () {
        return (exchangeUI.GetComponent<InfoCharacterPanel>().open);
	}
	
	public void closeInformationAndExchangeUI () {
        if (actionCharacter != null)
        {
			closeCharacterExchangeUI();
            exchangeUI.GetComponent<InfoCharacterPanel>().panelToPreviewPosition();
		}
		else Debug.LogWarning("Game Manager, closeInformationAndExchangeUI: Aucun personnage sÃƒÂ©lectionnÃƒÂ©");
	}
	
	public void closeCharacterExchangeUI () {
        if (actionCharacter != null)
        {
            clearListAndDestroyOccupyingTokens(exchangePoints);
            forceExchangeUI = false;
			if (!gManager.isActivePlayer(actionCharacter.GetComponent<CharacterBehavior>().affiliationJoueur)) actionCharacter = null;
		}
        else Debug.LogWarning("Game Manager, closeCharacterExchangeUI: Aucun personnage sÃƒÂ©lectionnÃƒÂ©");
    }

    void clearListAndDestroyOccupyingTokens(List<GameObject> spots)
    {
        foreach (GameObject spot in spots)
        {
            if (spot.GetComponent<ExchangePointUI>().occupyingToken != null)
            {
                Destroy(spot.GetComponent<ExchangePointUI>().occupyingToken);
            }
        }
        spots.Clear();
    }

    public void forfeitGame()
    {
        if (onlineGame)
        {
            // TODO: callback
            app.onlineGameInterface.ForfeitGame();
            //returnToMainMenu();
        }
    }

    public void deleteCurrentGame()
    {
        if (!onlineGame) app.deleteSavedGame();
    }

    public void resetGame()
    {
        //SceneManager.LoadScene(Application.loadedLevelName);
        sceneTransition(1.0f, "game");
    }

    public void skipTurn()
    {
        if (turnStarted)
        {
            GameObject[] tokens = GameObject.FindGameObjectsWithTag("Token");
            foreach (GameObject token in tokens)
            {
                if (token.GetComponent<CharacterBehavior>() != null)
                {
                    token.GetComponent<CharacterBehavior>().actionPoints = 0;
                }
            }
            actionPoints = 0;
            onlineGameInterface.RecordPass();
            changementTourJoueur(true);
        }
    }

    public void returnToMainMenu()
    {
        StartCoroutine(endGame());
    }

    IEnumerator endGame()
    {
        yield return new WaitForSeconds(0.5f);
        sceneTransition(1.0f, "menu");
        //Application.LoadLevel("menu");
    }

    IEnumerator tutorialMissionFailed()
    {
        yield return new WaitForSeconds(1f);
        GameObject failureFeedback = GameObject.Find("Failure text feedback");
        failureFeedback.GetComponent<Text>().enabled = true;
        failureFeedback.SendMessage("launchFadeIn");
        yield return new WaitForSeconds(3f);
        sceneTransition(1.0f, "game");
    }

    IEnumerator endMission(bool returnToMainMenu)
    {
        yield return new WaitForSeconds(1);
        GameObject failureFeedback = GameObject.Find("Clear text feedback");
        failureFeedback.GetComponent<Text>().enabled = true;
        failureFeedback.SendMessage("launchFadeIn");
        yield return new WaitForSeconds(1);
        if (returnToMainMenu)
        {
            GameObject menu = GameObject.Find("Options menu");
            menu.SendMessage("openMenu");
            menu.transform.Find("Cadre/Close").gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(2);
            loadNextTutorialLevel();
        }
    }

    public void loadNextTutorialLevel()
    {

        string currentLevel = app.gameToLaunch.tutorialName[app.gameToLaunch.tutorialName.Length - 1].ToString();
        int nextLevelNumber = (1 + int.Parse(currentLevel));

        if (nextLevelNumber > 9)
        {
            app.firstLaunchTutorialComplete = true;
            app.saveAppData();
            sceneTransition(1.0f, "menu");
        }
        else
        {
            string nextLevelName = "tuto0" + nextLevelNumber.ToString();
            app.gameToLaunch.tutorialName = nextLevelName;
            app.currentTutorialLevel = nextLevelName;
            app.saveAppData();

            //Application.LoadLevel(Application.loadedLevel);
            //Scene nextTuto = SceneManager.GetSceneByName(nextLevelName);
            //List<Scene> scenes = new List<Scene>();
            //scenes.AddRange(SceneManager.GetAllScenes());
            //Scene nextTuto = SceneManager.GetSceneByName("tuto02");
            //if (scenes.Contains(nextTuto))

            sceneTransition(1.0f, "game");
        }
    }

    void sceneTransition(float duration, string nextScene)
    {
        sceneTransitionFade.color = transparentBlack;
        sceneTransitionFade.enabled = true;
        StartCoroutine(sceneTransitionCoroutine(Time.time, duration, nextScene));
    }

    IEnumerator sceneTransitionCoroutine (float startTime, float duration, string nextScene)
    {
        float valueProgression = (Time.time - startTime) / duration;
        sceneTransitionFade.color = Color.Lerp(transparentBlack, Color.black, valueProgression);
        yield return new WaitForSeconds(0.01f);
        if (Time.time - startTime < duration) StartCoroutine(sceneTransitionCoroutine(startTime, duration, nextScene));
        else
        {
            sceneTransitionFade.color = Color.black;
            SceneManager.LoadScene(nextScene);
        }
    }

    public static string getActionTypeName(ActionType action)
    {
        switch (action)
        {
            case ActionType.CLOSEDOOR: return "Fermer";
            case ActionType.OPENDOOR: return "Ouvrir";
            case ActionType.DESTROYDOOR: return "Briser";
            case ActionType.FIREBALL: return "Boule de Feu";
            case ActionType.HEAL: return "Soin";
            case ActionType.REGENERATE: return "Régénération";
            case ActionType.ROTATE: return "Rotation";
            case ActionType.SPEEDPOTION: return "Potion de Vitesse";
            default: return null;
        }
    }

    public void playSound(AudioClip sound)
    {
        playSound(sound, 1);
    }

    public void playSound(AudioClip sound, float volume)
    {
        if (app.soundOn) audio.PlayOneShot(sound, volume);
    }

    public void displayInformationMessage(string newMessage)
    {
        messageUI.transform.GetChild(messageUI.transform.childCount - 1).GetComponent<Text>().text = newMessage;
        messageUI.GetComponent<SmallMenuAnimation>().openMenu();
    }
	
	public int indexFromMatrix(int i, int j) {
		return (i*longueurPlateau() + j);
	}
	
	public int rowMatrixFromIndex(int index) {
		return Mathf.FloorToInt(index/longueurPlateau()) ;
	}
	
	public int columnMatrixFromIndex(int index) {
		return index%longueurPlateau();
	}

	public int longueurPlateau() {
		return ( 2 + NOMBRE_TILES*5 );
	}

	public int hauteurPlateau() {
		return boardLength;
	}

	public GameObject getCase(int i, int j) {
		return caseMatrix[i][j];
	}

    public bool boardHoldsActiveEnemyCharacters()
    {
        GameObject[] characters = GameObject.FindGameObjectsWithTag("Token");
        foreach (GameObject token in characters)
        {
            if (token.GetComponent<CharacterBehavior>() != null && !isActivePlayer(token.GetComponent<Token>().affiliationJoueur))
            {
                CharacterBehavior chara = token.GetComponent<CharacterBehavior>();
                if (chara.isTokenOnBoard() && (!chara.wounded || chara.GetComponent<CB_Troll>() != null)) return true;
            }
        }
        return false;
    }

    public bool playerInteractionAvailable() { return !activePlayer.isScriptedPlayer() && !activePlayer.isOnlinePlayer(); }
    
    public PlayerBehavior activePlayer { get { return joueurActif.GetComponent<PlayerBehavior>(); } }
    public PlayerBehavior nonActivePlayer { get { return players[1 - activePlayer.index].GetComponent<PlayerBehavior>(); } }
    public bool isActivePlayer(GameObject activePlayerObject) { return joueurActif == activePlayerObject; }
    public void setActivePlayer(GameObject activePlayerObject) { joueurActif = activePlayerObject; }

    public List<Token> GetAllTokens()
    {
        List<Token> res = new List<Token>();
        GameObject[] tokens = GameObject.FindGameObjectsWithTag("Token");
        foreach (GameObject token in tokens)
                res.Add(token.GetComponent<Token>());
        return res;
    }
    public List<CharacterBehavior> GetAllCharacters()
    {
        List<CharacterBehavior> res = new List<CharacterBehavior>();
        GameObject[] tokens = GameObject.FindGameObjectsWithTag("Token");
        foreach (GameObject token in tokens)
            if (token.GetComponent<CharacterBehavior>() != null)
                res.Add(token.GetComponent<CharacterBehavior>());
        return res;
    }
    public List<Item> GetAllItems()
    {
        List<Item> res = new List<Item>();
        GameObject[] tokens = GameObject.FindGameObjectsWithTag("Token");
        foreach (GameObject token in tokens)
            if (token.GetComponent<Item>() != null)
                res.Add(token.GetComponent<Item>());
        return res;
    }

    #region combat
    public UnityEngine.Object InstantiateAccessForCombatManager(UnityEngine.Object original, Vector3 position, Quaternion rotation) { return Instantiate(original, position, rotation); }
    public void DestroyAccessForCombatManager(UnityEngine.Object obj) { Destroy(obj); }
    #endregion
}


















