using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeText : MonoBehaviour {

	public float timeBeforeFadeIn = 0.3f;
	Color standardColor, transparent;
	Text displayedText;

	// Use this for initialization
	void Awake () {
		displayedText = GetComponent<Text>();
        standardColor = new Color(displayedText.color.r, displayedText.color.g, displayedText.color.b, 1);
		transparent = new Color(displayedText.color.r, displayedText.color.g, displayedText.color.b, 0);
	}

	public IEnumerator launchFadeIn () {
		displayedText.color = transparent;
		yield return new WaitForSeconds(timeBeforeFadeIn);
		StartCoroutine(fadeIn(Time.time, 0.6f)); // 1.0f / GameManager.COMBAT_ANIMATION_SPEED
	}

    IEnumerator fadeIn(float startTime, float duration)
    {
        float valueProgression = (Time.time - startTime) / duration;
        displayedText.color = Color.Lerp(transparent, standardColor, valueProgression);
		yield return new WaitForSeconds(0.01f);
        if (Time.time - startTime < duration) StartCoroutine(fadeIn(startTime, duration));
        else displayedText.color = standardColor;
	}

	void endFadeIn () {
		displayedText.color = standardColor;
	}
	
	public void launchFadeOut () {
		StartCoroutine(fadeOut());
	}
	
	IEnumerator fadeOut () {
		yield return new WaitForSeconds(1.0f/GameManager.COMBAT_ANIMATION_SPEED);
		displayedText.color = Color.Lerp(displayedText.color, transparent, 0.3f);
		if (displayedText.color != transparent) StartCoroutine(fadeOut());
	}
}
