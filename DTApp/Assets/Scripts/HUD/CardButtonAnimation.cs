using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CardButtonAnimation : MonoBehaviour {

    Image cardWhiteIcon;
    Color transparentWhite = new Color(1, 1, 1, 0);

	// Use this for initialization
	void Awake () {
        cardWhiteIcon = GetComponent<Image>();
        cardWhiteIcon.color = transparentWhite;
	}

    void OnEnable()
    {
        StartCoroutine(fadeAnimation(transparentWhite, Color.white, Time.time, 1.3f));
    }

    void OnDisable()
    {
        cardWhiteIcon.color = transparentWhite;
    }

    IEnumerator fadeAnimation(Color from, Color to, float startTime, float duration)
    {
        float valueProgression = (Time.time - startTime) / duration;
        cardWhiteIcon.color = Color.Lerp(from, to, valueProgression);
        yield return new WaitForSeconds(0.01f);
        if (Time.time - startTime < duration) StartCoroutine(fadeAnimation(from, to, startTime, duration));
        else
        {
            cardWhiteIcon.color = to;
            StartCoroutine(fadeAnimation(to, from, Time.time, duration));
        }
    }
}
