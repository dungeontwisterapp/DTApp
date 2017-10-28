using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class TokenIHM : MonoBehaviour {

	protected Token associatedToken;
	protected GameManager gManager;

	protected GameObject tokenInfo;
	protected GameObject tokenHighlight;
	public Sprite actionDoableHighlight;
	public Sprite couleurDos;
	public bool tokenHidden = false;
	public GameObject previousCase;
    public GameObject previousPersonnageAssocie;

    public AudioClip tokenGetSound;
    public AudioClip tokenDropSound;

	float distanceSnapInitial = 0.6f;
	float distanceSnapStandard = 0.3f;

    [HideInInspector]
    public float stdScaleValue;

	protected SphereCollider sCollider;
	SpriteRenderer sRenderer;
	Sprite standardHighlight;
	int orderInLayerHidden = 4;
	int orderInLayerVisible = 0;
	
	public List<GameObject> moveTargets = new List<GameObject>();
	public bool cancelMovementPossible = false;
	protected float selectionTimer;
	protected float SIMPLE_CLICK_DURATION = 0.1f;
	public bool gliding = false;

    public bool bAllowInGameDrag = false;
	
	protected Ray ray;
	protected RaycastHit hit;

    Color transparentSpriteColor = new Color(1, 1, 1, 0);

    Vector3 returnSpot;

    // Use this for initialization
    void Awake () {
		initialization();
	}

	public virtual void Start () {
		standardHighlight = tokenHighlight.GetComponent<SpriteRenderer>().sprite;
		sCollider = GetComponent<SphereCollider>();
        sCollider.radius = stdColliderRadius;
        sRenderer = GetComponent<SpriteRenderer>();
		sRenderer.sprite = couleurDos;
		highlightToken(false);
        returnSpot = transform.position;
        if (associatedToken.tokenHolder != null)
        {
            associatedToken.tokenHolder.GetComponent<CharacterBehaviorIHM>().ramasserToken(gameObject, false);
        }
	}

	public virtual void initialization () {
		associatedToken = GetComponent<Token>();
		if (associatedToken == null) {
			Debug.LogError("TokenIHM, Awake: Le script Token n'a pas été trouvé sur le meme Game Object");
			this.enabled = false;
		}
        gManager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameManager>();
        tokenInfo = transform.Find("Token_Info").gameObject;
        tokenHighlight = transform.Find("Token_Highlight").gameObject;
        stdScaleValue = transform.localScale.x;
    }
	
	public bool canDisplayTokenGUI () {
		return (gManager.canDisplayTokenGUI() && associatedToken.tokenHolder.gameObject == gManager.actionCharacter);
	}
	
	// Update is called once per frame
	void Update () {
        if (!gManager.freezeDisplay)
        {
            placementDesTokens();
            if (associatedToken.selected)
            {
                foreach (GameObject target in moveTargets)
                {
                    target.GetComponent<SpriteRenderer>().color = Color.green;
                }
            }
        }
	}

	// Gérer l'apparence des tokens et la mise en place du plateau
	public void placementDesTokens () {
		// Si le token n'est pas encore placé sur une case
		if (!associatedToken.tokenPlace) {
			// Placement des tokens sur le plateau
            if (gManager.gameMacroState == GameProgression.GameStart || gManager.gameMacroState == GameProgression.TokenPlacement)
            {
				// Si le token n'est pas encore mis sur un emplacement
				if (associatedToken.cibleToken == null) {
					// Si c'est au tour du joueur qui possède ce token de placer
					if (gManager.isActivePlayer(associatedToken.affiliationJoueur)) {
                        if (gManager.startTurn)
                        {
                            // Afficher le token
                            displayToken();
                            // Si on joue une partie en ligne, on garde le token face cachée pendant que l'adversaire joue
                            if (gManager.onlineGameInterface.isOnlineOpponent(associatedToken.indexToken)) coverToken();
                            else
                            {
                                // Mettre le token face visible
                                revealToken();
                                GetComponent<Collider>().enabled = true;
                                // Afficher le halo si le token est un personnage OU que les 4 personnages initiaux ont déjà été placés
                                if (gManager.gameMacroState != GameProgression.GameStart || GetComponent<Item>() == null)
                                    highlightToken(true);
                            }
                        }
                        else
                        {
                            hideToken();
                            GetComponent<Collider>().enabled = false;
                        }
					}
					// Sinon faire disparaitre le token
					else {
						hideToken();
						GetComponent<Collider>().enabled = false;
					}
				}
				// Si le token est mis sur un emplacement
                else manageTokenInfoDisplay();

				// Griser les Items pendant le placement des 4 premiers personnages, les repasser en blanc après
				if (GetComponent<Item>() != null) {
					Color newColor = Color.white;
                    if (gManager.gameMacroState == GameProgression.GameStart)
                        newColor = Color.gray;
					GetComponent<Renderer>().material.color = newColor;
					Renderer[] itemInfo = GetComponentsInChildren<Renderer>();
					foreach(Renderer r in itemInfo) r.material.color = newColor;
				}
			}
			// La partie a commencé
			else {
				// Afficher le token
				displayToken();
				// Le token est rattaché à un emplacement sur une case ou sur une salle, mais pas encore posé sur le plateau
				if (associatedToken.cibleToken != null) {
					// Si le token est associé à une salle, il est présenté face cachée et il n'est pas cliquable
					if (associatedToken.cibleToken.caseActuelle == null) {
						coverToken();
						GetComponent<Collider>().enabled = false;
					}
					// Sinon le token est placé dans une salle en train d'etre ouverte, il est présenté face visible avec un halo
					else {
						revealToken();
						highlightToken(true);
					}
				}
				// Le token est en attente d'etre placé dans une salle en train d'etre ouverte
				else {
					revealToken();
					GetComponent<Collider>().enabled = true;
					// Si on peut déplacer le token, on affiche son halo
					if (canMoveToken()) highlightToken(true);
				}
			}

            // Agrandir le token quand il n'est pas placé sur le plateau
            if (associatedToken.cibleToken == null)
            {
                SetScaleAndCollider(NOT_PLACED_SCALE_VALUE);
            }
            // Sinon le ramener à sa taille standard
            else
            {
                changeSortingLayer("TokensOnBoard");
                highlightToken(false);
                SetScaleAndCollider(NORMAL_SCALE_VALUE);
            }
		}
		// Si le token est placé sur une case
		else if (!associatedToken.isRemovedFromGame())
        {
            if (tokenHidden)
            {
                GetComponent<Collider>().enabled = false;
                hideToken();
            }
            else
            {
                // Si la partie n'a pas encore commencé, les pions placés sont face cachée
                if (gManager.gameMacroState == GameProgression.GameStart || gManager.gameMacroState == GameProgression.TokenPlacement) manageTokenInfoDisplay();
                // Ensuite, tous les pions placés sont face visible
                else
                {
                    revealToken();
                    if (associatedToken.tokenHolder == null) GetComponent<Collider>().enabled = true;
                }
            }
        }
	}
    
    void manageTokenInfoDisplay()
    {
        // Si c'est au tour du joueur qui possède ce token, afficher le token face visible
        if (gManager.isActivePlayer(associatedToken.affiliationJoueur))
        {
            if (!gManager.startTurn || gManager.onlineGameInterface.isOnlineOpponent(associatedToken.indexToken)) coverToken();
            else revealToken();
        }
        // Sinon, l'afficher face cachée
        else coverToken();
    }

    public void tokenPutOnBoard () {
		changeSortingLayer("TokensOnBoard");
        highlightToken(false);
        StartUnselectAnimation();
    }
	
	// Rendre le token visible
	public void displayToken () {
        if (associatedToken.tokenHolder != null)
        {
            getTokenIcon().GetComponent<SpriteRenderer>().enabled = true;
        }
        else
        {
            Transform heldToken = null;
            if (GetComponent<CharacterBehavior>() != null)
            {
                if (GetComponent<CharacterBehavior>().wounded) getTokenIcon().GetComponent<SpriteRenderer>().color = Color.red;
                if (GetComponent<CharacterBehavior>().tokenTranporte != null)
                {
                    heldToken = GetComponent<CharacterBehavior>().tokenTranporte.transform;
                    heldToken.parent = null;
                }
            }

            SpriteRenderer[] sRenderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer sR in sRenderers)
            {
                sR.enabled = true;
            }

            if (heldToken != null) heldToken.parent = transform;
        }
	}
	
	// Faire disparaitre le token
	public void hideToken () {
		SpriteRenderer[] sRenderers = GetComponentsInChildren<SpriteRenderer>();
		foreach (SpriteRenderer sR in sRenderers) {
			sR.enabled = false;
		}
	}

    List<SpriteRenderer> getAllSpriteRenderers()
    {
        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        List<SpriteRenderer> sRenderers = new List<SpriteRenderer>();
        sRenderers.AddRange(spriteRenderers);
        if (!sRenderers.Contains(tokenHighlight.GetComponent<SpriteRenderer>())) sRenderers.Add(tokenHighlight.GetComponent<SpriteRenderer>());
        return sRenderers;
    }
	
	public void fadeOutToken ()
    {
        StartCoroutine(fadeOutToken(getAllSpriteRenderers(), 0.01f));
	}

    public void fadeInToken()
    {
        tokenHidden = false;
        displayToken();
        StartCoroutine(fadeInToken(getAllSpriteRenderers(), Time.time, 0.4f));
    }

    IEnumerator fadeInToken(List<SpriteRenderer> sRenderers, float startTime, float duration)
    {
        float valueProgression = (Time.time - startTime) / duration;
        foreach (SpriteRenderer sR in sRenderers)
        {
            sR.color = Color.Lerp(transparentSpriteColor, Color.white, valueProgression);
        }
        yield return new WaitForSeconds(0.01f);
        if (Time.time - startTime < duration) StartCoroutine(fadeInToken(sRenderers, startTime, duration));
        else
        {
            foreach (SpriteRenderer sR in sRenderers)
            {
                sR.color = Color.white;
            }
        }
    }
	
	IEnumerator fadeOutToken (List<SpriteRenderer> sRenderers, float timeInterval) {
		yield return new WaitForSeconds(timeInterval);
		foreach (SpriteRenderer sR in sRenderers) {
			sR.color = Color.Lerp(sR.color, transparentSpriteColor, 0.1f);
		}
		if (sRenderers[0].color != transparentSpriteColor) StartCoroutine(fadeOutToken(sRenderers, 0.01f));
	}
	
	// Mettre le token face cachée
	public void coverToken () {
		sRenderer.sortingOrder = orderInLayerHidden;
		highlightToken(false);
	}
	
	// Mettre le token face visible
	public void revealToken () {
		sRenderer.sortingOrder = orderInLayerVisible;
	}
	
	// Activer/Désactiver le halo du token
	public void highlightToken (bool newValue) {
		tokenHighlight.SetActive(newValue);
	}
	
	// Changer la position du token dans l'ordre d'affichage
	public void changeSortingLayer (string newLayerName) {
		sRenderer.sortingLayerName = newLayerName;
		SpriteRenderer[] sRenderers = GetComponentsInChildren<SpriteRenderer>();
		foreach (SpriteRenderer sR in sRenderers) {
			if (sR.sortingLayerName == "HUD") sR.sortingLayerName = "TokensOverlay";
			if (sR.sortingLayerName != "TokensOverlay") sR.sortingLayerName = newLayerName;
		}
	}

	public void fadeInTokenIcon () {
		SpriteRenderer tokenIcon = getTokenIcon().GetComponent<SpriteRenderer>();
		tokenIcon.color = transparentSpriteColor;
		tokenIcon.enabled = true;
		StartCoroutine(fadeInTokenIcon(tokenIcon, 0.01f));
	}

	IEnumerator fadeInTokenIcon (SpriteRenderer tokenIcon, float timeInterval) {
		yield return new WaitForSeconds(timeInterval);
		tokenIcon.color = Color.Lerp(tokenIcon.color, Color.white, 0.1f);
        if (tokenIcon.color != Color.white) StartCoroutine(fadeInTokenIcon(tokenIcon, timeInterval));
	}
	
	public void fadeOutTokenIcon () {
		SpriteRenderer tokenIcon = getTokenIcon().GetComponent<SpriteRenderer>();
		tokenIcon.color = transparentSpriteColor;
		StartCoroutine(fadeOutTokenIcon(tokenIcon, 0.01f));
	}
	
	IEnumerator fadeOutTokenIcon (SpriteRenderer tokenIcon, float timeInterval) {
		yield return new WaitForSeconds(timeInterval);
		tokenIcon.color = Color.Lerp(tokenIcon.color, transparentSpriteColor, 0.1f);
        if (tokenIcon.color != transparentSpriteColor) StartCoroutine(fadeOutTokenIcon(tokenIcon, timeInterval));
	}

	// Renvoit le sprite de l'image du token (item ou tete du personnage)
	public virtual GameObject getTokenIcon () {
		for (int i=0 ; i < tokenInfo.transform.childCount ; i++) {
			Transform child = tokenInfo.transform.GetChild(i);
			if (!child.name.Contains("Combat") && !child.name.Contains("Movement"))
				return tokenInfo.transform.GetChild(i).gameObject;
		}
		Debug.LogError("Token IHM, getTokenIcon: Aucun game object correspondant trouvé");
		return null;
	}
	
	// Renvoit le sprite de l'image du token (item ou tete du personnage)
	public virtual Sprite getTokenNameSprite () {
		Transform container = transform.Find("Token_Name");
		for (int i=0 ; i < container.childCount ; i++) {
			Transform child = container.GetChild(i);
			if (child.gameObject.activeSelf) return child.GetComponent<SpriteRenderer>().sprite;
		}
		Debug.LogError("Token IHM, getTokenName: Aucun game object correspondant trouvé");
		return null;
	}
	
	// Renvoit sous forme de texture le sprite de l'image du token (item ou tete du personnage)
	public Texture2D getTokenAppearance () {
		Sprite result = couleurDos;
		for (int i=0 ; i < tokenInfo.transform.childCount ; i++) {
			Transform child = tokenInfo.transform.GetChild(i);
			if (!child.name.Contains("Combat") && !child.name.Contains("Movement"))
				result = tokenInfo.transform.GetChild(i).GetComponent<SpriteRenderer>().sprite;
		}
		return result.texture;
	}

    // Renvoie TRUE si le joueur actif peut bouger le token a cet instant
    public bool canMoveToken() // used for IHM
    {
        return gManager.startTurn && !gManager.onlineGameInterface.isOnlineOpponent(gManager.activePlayer.index) && canMoveToken(gManager.activePlayer);
    }

    // Tells if the given player would be allowed to move this pawn if it was his turn to play
	public bool canMoveToken(PlayerBehavior player) {
        switch (gManager.gameMacroState)
        {
            case GameProgression.GameStart:
                return (!associatedToken.tokenPlace && player.index == associatedToken.getOwnerIndex() && gameObject.GetComponent<Item>() == null);
            case GameProgression.TokenPlacement:
                return (!associatedToken.tokenPlace && player.index == associatedToken.getOwnerIndex());
            case GameProgression.Playing:
                // Placement des pions à la découverte d'une salle
                return (!associatedToken.tokenPlace && (gameObject.GetComponent<Item>() == null || player.index != associatedToken.getOwnerIndex()));
        }
        return false;
    }

    protected bool validInput()
    {
        return (gManager.playerInteractionAvailable() && !gManager.rotationEnCours && !gManager.combatEnCours);
    }

    public void placeTokenOnCell(CaseBehavior cell)
    {
        transform.position = new Vector3(cell.transform.position.x, cell.transform.position.y, 0);
        applyNewScale(NORMAL_SCALE_VALUE);
        tokenHighlight.GetComponent<SpriteRenderer>().sprite = standardHighlight;
        associatedToken.placeTokenOnCell(cell);
    }

    void OnMouseDown()
    {
        if (validInput()) OnInputDown();
    }

    public virtual void OnInputDown()
    {
        // Si on peut déplacer le token
        if (canMoveToken())
        {
            // On bloque le déplacement de la caméra
            gManager.cameraMovementLocked = true;
            // On affiche le token au-dessus de tout
            changeSortingLayer("Selection");
            // Si le token était déjà placé
            if (associatedToken.cibleToken != null)
            {
                // Si le placement du token n'est pas déjà validé, on le retire de son emplacement
                if (!associatedToken.cibleToken.locked)
                {
                    associatedToken.cibleToken.tokenAssociated = null;
                    associatedToken.cibleToken = null;
                    //Screen.showCursor = false;
                }
            }
            //else Screen.showCursor = false;
        }
    }
	
	// Quand on maintient le clic sur le token et qu'on veut le déplacer
	void OnMouseDrag() {
		// Si le token n'est pas encore placé sur le plateau
		if (!associatedToken.tokenPlace) {
			// Si on peut déplacer le token
			if (canMoveToken()) {
				// Si le token était déjà placé
				if (associatedToken.cibleToken != null) {
					// Si le placement du token n'est pas déjà validé, on le déplace
					if (!associatedToken.cibleToken.locked) dragToken();
				}
				else dragToken();
			}
		}
		else if (bAllowInGameDrag) {
			// Si on peut déplacer le token
			if (associatedToken.selected) {
				// On bloque le déplacement de la caméra
				gManager.cameraMovementLocked = true;
				ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				// Détecter la position de la souris / du doigt et téléporter le token à cet endroit
				if (Physics.Raycast(ray, out hit, 100)) {
					transform.position = new Vector3(hit.point.x, hit.point.y, -0.2f);
					Debug.DrawRay(Camera.main.transform.position, new Vector3(hit.point.x, hit.point.y, 0));
					//Debug.DrawRay(Camera.main.transform.position, hit.point);
				}
			}
			if (gManager.actionCharacter == gameObject && Time.time - selectionTimer > SIMPLE_CLICK_DURATION) gManager.longTouch = true;
		}
	}
	
	// Déplacer le token
	void dragToken () {
		ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		// Détecter la position de la souris / du doigt et téléporter le token à cet endroit
		if (Physics.Raycast(ray, out hit, 100)) {
			transform.position = new Vector3(hit.point.x, hit.point.y, -1);
			/*
			transform.position = new Vector3(hit.point.x, hit.point.y*1.8f, 0);
			sCollider.center = new Vector3(hit.point.x, hit.point.y, 0);
			*/
		}
		
		// Illuminer le token s'il se trouve à un endroit où il peut etre déposé
		bool highlightToken = false;
		float xCase, yCase;
		// Vérification que le token peut etre déposé
		for (int i=0 ; i < associatedToken.ciblesTokens.Count ; i++) {
			if (associatedToken.ciblesTokens[i].GetComponent<PlacementTokens>().tokenAssociated == null) {
				xCase = associatedToken.ciblesTokens[i].transform.position.x;
				yCase = associatedToken.ciblesTokens[i].transform.position.y;
				//Debug.Log(Mathf.Abs(transform.position.x) - Mathf.Abs(xCase));
				if (Mathf.Abs(transform.position.x - xCase) < distanceSnapInitial && Mathf.Abs(transform.position.y - yCase) < distanceSnapInitial) highlightToken = true;
			}
		}
		// Si le token peut etre déposé, l'illuminer
		if (highlightToken) {
			tokenHighlight.GetComponent<SpriteRenderer>().sprite = actionDoableHighlight;
			tokenHighlight.GetComponent<SpriteRenderer>().sortingLayerName = "HUD";
		}
		// Sinon, le mettre son halo normal
		else {
			tokenHighlight.GetComponent<SpriteRenderer>().sprite = standardHighlight;
			tokenHighlight.GetComponent<SpriteRenderer>().sortingLayerName = "TokensOverlay";
		}
	}

    void OnMouseUp()
    {
        if (validInput()) OnInputUp();
    }
	
	// Quand on relache le clic sur le token
    public virtual void OnInputUp() {
		// Si le token n'est pas encore placé sur le plateau
		if (!associatedToken.tokenPlace) {
			// Si on peut déplacer le token
			if (canMoveToken()) {
				// On affiche le token sur le meme plan que les autres tokens
				changeSortingLayer("Tokens");
				// Si le token était déjà placé
				if (associatedToken.cibleToken != null) {
					// Si le placement du token n'est pas déjà validé, on le place
					if (!associatedToken.cibleToken.locked) placeToken();
				}
				// Sinon placer le token
				else placeToken();
				//Screen.showCursor = true;
			}
		}
		else {
			// Si on peut déplacer le token, on le snappe sur la dernière case parcourue
			if (associatedToken.selected) {
				Vector3 snapPosition;
                if (moveTargets.Count > 0)
                {
                    snapPosition = moveTargets[moveTargets.Count - 1].transform.position;
                    if (moveTargets.Count > 1 || moveTargets[0].GetComponent<CibleDeplacement>().caseAssociated != associatedToken.caseActuelle || isInteractedWith) // hack
                    {
                        if (!gManager.deplacementEnCours)
                        {
                            gManager.deplacementEnCours = true;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("TokenIHM, OnMouseUp: Clearing moveTargets");
                        moveTargets.Clear();
                    }
				}
				else snapPosition = associatedToken.caseActuelle.transform.position;
				transform.position = new Vector3(snapPosition.x, snapPosition.y, -1);
			}
			gManager.longTouch = false;
		}
		// On débloque le déplacement de la caméra
        if (gManager.gameMacroState == GameProgression.Playing) gManager.cameraMovementLocked = false;
	}
	
	// Vérifie que l'on peut placer un token que l'on vient de lacher, si oui on place le token
	void placeToken () {
		float xCase, yCase;
		float distanceSnap = 0.1f;
        if (gManager.gameMacroState == GameProgression.GameStart) distanceSnap = distanceSnapInitial;
        else distanceSnap = distanceSnapStandard;
		// Pour chaque emplacement de token
		for (int i=0 ; i < associatedToken.ciblesTokens.Count ; i++) {
			// Si l'emplacement est disponible
			if (associatedToken.ciblesTokens[i].GetComponent<PlacementTokens>().tokenAssociated == null) {
				xCase = associatedToken.ciblesTokens[i].transform.position.x;
				yCase = associatedToken.ciblesTokens[i].transform.position.y;
				//Debug.Log(Mathf.Abs(transform.position.x) - Mathf.Abs(xCase));
				
				// Si le token est suffisamment près
                if (Mathf.Abs(transform.position.x - xCase) < distanceSnap && Mathf.Abs(transform.position.y - yCase) < distanceSnap)
                {
                    //highlightToken(false);
					tokenHighlight.GetComponent<SpriteRenderer>().sprite = standardHighlight;
					// On snappe le token sur la position de l'emplacement
					transform.position = new Vector3(xCase, yCase, 0);
					// On lie le token et l'emplacement
                    associatedToken.placeToken(associatedToken.ciblesTokens[i].GetComponent<PlacementTokens>());
                    if (gManager.gameMacroState != GameProgression.Playing) gManager.playSound(gManager.tokenPlaceSound[UnityEngine.Random.Range(0, gManager.tokenPlaceSound.Length)]);
				}
			}
		}
        returnTokenToWaitingSpot();
	}

    public void returnTokenToWaitingSpot()
    {
        if (associatedToken.cibleToken == null) StartCoroutine(moveTo(Time.time, 0.2f, transform.position, returnSpot));
    }

    IEnumerator moveTo(float startTime, float duration, Vector3 from, Vector3 to)
    {
        float valueProgression = (Time.time - startTime) / duration;
        transform.position = Vector3.Lerp(from, to, valueProgression);
        yield return new WaitForSeconds(0.01f);
        if (Time.time - startTime < duration) StartCoroutine(moveTo(startTime, duration, from, to));
        else transform.position = to;
    }

    public void refreshReturnSpot()
    {
        returnSpot = transform.position;
    }

	// Place le token sur une case donnée
	public void placeToken (GameObject cible) {
		// On place le token sur la position de l'emplacement
		transform.position = new Vector3(cible.transform.position.x, cible.transform.position.y, 0);
		// On lie le token et l'emplacement
		associatedToken.placeToken(cible.GetComponent<PlacementTokens>());
		if (!gManager.isActivePlayer(associatedToken.affiliationJoueur)) displayToken();
	}
	
	// Mettre à jour la position du token (généralement à la fin d'une rotation de salle)
	public virtual void refreshPosition () {
		bool refresh = false;
		if (transform.parent != null) {
			if (transform.parent.gameObject.GetComponent<CharacterBehavior>() == null) {
				refresh = true;
			}
		}
		else refresh = true;

        if (refresh)
        {
            if (associatedToken.caseActuelle != null)
                transform.position = new Vector3(associatedToken.caseActuelle.transform.position.x, associatedToken.caseActuelle.transform.position.y, 0);
            else if (associatedToken.cibleToken != null)
                transform.position = new Vector3(associatedToken.cibleToken.transform.position.x, associatedToken.cibleToken.transform.position.y, 0);
        }
	}
	
	void makeMovePath (GameObject nextTarget) {
		//Debug.Log("Dragging");
		// Si la cible faisait déjà partie de la liste
		if (moveTargets.Contains(nextTarget)) {
			// Retirer toutes les cibles situées après cette cible
			int targetIndex = moveTargets.IndexOf(nextTarget);
			while (targetIndex < moveTargets.Count - 1) {
				moveTargets[targetIndex+1].GetComponent<SpriteRenderer>().color = Color.white;
				moveTargets.Remove(moveTargets[targetIndex+1]);
				associatedToken.deplacementRestant++;
			}
		}
		else {
			bool addNewTarget = true;
			if (moveTargets.Count > 0) {
				if (Mathf.Abs((moveTargets[moveTargets.Count-1].GetComponent<CibleDeplacement>().nbDeplacementRestant - nextTarget.GetComponent<CibleDeplacement>().nbDeplacementRestant)) > 1) {
					addNewTarget = false;
				}
			}
			// Sinon, l'ajouter à la liste
			if (addNewTarget) {
				moveTargets.Add(nextTarget);
				associatedToken.deplacementRestant--;
			}
			// Si le personnage initié un déplacement
			if (moveTargets.Count > 0 && !cancelMovementPossible) {
				cancelMovementPossible = true;
				// Afficher un bouton permettant d'annuler le mouvement
				gManager.displayCancelButton = true;
			}
		}
	}
	
	public virtual void cancelMovement () {
		moveTargets.Clear();
		transform.position = new Vector3(associatedToken.caseActuelle.transform.position.x, associatedToken.caseActuelle.transform.position.y, 0);
		if (GetComponent<CharacterBehavior>() != null)
        {
            GetComponent<CharacterBehavior>().deselectCharacter();
            associatedToken.deplacementRestant = GetComponent<CharacterBehavior>().MOVE_VALUE;
        }
		gManager.displayCancelButton = false;
		gManager.actionPointCost = 0;
        cancelMovementPossible = false;
        StartUnselectAnimation();
	}

    void restoreScaleAfterJump()
    {
        iTween.ScaleTo(gameObject, iTween.Hash("scale", selectedScale, "time", 0.3f, "easetype", iTween.EaseType.easeInQuart));
    }

    public bool isInteractedWith { get { return previousCase != null; } }

    public void recordPreviousState()
    {
        if (!isInteractedWith) // already recorded
        {
            if (associatedToken.tokenHolder != null) previousPersonnageAssocie = associatedToken.tokenHolder.gameObject;
            else previousPersonnageAssocie = gManager.gameObject;
            previousCase = associatedToken.caseActuelle;
        }
    }

    public void clearPreviousState()
    {
        previousCase = null;
        previousPersonnageAssocie = null;
    }

    public void restorePreviousState()
    {
        if (isInteractedWith)
        {
            if (previousPersonnageAssocie != null && (previousPersonnageAssocie != associatedToken.tokenHolder)) // token holder has changed
            {
                if (associatedToken.tokenHolder != null) // current holder drops the token
                {
                    associatedToken.tokenHolder.GetComponent<CharacterBehaviorIHM>().deposerToken(false);
                }

                if (previousPersonnageAssocie == gManager.gameObject) // token was on the ground
                {
                    transform.position = new Vector3(previousCase.transform.position.x, previousCase.transform.position.y, 0);
                }
                else // token had a previous holder
                {
                    if (previousPersonnageAssocie.GetComponent<CharacterBehavior>().tokenTranporte != null)
                    {
                        previousPersonnageAssocie.GetComponent<CharacterBehaviorIHM>().deposerToken(false);
                    }
                    previousPersonnageAssocie.GetComponent<CharacterBehaviorIHM>().ramasserToken(gameObject, false);
                }
            }

            if (previousCase != null) // token cell has changed
            {
                associatedToken.caseActuelle = previousCase;
            }

            clearPreviousState();
        }
    }


    #region scale
    private const float coeffNotOnBoardScaleValue = 2.0f;
    private const float coeffScaleValue = 1.3f;
    private const float stdColliderRadius = 0.36f;
    private const float speedScale = 10.0f;

    public float NORMAL_SCALE_VALUE { get { return stdScaleValue; } }
    public float SELECTED_SCALE_VALUE { get { return stdScaleValue * coeffScaleValue; } }
    public float NOT_PLACED_SCALE_VALUE { get { return stdScaleValue * coeffNotOnBoardScaleValue; } }
    private float coeffFromScale(float scale) { return scale / stdScaleValue; }
    private float currentScale { get { return transform.localScale.x; } }

    static private Vector3 __scaleVectorFromValue__(float scale) { return new Vector3(scale, scale, 1); }
    public Vector3 notPlacedScale { get { return __scaleVectorFromValue__(NOT_PLACED_SCALE_VALUE); } } // used for jump animation
    public Vector3 selectedScale { get { return __scaleVectorFromValue__(SELECTED_SCALE_VALUE); } } // used for jump animation

    public bool isAtStandardScale() { return (transform.localScale.x == stdScaleValue); }
    public float currentScaleRatio() { return transform.localScale.x /stdScaleValue; }

    public void applyNewScale(float scaleValue)
    {
        transform.localScale = __scaleVectorFromValue__(scaleValue);
        sCollider.radius = stdColliderRadius * coeffFromScale(scaleValue);
    }

    void SetScaleAndCollider(float expectedScale)
    {
        if (!Mathf.Approximately(currentScale, expectedScale))
        {
            applyNewScale( Mathf.Lerp(currentScale, expectedScale, speedScale * Time.deltaTime) );
        }
        else
        {
            applyNewScale(expectedScale);
        }
    }

    public void forceEndScaleValue()
    {
        if (associatedToken.tokenHolder == null)
        {
            if (associatedToken.selected) applyNewScale(SELECTED_SCALE_VALUE);
            else applyNewScale(NORMAL_SCALE_VALUE);
        }
    }

    // Animation de sélection/désélection
    private IEnumerator tokenSelectionAnimation(float startTime, float duration, float startValue, float endValue, bool selectionState)
    {
        while (associatedToken.selected == selectionState)
        {
            if (Time.time - startTime < duration)
            {
                float valueProgression = (Time.time - startTime) / duration;
                applyNewScale( Mathf.Lerp(startValue, endValue, valueProgression) );
                yield return new WaitForSeconds(0.01f);
            }
            else
            {
                applyNewScale(endValue);
                break;
            }
        }
    }

    protected void StartSelectAnimation()
    {
        StartCoroutine(tokenSelectionAnimation(Time.time, 0.3f, currentScale, SELECTED_SCALE_VALUE, true));
    }

    protected void StartUnselectAnimation()
    {
        StartCoroutine(tokenSelectionAnimation(Time.time, 0.2f, currentScale, NORMAL_SCALE_VALUE, false));
    }
    #endregion
}
