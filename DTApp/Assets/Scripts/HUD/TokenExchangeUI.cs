using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class TokenExchangeUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler {

    public float SNAP_DISTANCE;
	public GameObject token;
	public GameObject originalPoint;
	public CaseBehavior currentCase;
	
	Ray ray;
	RaycastHit hit;
	GameManager gManager;

	// Use this for initialization
    void Start()
    {
        gManager = GameManager.gManager;
        SNAP_DISTANCE =  0.06f * Screen.height;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        gManager.cameraMovementLocked = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        dragExchangeToken();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        checkAndExecuteExchange();
    }
	
	void dragExchangeToken () {
        transform.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, originalPoint.transform.position.z);
	}

    void checkIfHoldingToken(GameObject token)
    {
        if (token.GetComponent<CharacterBehavior>() != null)
        {
            if (token.GetComponent<CharacterBehavior>().tokenTranporte != null) token.GetComponent<CharacterBehaviorIHM>().deposerToken();
        }
    }

    bool isInteractableSpot(GameObject spot) {
        return (spot.GetComponent<ExchangePointUI>().occupyingToken == null) || spot.GetComponent<ExchangePointUI>().occupyingToken.GetComponent<TokenExchangeUI>().enabled;
    }

    void checkAndExecuteExchange()
    {
        gManager.cameraMovementLocked = false;
		bool spotFound = false;
		GameObject newPoint = originalPoint;
		foreach (GameObject spot in gManager.exchangePoints) {
            if (isInteractableSpot(spot) && spot != originalPoint && Vector3.Distance(transform.position, spot.transform.position) < SNAP_DISTANCE) {
				newPoint = spot;
				spotFound = true;
				break;
			}
		}
		// Si le token n'est pas dirigé vers un emplacement différent, on le remet à son emplacement de départ
		if (!spotFound) {
			Debug.LogWarning("Not found");
			transform.position = originalPoint.transform.position;
		}
		// Sinon, on le déplace vers son nouvel emplacement
		else {
            CharacterBehaviorIHM charaIHM = gManager.actionCharacter.GetComponent<CharacterBehaviorIHM>();
			//if (!gManager.tokensInteractedWith.Contains(token)) gManager.tokensInteractedWith.Add(token);

			// Si un autre token se trouve déjà à cet endroit, échanger sa place avec celui-ci
            if (newPoint.GetComponent<ExchangePointUI>().isHoldingToken())
            {
				Debug.Log("Echange");
				TokenExchangeUI otherToken = newPoint.GetComponent<ExchangePointUI>().occupyingToken.GetComponent<TokenExchangeUI>();
				otherToken.originalPoint = originalPoint;
				otherToken.transform.position = originalPoint.transform.position;
				originalPoint.GetComponent<ExchangePointUI>().occupyingToken = otherToken.gameObject;
				//if (!gManager.tokensInteractedWith.Contains(otherToken.token)) gManager.tokensInteractedWith.Add(otherToken.token);

				// Le token est déplacé vers l'emplacement de token transporté
                if (newPoint.GetComponent<ExchangePointUI>().isCarriedTokenSpot())
                {
                    charaIHM.deposerToken();
                    if (token.GetComponent<Token>().tokenHolder != null)
                    {
                        CharacterBehavior characterHoldingThisToken = token.GetComponent<Token>().tokenHolder;
                        characterHoldingThisToken.GetComponent<CharacterBehaviorIHM>().deposerToken();
                        characterHoldingThisToken.GetComponent<CharacterBehaviorIHM>().ramasserToken(otherToken.token);
                    }
                    else
                    {
                        checkIfHoldingToken(token);
                        otherToken.token.GetComponent<Token>().caseActuelle = currentCase.gameObject;
                    }
                    // Jouer le son de l'objet qui est ramassé
                    GameManager.gManager.playSound(token.GetComponent<TokenIHM>().tokenGetSound);
                    charaIHM.ramasserToken(token);
                }
				// Le token est déplacé vers l'emplacement de sol
                else if (originalPoint.GetComponent<ExchangePointUI>().isCarriedTokenSpot())
                {
                    // Jouer le son de l'objet qui est déposé
                    GameManager.gManager.playSound(token.GetComponent<TokenIHM>().tokenDropSound);
                    charaIHM.deposerToken();
                    if (otherToken.token.GetComponent<Token>().tokenHolder != null)
                    {
                        CharacterBehavior characterHoldingOtherToken = otherToken.token.GetComponent<Token>().tokenHolder;
                        characterHoldingOtherToken.GetComponent<CharacterBehaviorIHM>().deposerToken();
                        characterHoldingOtherToken.GetComponent<CharacterBehaviorIHM>().ramasserToken(token);
                    }
                    else
                    {
                        checkIfHoldingToken(otherToken.token);
                        token.GetComponent<Token>().caseActuelle = currentCase.gameObject;
                    }
                    charaIHM.ramasserToken(otherToken.token);
                }
			}
			// Sinon
			else {
				Debug.Log("Transfert");
                // Déposer le token au sol et si un autre personnage est présent, lui associer
                if (originalPoint.GetComponent<ExchangePointUI>().isCarriedTokenSpot())
                {
                    // find if another character can hold the dropped item
                    CharacterBehavior receiverCharacter = currentCase.getOtherMainCharacter(token.GetComponent<Token>().tokenHolder);
                    // Jouer le son de l'objet qui est déposé
                    GameManager.gManager.playSound(token.GetComponent<TokenIHM>().tokenDropSound);
                    charaIHM.deposerToken();
                    //Debug.Log(receiverCharacter);
                    if (receiverCharacter != null)
                    {
                        receiverCharacter.GetComponent<CharacterBehaviorIHM>().ramasserToken(token);
                    }
                    token.GetComponent<Token>().caseActuelle = currentCase.gameObject;
                }
                // Récupérer le token
                else if (newPoint.GetComponent<ExchangePointUI>().isCarriedTokenSpot())
                {
                    // Si le token était déjà tenu par un autre personnage, les dissocier
                    if (token.GetComponent<Token>().tokenHolder != null)
                    {
                        token.GetComponent<Token>().tokenHolder.GetComponent<CharacterBehaviorIHM>().deposerToken();
                    }
                    // Sinon, si le token à ramasser est un personnage qui porte un objet, déposer son objet
                    else checkIfHoldingToken(token);
                    // Jouer le son de l'objet qui est ramassé
                    GameManager.gManager.playSound(token.GetComponent<TokenIHM>().tokenGetSound);
                    charaIHM.ramasserToken(token);
                }
                else Debug.LogError("TokenExchangeUI, checkAndExecuteExchange: Token non affilié");
                originalPoint.GetComponent<ExchangePointUI>().occupyingToken = null;
			}
			originalPoint = newPoint;
			transform.position = newPoint.transform.position;
			newPoint.GetComponent<ExchangePointUI>().occupyingToken = gameObject;

            // Recalculer le déplacement du personnage
            gManager.actionCharacter.GetComponent<CharacterBehaviorIHM>().recomputePossibleActionsIHM(false, false);
        }
	}
}
