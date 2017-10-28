using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ChooseCombatCardScreen : MonoBehaviour {

    [HideInInspector]
    public Transform leftCard, rightCard;
    public Sprite[] cardsBack;
    public GameObject[] combatCardsPrefabs;
    Vector3 rightCardStartPosition, rightCardHiddenPosition;
    [HideInInspector]
    public Text leftCombatValue, rightCombatValue;
    Vector2 smallShade, largeShade;

    GameManager gManager;

    private const float turnCardAnimDuration = 1.2f;

    // Use this for initialization
    void Start()
    {
        gManager = GameManager.gManager;
        leftCombatValue = transform.Find("VS/LeftCombatValue").GetComponent<Text>();
        rightCombatValue = transform.Find("VS/RightCombatValue").GetComponent<Text>();

        leftCard = transform.Find("LeftCard");
        rightCard = transform.Find("RightCard");
        smallShade = transform.Find("Shade").GetComponent<RectTransform>().sizeDelta;
        largeShade = new Vector2(Screen.width * 1.2f, Screen.width * 1.2f);
        leftCard.GetComponent<Image>().enabled = false;
        rightCard.GetComponent<Image>().enabled = false;

        float currentRatio = (float)Screen.width / (float)Screen.height;
        float expectedRatio = 16.0f / 9.0f;
        if (currentRatio < expectedRatio)
        {
            float multiplier = expectedRatio / currentRatio;
            transform.Find("Cards").localScale = new Vector3(2 - multiplier, 2 - multiplier, 1);
            transform.Find("LeftCard").GetComponent<RectTransform>().sizeDelta *= (2 - multiplier);
            transform.Find("RightCard").GetComponent<RectTransform>().sizeDelta *= (2 - multiplier);
        }
        gameObject.SetActive(false);
	}

    public void playFakeCard()
    {
        Vector3 animEndindPoint = rightCard.position;
        rightCard.Translate(Vector3.down * Screen.height);
        rightCard.GetComponent<Image>().enabled = true;
        iTween.MoveTo(rightCard.gameObject, iTween.Hash("position", animEndindPoint, "time", 0.6f, "easetype", iTween.EaseType.easeOutQuad));
    }

    public void combatCardChosen(GameObject card)
    {
        StartCoroutine(combatCardChosenCoroutine(card));
    }

    IEnumerator combatCardChosenCoroutine(GameObject card)
    {
        yield return new WaitForSeconds(1.1f);

        Transform playedCard = (gManager.activePlayer.index == 0) ? leftCard : rightCard;

        // hide the selected card and copy its sprite to the card dummy
        card.transform.rotation = Quaternion.Euler(0, 90, 0);
        playedCard.GetComponent<Image>().enabled = true;
        playedCard.GetComponent<UISelectedCombatCard>().setSelectedCardSprite( card.GetComponent<Image>().sprite );
        playedCard.GetComponent<UISelectedCombatCard>().swapRectoVerso();

        if (!(gManager.scriptedGame || gManager.onlineGame)) // need to turn cards for local 2 player game.
        {
            // hide the first card when played, and reveal it when second card is played
            Transform otherCard = (gManager.activePlayer.index == 1) ? leftCard : rightCard;
            Transform firstCard = (gManager.combatManager.cardPlayedCount == 0) ? playedCard : otherCard;
            StartCoroutine(turnCombatCard(turnCardAnimDuration, firstCard));
            yield return new WaitForSeconds(turnCardAnimDuration);
        }

        gManager.combatManager.combatCardChosen(card);
    }

    IEnumerator turnCombatCard(float duration, Transform card)
    {
        float startTime = Time.time;
        float halfduration = duration /2;

        while (Time.time - startTime < halfduration)
        {
            float valueProgression = (Time.time - startTime) / halfduration;
            card.rotation = LerpYAngle(0, 90, valueProgression);
            yield return new WaitForSeconds(0.01f);
        }

        card.GetComponent<UISelectedCombatCard>().swapRectoVerso();
        startTime += halfduration;

        while (Time.time - startTime < halfduration)
        {
            float valueProgression = (Time.time - startTime) / halfduration;
            card.rotation = LerpYAngle(90, 0, valueProgression);
            yield return new WaitForSeconds(0.01f);
        }
        card.rotation = Quaternion.Euler(0, 0, 0);
    }

    private Quaternion LerpYAngle(float from, float to, float progression) { return Quaternion.Lerp(Quaternion.Euler(0, from, 0), Quaternion.Euler(0, to, 0), progression); }

    public void fadeInScreen()
    {
        StartCoroutine(fadeScreenCoroutine(Time.time, 0.4f, false, GetComponentsInChildren<Image>()));
    }

    public void fadeOutScreen()
    {
        StartCoroutine(fadeScreenCoroutine(Time.time, 0.6f, true, GetComponentsInChildren<Image>()));
    }

    IEnumerator fadeScreenCoroutine(float startTime, float duration, bool fadeOut, Image[] images)
    {
        float valueProgression = (Time.time - startTime) / duration;
        float alpha = valueProgression;
        if (fadeOut) alpha = 1 - valueProgression;
        foreach (Image img in images)
        {
            if (img != null)
            {
                if (img.name != "Shade") img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);
                else
                {
                    if (fadeOut) img.GetComponent<RectTransform>().sizeDelta = Vector2.Lerp(largeShade, smallShade, valueProgression);
                    else img.GetComponent<RectTransform>().sizeDelta = Vector2.Lerp(smallShade, largeShade, valueProgression);
                }
            }
            else Debug.LogError("ChooseCombatCardScreen, fadeScreenCoroutine : Un sprite a été détruit");
        }
        yield return new WaitForSeconds(0.01f);
        if (Time.time - startTime < duration) StartCoroutine(fadeScreenCoroutine(startTime, duration, fadeOut, images));
        else
        {
            if (fadeOut)
            {
                cleanUpScreen();
                gameObject.SetActive(false);
            }
        }
    }

    void cleanUpScreen()
    {
        leftCard.GetComponent<UISelectedCombatCard>().restoreCardVerso();
        rightCard.GetComponent<UISelectedCombatCard>().restoreCardVerso();
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).name.Contains("Dynamic"))
            {
                Destroy(transform.GetChild(i).gameObject);
                //i--;
            }
        }
    }

}
