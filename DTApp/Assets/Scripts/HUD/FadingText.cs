using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadingText : MonoBehaviour {

    public Font alternativeFont;
	float movingSpeed = 0.01f;
    GUIText textMesh;
    Text textUI;
    Outline outline;

    Color transparentTextColor;

    void Awake()
    {
        Color textColor = GameManager.gManager.activePlayer.playerColor;
        transparentTextColor = new Color(textColor.r, textColor.r, textColor.b, 0);
        textUI = GetComponent<Text>();
        outline = GetComponent<Outline>();
        textMesh = GetComponent<GUIText>();
        if (textUI != null)
        {
            movingSpeed *= 1000;
            transparentTextColor = new Color(1, 1, 1, 0);
            textUI.color = Color.white;
            outline.effectColor = textColor;
        }
        else if (textMesh != null) textMesh.color = textColor;
    }

	public void displayVPEarnedFeedback (int victoryPointsEarned) {
        if (textUI != null)
        {
            textUI.fontSize = Mathf.FloorToInt(Screen.height / (20 - victoryPointsEarned * 4) * (Camera.main.GetComponent<ZoomControl>().ZOOM_MAX / Camera.main.orthographicSize));
            textUI.text = "+" + victoryPointsEarned.ToString();
            textUI.enabled = true;
            outline.enabled = true;
        }
        else if (textMesh != null)
        {
            textMesh.fontSize = Mathf.FloorToInt(Screen.height / (20 - victoryPointsEarned * 4) * (Camera.main.GetComponent<ZoomControl>().ZOOM_MAX / Camera.main.orthographicSize));
            textMesh.text = "+" + victoryPointsEarned.ToString();
            textMesh.enabled = true;
        }
		StartCoroutine(selfDestruct(10));
		StartCoroutine(launchFadeOut(0.5f));
	}

    public void displayActionPointsUsedFeedback(int actionPointsUsed)
    {
        int fontSize = Mathf.FloorToInt(Screen.height / (30 - actionPointsUsed * 4) * (Camera.main.GetComponent<ZoomControl>().ZOOM_MAX / Camera.main.orthographicSize));
        if (textUI != null)
        {
            textUI.fontSize = fontSize;
            textUI.font = alternativeFont;
            textUI.text = "-" + actionPointsUsed.ToString() + " AP";
            textUI.enabled = true;
            outline.effectColor = Color.black;
            outline.enabled = true;
        }
        else if (textMesh != null)
        {
            textMesh.fontSize = fontSize;
            textUI.font = alternativeFont;
            textMesh.text = "-" + actionPointsUsed.ToString() + " AP";
            textMesh.enabled = true;
        }
        StartCoroutine(selfDestruct(10));
        StartCoroutine(launchFadeOut(0.5f));
    }

	void Update () {
		transform.Translate(0, movingSpeed * Time.deltaTime, 0);
	}
	
	IEnumerator launchFadeOut (float wait) {
		yield return new WaitForSeconds(wait);
		StartCoroutine(fadeOut(0.03f));
	}
	
	IEnumerator fadeOut (float interval) {
		yield return new WaitForSeconds(interval);
        if (textUI != null)
        {
            if (textUI.color.a > 0.3f) textUI.color = Color.Lerp(textUI.color, transparentTextColor, 0.1f);
            else textUI.color = Color.Lerp(textUI.color, transparentTextColor, 0.3f);
            if (textUI.color != transparentTextColor) StartCoroutine(fadeOut(interval));
        }
        else if (textMesh != null)
        {
            if (textMesh.color.a > 0.3f) textMesh.color = Color.Lerp(textMesh.color, transparentTextColor, 0.1f);
            else textMesh.color = Color.Lerp(textMesh.color, transparentTextColor, 0.3f);
            if (textMesh.color != transparentTextColor) StartCoroutine(fadeOut(interval));
        }
	}

	IEnumerator selfDestruct (float timeBeforeDestroy) {
		yield return new WaitForSeconds(timeBeforeDestroy);
		Destroy(gameObject);
	}
}
