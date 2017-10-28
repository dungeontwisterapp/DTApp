using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CombatCards : MonoBehaviour {

    ChooseCombatCardScreen thisScreen;
    RectTransform rTransform;
    [HideInInspector]
    public Image image;
    public Sprite usedCardSprite;
	// Bonus de combat de la carte
	public int combatValue = 0;
	// Affiliation de la carte à un joueur
	public int indexCard;
    public bool doublon = false;

	bool selectionable = false;
	bool cardChosen = false;

	GameManager gManager;
	
	// Use this for initialization
	void Awake () {
        gManager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameManager>();
        thisScreen = transform.parent.parent.GetComponent<ChooseCombatCardScreen>();

        image = GetComponent<Image>();
        rTransform = GetComponent<RectTransform>();

        image.color = Color.white;
    }

    public void launchFadeIn()
    {
        StartCoroutine(fadeIn());
    }

    // Fonction exécutée par toutes les cartes une fois que l'une d'entre elles a été sélectionnée
    public void launchFadeOut()
    {
        StartCoroutine(fadeOut(0.3f));
    }

    // Réaction à un clic / appui sur la carte
    public void OnMouseDown() {
		// Si la carte a terminé son animation d'apparition et qu'aucune autre carte n'a été choisie
        if (selectionable)
        {
            cardSelection();
        }
	}

    public void cardSelection()
    {
        if (thisScreen != null) thisScreen.combatCardChosen(gameObject);
        else
        {
            thisScreen = transform.parent.parent.GetComponent<ChooseCombatCardScreen>();
            gManager = GameManager.gManager;
        }
        cardChosen = true;
        // Mettre un effet visuel sympa de sélection
        RectTransform targetTransform = thisScreen.leftCard.GetComponent<RectTransform>();
        if (gManager.activePlayer.index != 0) targetTransform = thisScreen.rightCard.GetComponent<RectTransform>();
        if (gManager.playerInteractionAvailable())
        {
            StartCoroutine(goToChoosenCardSpace(Time.time, 0.6f, transform.position, targetTransform.position));
            StartCoroutine(changeSizeDelta(Time.time, 0.6f, rTransform.sizeDelta, targetTransform.sizeDelta));
            transform.parent.BroadcastMessage("launchFadeOut");
        }
        else
        {
            rTransform.sizeDelta = targetTransform.sizeDelta;
            image.color = Color.white;
            thisScreen.rightCard.GetComponent<UISelectedCombatCard>().setSelectedCardSprite(image.sprite);
            image.sprite = targetTransform.GetComponent<Image>().sprite;
            StartCoroutine(goToChoosenCardSpace(Time.time, 0.6f, new Vector3(targetTransform.position.x, transform.position.y, transform.position.z), targetTransform.position));
        }
        withdrawThisCardFromPlayerHand(indexCard);
        StartCoroutine(removeCards(3.0f));
    }

    private void withdrawThisCardFromPlayerHand(int playerIndex)
    {
        GameObject[] combatCards = gManager.combatCards;
        bool[] playerCards = gManager.players[playerIndex].GetComponent<PlayerBehavior>().combatCardsAvailable;
        for (int i = 1; i < playerCards.GetLength(0); i++)
        {
            if (playerCards[i] && combatValue == combatCards[i].GetComponent<CombatCards>().combatValue)
            {
                gManager.players[playerIndex].GetComponent<PlayerBehavior>().combatCardsAvailable[i] = false;
                break;
            }
        }
    }

    #region Animations
    private float alpha { get { return image.color.a; } }

    private void setChildrenAlpha(float alpha)
    {
        image.color = new Color(1, 1, 1, alpha);
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).GetComponent<Image>().color = new Color(1, 1, 1, alpha);
    }

    // Animation d'apparition de la carte
    IEnumerator fadeIn()
    {
        const float timeBeforeFadeIn = 1.5f;
        const float intervals = 0.02f;
        setChildrenAlpha(alpha);
        yield return new WaitForSeconds(timeBeforeFadeIn);
        do
        {
            setChildrenAlpha(alpha + 0.1f);
            yield return new WaitForSeconds(intervals);
        }
        while (alpha < 1);
        selectionable = true;
    }

    // Animation de disparition de la carte
    IEnumerator fadeOut(float duration)
    {
        if (!cardChosen)
        {
            Debug.Assert(selectionable);
            const float timeBeforeFadeOut = 0.0f;
            const float intervals = 0.01f;
            float startTime = Time.time;
            selectionable = false;
            yield return new WaitForSeconds(timeBeforeFadeOut);
            do
            {
                float valueProgression = (Time.time - startTime) / duration;
                setChildrenAlpha(Mathf.Lerp(1, 0, valueProgression));
                yield return new WaitForSeconds(intervals);
            }
            while (Time.time - startTime < duration);
            image.enabled = false;
        }
    }

    IEnumerator goToChoosenCardSpace(float startTime, float duration, Vector3 from, Vector3 to)
    {
        float valueProgression = (Time.time - startTime) / duration;
        transform.position = Vector3.Lerp(from, to, valueProgression);
        yield return new WaitForSeconds(0.01f);
        if (Time.time - startTime < duration) StartCoroutine(goToChoosenCardSpace(startTime, duration, from, to));
        else transform.position = to;
    }

    IEnumerator changeSizeDelta(float startTime, float duration, Vector2 from, Vector2 to)
    {
        float valueProgression = (Time.time - startTime) / duration;
        rTransform.sizeDelta = Vector2.Lerp(from, to, valueProgression);
        yield return new WaitForSeconds(0.01f);
        if (Time.time - startTime < duration) StartCoroutine(changeSizeDelta(startTime, duration, from, to));
        else rTransform.sizeDelta = to;
    }

    // Détruire les Game Object des cartes
    IEnumerator removeCards (float wait) {
		yield return new WaitForSeconds(wait);
		Destroy(transform.parent.gameObject);
	}
    #endregion
}
