using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonHideTokens : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {

    public AudioClip hideSoundFeedback;

    public void OnPointerDown(PointerEventData eventData)
    {
        hideTokens();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        unhideTokens();
    }

    public void hideTokens()
    {
        GameObject[] tokens = GameObject.FindGameObjectsWithTag("Token");
        foreach (GameObject t in tokens)
        {
            t.GetComponent<TokenIHM>().tokenHidden = true;
        }
        GameManager.gManager.playSound(hideSoundFeedback);
    }

    public void unhideTokens()
    {
        GameObject[] tokens = GameObject.FindGameObjectsWithTag("Token");
        foreach (GameObject t in tokens)
        {
            t.GetComponent<TokenIHM>().tokenHidden = false;
            t.GetComponent<TokenIHM>().displayToken();
        }
    }


}
