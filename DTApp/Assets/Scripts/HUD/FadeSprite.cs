using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeSprite : MonoBehaviour {

	public CharacterBehavior fighter;
	public float timeBeforeFadeIn = 0.3f;
	Color targetColor, targetTransparentColor;
	Color transparentRed = new Color(1, 0, 0, 0);
	Color transparentWhite = new Color(1, 1, 1, 0);
	Image image;

	// Use this for initialization
	void Awake () {
		image = GetComponent<Image>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public IEnumerator launchFadeIn () {
		image.color = transparentWhite;
		targetColor = Color.white;
		targetTransparentColor = transparentWhite;
		if (fighter != null) {
			if (fighter.wounded) {
				image.color = transparentRed;
				targetColor = Color.red;
				targetTransparentColor = transparentRed;
			}
		}
		yield return new WaitForSeconds(timeBeforeFadeIn);
		StartCoroutine(fadeIn());
	}
	
	IEnumerator fadeIn () {
		yield return new WaitForSeconds(1.0f/GameManager.COMBAT_ANIMATION_SPEED);
		image.color = Color.Lerp(image.color, targetColor, 0.2f);
		if (image.color != targetColor) {
			if (image.color.a < 0.95f) StartCoroutine(fadeIn());
			else BroadcastMessage("endFadeIn");
		}
	}
	
	void endFadeIn () {
		image.color = targetColor;
	}
	
	public void launchFadeOut () {
		if (fighter != null) {
			Debug.Log("Début : " + Time.time);
			if (fighter.wounded) targetTransparentColor = transparentRed;
		}
		StartCoroutine(fadeOut());
	}
	
	IEnumerator fadeOut () {
		//Debug.Log(gameObject + " : " + image.color.ToString());
		yield return new WaitForSeconds(1.0f/GameManager.COMBAT_ANIMATION_SPEED);
		image.color = Color.Lerp(image.color, targetTransparentColor, 0.3f);
		if (image.color != targetTransparentColor) StartCoroutine(fadeOut());
		else if (fighter != null) Debug.Log("Fin : " + Time.time);
	}
}
