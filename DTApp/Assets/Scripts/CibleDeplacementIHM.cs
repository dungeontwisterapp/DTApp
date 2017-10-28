using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class CibleDeplacementIHM : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
	
	CibleDeplacement associatedCible;
    SpriteRenderer sRenderer;
	Transform characterPassingBy;
	GameObject specialIcon;
	GameObject jumpsRemainingFeedback;

    float activationDistance = 0.2f;
    float timerDoubleInput = -1;
    const float DOUBLE_INPUT_MAX_INTERVAL = 0.3f;

    bool enemyOnCell = false;
    bool characterHovering = false;
    bool firstInput = true;
    bool secondInput = false;
    bool combatInitiated = false;

    public bool adjacentFighter = false;
    public bool wallwalkTarget = false;
    public bool jumpingTarget = false;

    GameManager gManager;
	
	void Start () {
		associatedCible = GetComponent<CibleDeplacement>();
		if (associatedCible == null) {
			Debug.LogError("CibleDeplacementIHM, Awake: Le script CibleDeplacement n'a pas été trouvé sur le même Game Object");
			this.enabled = false;
		}
        sRenderer = GetComponent<SpriteRenderer>();
        gManager = GameManager.gManager;
	}

	void Update () {
		if (characterHovering && gManager.longTouch) {
			if (Mathf.Abs(transform.position.x - characterPassingBy.position.x) < activationDistance && Mathf.Abs(transform.position.y - characterPassingBy.position.y) < activationDistance) {
				if (characterPassingBy.GetComponent<Token>().deplacementRestant > 0)
                {
					characterPassingBy.SendMessage("makeMovePath", gameObject);
                    sRenderer.color = Color.green;
                    if (associatedCible.caseAssociated.GetComponent<CaseBehavior>().caseDeDepart) gManager.resetPartialMoveFlags();
				}
			}
			//else sRenderer.color = Color.white;
		}
	}
	
	public void displayTarget () {
        //Debug.Log(associatedCible.caseAssociated.name);
		sRenderer.color = Color.white;
		CharacterBehavior character = associatedCible.caseAssociated.GetComponent<CaseBehavior>().getMainCharacter();
		if (character != null) {
            if (!gManager.isActivePlayer(character.affiliationJoueur))
            {
                if (!character.freshlyWounded)
                {
                    sRenderer.color = Color.red;
                    enemyOnCell = true;
                }
			}
		}
		transform.position = new Vector3(transform.position.x, transform.position.y, -0.5f);
		if (jumpingTarget) {
			if (specialIcon == null) {
				specialIcon = (GameObject) Instantiate(gManager.jumpIcon, transform.position, gManager.jumpIcon.transform.rotation);
				specialIcon.transform.parent = transform;
			}
			if (jumpsRemainingFeedback == null) {
                //Vector3 screenPoint = Camera.main.WorldToScreenPoint(transform.position);
                //Vector2 screenPosition = new Vector2(screenPoint.x/Screen.width, screenPoint.y/Screen.height);
                //jumpsRemainingFeedback = (GameObject)Instantiate(gManager.jumpsRemainingPrefab, screenPosition, gManager.jumpsRemainingPrefab.transform.rotation);
                jumpsRemainingFeedback = (GameObject)Instantiate(gManager.jumpsRemainingPrefab, transform.position, gManager.jumpsRemainingPrefab.transform.rotation);
                jumpsRemainingFeedback.GetComponent<MeshRenderer>().sortingLayerName = "TokensOverlay";
				jumpsRemainingFeedback.transform.parent = transform;
                //jumpsRemainingFeedback.GetComponent<GUIText>().fontSize = Screen.height/25;
                //jumpsRemainingFeedback.GetComponent<GUIText>().color = Color.black;
                //jumpsRemainingFeedback.GetComponent<GUIText>().text = "x" + associatedCible.tokenAssociated.GetComponent<Token>().affiliationJoueur.GetComponent<PlayerBehavior>().nbSauts;
                jumpsRemainingFeedback.GetComponent<TextMesh>().text = "x" + associatedCible.tokenAssociated.GetComponent<Token>().affiliationJoueur.GetComponent<PlayerBehavior>().nbSauts;
			}
		}
		if (wallwalkTarget) {
			if (specialIcon == null) {
				specialIcon = (GameObject) Instantiate(gManager.wallwalkIconPrefab, transform.position, gManager.wallwalkIconPrefab.transform.rotation);
				specialIcon.transform.parent = transform;
			}
		}
		GetComponent<Renderer>().enabled = true;
		GetComponent<Collider>().enabled = true;
	}

    // Réaction à un clic / appui sur la cible
    public void OnPointerDown(PointerEventData e)
    {
        if (gManager.playerInteractionAvailable())
        {
            if (!combatInitiated)
            {
                if (associatedCible.caseAssociated == null) Debug.LogError("Cible Deplacement IHM, OnMouseDown: caseAssociated non initialisé");
                if (!enemyOnCell) moveToken();
                else
                {
                    if (firstInput)
                    {
                        firstInput = false;
                        timerDoubleInput = Time.time;
                        Invoke("moveTokenIfNoAdditionalInput", DOUBLE_INPUT_MAX_INTERVAL);
                    }
                    else secondInput = true;
                }
            }
        }
    }

    void moveTokenIfNoAdditionalInput()
    {
        if (!secondInput && !combatInitiated)
        {
            firstInput = true;
            moveToken();
        }
    }

    void moveToken()
    {
        if (associatedCible.tokenAssociated != null)
        {
            // Calculer le chemin entre la case sélectionnée et la case où se trouve le personnage
            if (associatedCible.tokenAssociated.GetComponent<Token>().caseActuelle != associatedCible.caseAssociated || gManager.deplacementEnCours)
            {
                if (gManager.isInformationAndExchangeUIOpen()) gManager.closeInformationAndExchangeUI();
                //Debug.Log("Do pathfinding, deplacementRestant = "+associatedCible.tokenAssociated.GetComponent<Token>().deplacementRestant);
                moveSelectedTokenToTarget();
                gManager.deplacementEnCours = true;
                gManager.displayCancelButton = true;
            }
        }
        else Debug.LogError("Cible Deplacement IHM: tokenAssociated non initialisé");
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (gManager.playerInteractionAvailable())
        {
            if (adjacentFighter && !combatInitiated)
            {
                if (secondInput && Time.time - timerDoubleInput < DOUBLE_INPUT_MAX_INTERVAL)
                {
                    combatInitiated = true;
                    Invoke("resetInputData", DOUBLE_INPUT_MAX_INTERVAL);
                    CharacterBehavior combatTarget = associatedCible.caseAssociated.GetComponent<CaseBehavior>().getMainCharacter();
                    if (gManager.adversaireAdjacent &&
                        gManager.actionCharacter.GetComponent<Token>().affiliationJoueur != combatTarget.affiliationJoueur
                        && !gManager.usingSpecialAbility)
                        gManager.combatManager.combat(combatTarget.gameObject);
                }
            }
        }
    }

    void resetInputData()
    {
        firstInput = true;
        secondInput = false;
        combatInitiated = false;
    }

    public void moveSelectedTokenToTarget ()
    {
        ActionType type = jumpingTarget ? ActionType.JUMP : (wallwalkTarget ? ActionType.WALLWALK : ActionType.WALK);
        bool success = gManager.actionCharacter.GetComponent<CharacterBehavior>().MoveToTarget(type, associatedCible.caseAssociated.GetComponent<CaseBehavior>());

        if (success && wallwalkTarget) // display sound for wallwalker
        {
            gManager.playSound(associatedCible.tokenAssociated.GetComponent<CharacterBehaviorIHM>().abilitySound);
            //TokenIHM t = associatedCible.tokenAssociated.GetComponent<TokenIHM>();
        }
        //else playFootstepsSequence();
    }

	public void pastMovePossibility () {
		//Debug.Log("Shroud past move possibilities");
		if (jumpingTarget) {
			jumpsRemainingFeedback.SetActive(false);
			specialIcon.SetActive(false);
		}
		if (wallwalkTarget) {
			specialIcon.SetActive(false);
		}
		characterHovering = false;
		GetComponent<Collider>().enabled = false;
		sRenderer.color = new Color(1, 1, 1, 0.4f);
	}
	
	public void resetMovePossibility () {
        associatedCible.resetMovePossibility();
        transform.position = new Vector3(transform.position.x, transform.position.y, 0);
		GetComponent<Collider>().enabled = false;
		GetComponent<Renderer>().enabled = false;
		enemyOnCell = false;
        adjacentFighter = false;
        characterHovering = false;
		if (jumpingTarget) {
			jumpingTarget = false;
			Destroy(jumpsRemainingFeedback);
			Destroy(specialIcon);
		}
		if (wallwalkTarget) {
			wallwalkTarget = false;
			Destroy(specialIcon);
		}
	}

	void OnTriggerEnter (Collider character) {
        //if (!characterHovering) character.GetComponent<CharacterBehaviorIHM>().playCharacterFootstep();
		characterHovering = true;
		characterPassingBy = character.transform;
	}
	
	void OnTriggerExit () {
		characterHovering = false;
		characterPassingBy = null;
	}
    
    void playFootstepsSequence()
    {
        int nbSteps = associatedCible.tokenAssociated.GetComponent<Token>().deplacementRestant - associatedCible.nbDeplacementRestant;
        switch (nbSteps)
        {
            case 1:
                playFootstep();
                Invoke("playFootstep", 0.15f);
                break;
            case 2:
                playFootstep();
                Invoke("playFootstep", 0.14f);
                Invoke("playFootstep", 0.29f);
                break;
            case 3:
                Invoke("playFootstep", 0.1f);
                Invoke("playFootstep", 0.25f);
                Invoke("playFootstep", 0.4f);
                break;
            case 4:
                Invoke("playFootstep", 0.1f);
                Invoke("playFootstep", 0.25f);
                Invoke("playFootstep", 0.4f);
                Invoke("playFootstep", 0.55f);
                break;
            case 5:
                Invoke("playFootstep", 0.1f);
                Invoke("playFootstep", 0.25f);
                Invoke("playFootstep", 0.4f);
                Invoke("playFootstep", 0.55f);
                Invoke("playFootstep", 0.7f);
                break;
            default: Debug.LogError("CibleDeplacementIHM, playFootstepsSequence: Le nombre de cases de déplacement est invalide");
                break;
        }
    }

    void playFootstep()
    {
        associatedCible.tokenAssociated.GetComponent<CharacterBehaviorIHM>().playCharacterFootstep();
    }

}
