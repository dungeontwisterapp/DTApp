using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class HiddenTileBehaviorIHM : MonoBehaviour, IPointerDownHandler {

	HiddenTileBehavior associatedBackTile;
	GameManager gManager;
	SpriteRenderer highlight;
	
	void Awake () {
		gManager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameManager>();
		highlight = transform.GetChild(0).GetComponent<SpriteRenderer>();
		associatedBackTile = GetComponent<HiddenTileBehavior>();
		if (associatedBackTile == null) {
			Debug.LogError("HiddenTileBehaviorIHM, Awake: Le script HiddenTileBehavior n'a pas été trouvé sur le même Game Object");
			this.enabled = false;
		}
	}
	
	// Update is called once per frame
    void Update()
    {
        if (!gManager.freezeDisplay)
        {
            // Modifier la couleur quand on peut retourner la salle, avec une couleur différente lorsque l'on passe la souris sur la salle
            if (isSelectionnable())
            {
                highlight.enabled = true;
                GetComponent<Collider>().enabled = true;
            }
            else
            {
                highlight.enabled = false;
                GetComponent<Collider>().enabled = false;
            }
        }
	}

    public void OnPointerDown(PointerEventData e)
    {
        // Si on peut ouvrir la salle
        if (isSelectionnable())
        {
            gManager.onlineGameInterface.RecordReveal(associatedBackTile.tileAssociated.GetComponent<TileBehavior>());
            if (!gManager.onlineGame)
            {
                openRoom();
            }
            else
            {
                Debug.Assert(!gManager.onlineGameInterface.isOnlineOpponent(gManager.activePlayer.index));
                gManager.progression.BlockInteractionsUntilBgaAnswer(true);
            }
        }
    }

    public Vector3 getWaitingToBePlacedTokenPosition(Vector3 tokenPosition)
    {
        // depart the token vertically from the center so it will be on top (or bottom) of its room
        float verticalShift = 2.2f;
        float currentRatio = (float)Screen.width / (float)Screen.height;
        float expectedRatio = 16.0f / 9.0f;
        if (currentRatio < expectedRatio)
        {
            float multiplier = Mathf.Sqrt(expectedRatio / currentRatio);
            verticalShift *= multiplier;
        }
        Vector3 waitPosition = new Vector3(tokenPosition.x, transform.position.y * verticalShift, tokenPosition.z);

        // depart tokens horizontaly if there are more than 2 on the same room
        if (transform.childCount - 1 > 2)
        {
            waitPosition.x += (waitPosition.x - transform.position.x);
        }
        return waitPosition;
    }

    public void openRoom()
    {
        //Debug.LogError("Open Discovered Room");
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).name != "Highlight")
            {
                Transform token = transform.GetChild(i).GetComponent<PlacementTokens>().tokenAssociated.transform;
                Vector3 waitPosition = getWaitingToBePlacedTokenPosition(token.position);
                StartCoroutine(moveTokensInPlace(token, waitPosition));
            }
        }

        List<CaseBehavior> spacesAvailable = new List<CaseBehavior>();
        bool someTokensOnTile = (transform.childCount > 1);
        if (someTokensOnTile)
        {
            gManager.actionCharacter.GetComponent<CharacterBehaviorIHM>().endDeplacementIHMOnly();
            // WARNING: No call to endDeplacement because actionDone should not be called yet. But only place doing so.

            spacesAvailable.AddRange(revealTile(gManager.actionCharacter, associatedBackTile.tileAssociated));
            instanciateTileTargets(spacesAvailable);
            // Mise à jour des cibles disponibles pour les tokens à placer
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).name != "Highlight")
                {
                    Token t = transform.GetChild(i).GetComponent<PlacementTokens>().tokenAssociated.GetComponent<Token>();
                    t.ciblesTokens.Clear();
                    t.ciblesTokens.AddRange(GameObject.FindGameObjectsWithTag("TileRevealedTarget"));
                }
            }
        }
        else
        {
            gManager.actionCharacter.GetComponent<CharacterBehaviorIHM>().endDeplacementIHM();
            associatedBackTile.tileAssociated.GetComponent<TileBehavior>().hidden = false;
        }

        GetComponent<Collider>().enabled = false;
        iTween.ColorTo(gameObject, iTween.Hash("color", Color.black, "time", 0.6f, "oncomplete", "fadeOutDisplay"));
        SpriteRenderer[] anciennesCibles = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sR in anciennesCibles)
        {
            if (sR.gameObject != gameObject) sR.enabled = false;
        }
        if (spacesAvailable.Count > 0)
        {
            gManager.placerTokens = true;
            if (Camera.main.GetComponent<ZoomControl>().isActiveAndEnabled) Camera.main.SendMessage("dezoom");
        }
    }

    List<CaseBehavior> revealTile(GameObject actionCharacter, GameObject targetTile)
    {
        gManager.actionCharacter = actionCharacter;
        List<CaseBehavior> casesAvailable = new List<CaseBehavior>();
        GameObject[] tileBacks = GameObject.FindGameObjectsWithTag("HiddenTile");
        foreach (GameObject tileBack in tileBacks)
        {
            if (tileBack.GetComponent<HiddenTileBehavior>().tileAssociated == targetTile)
            {
                casesAvailable = tileBack.GetComponent<HiddenTileBehavior>().revealTile();
                break;
            }
        }
        return casesAvailable;
    }

    public void instanciateTileTargets(List<CaseBehavior> spacesAvailable)
    {
        // Pour chaque case valide de la salle, créer une cible pour y poser les tokens qui sont associés à la salle
        foreach (CaseBehavior space in spacesAvailable)
        {
            GameObject temp = (GameObject)Instantiate(associatedBackTile.cibleToken, new Vector3(space.transform.position.x, space.transform.position.y, 0.1f), associatedBackTile.cibleToken.transform.rotation);
            temp.tag = "TileRevealedTarget";
            temp.GetComponent<PlacementTokens>().caseActuelle = space;
        }
    }

    void fadeOutDisplay()
    {
        iTween.ColorTo(gameObject, iTween.Hash("color", new Color(0, 0, 0, 0), "time", 0.4f, "oncomplete", "disableDisplay"));
    }

    public void disableDisplay()
    {
        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sR in sprites)
        {
            sR.enabled = false;
        }
    }

	// Si on peut ouvrir la salle ou pas
	bool isSelectionnable () {
        return (gManager.selectionEnCours && !gManager.deplacementEnCours && !gManager.rotationEnCours && !gManager.usingSpecialAbility && associatedBackTile.tileAssociated.GetComponent<TileBehavior>().canShow);
	}

	IEnumerator moveTokensInPlace (Transform token, Vector3 waitPosition) {
		yield return new WaitForSeconds(0.01f);
		token.position = Vector3.MoveTowards(token.position, waitPosition, 0.3f);
        if (token.position != waitPosition) StartCoroutine(moveTokensInPlace(token, waitPosition));
        else token.GetComponent<TokenIHM>().refreshReturnSpot();
    }

    public void resetAppearance()
    {
        iTween.ColorTo(gameObject, iTween.Hash("color", Color.white, "time", 0.01f));
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).name != "Highlight")
            {
                transform.GetChild(i).GetComponent<SpriteRenderer>().enabled = true;
            }
        }
    }

    public PlacementTokens getPlacementSpot(int index) { return associatedBackTile.getPlacementSpot(index); }
}
