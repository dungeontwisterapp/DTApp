using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class HideActionCards : MonoBehaviour, IPointerDownHandler {

    bool cardsHidden = true;
    Image image;

    void Awake()
    {
        float currentRatio = (float)Screen.width / (float)Screen.height;
        float expectedRatio = 16.0f / 9.0f;
        if (currentRatio < expectedRatio)
        {
            float multiplier = 2.1f - expectedRatio / currentRatio;
            transform.localScale = new Vector3(multiplier, multiplier, 1);
        }
    }

    void Start()
    {
        image = GetComponent<Image>();
        image.enabled = false;
    }

    public void activateActionCards()
    {
        image.enabled = true;
        if (cardsHidden)
        {
            cardsHidden = false;
            BroadcastMessage("launchCardInAnimation");
        }
        else BroadcastMessage("launchFadeIn");
    }

    public void deactivateActionCards()
    {
        //image.enabled = false;
        cardsHidden = true;
    }
    /*
	void OnMouseDown () {
		if (EventSystem.current.IsPointerOverGameObject()) BroadcastMessage("launchFadeOut");
		else Debug.LogWarning("HideActionCards, OnMouseDown: L'input a été détecté sur une partie du HUD");
	}
    */
    public void OnPointerDown(PointerEventData eventData)
    {
        if (EventSystem.current.IsPointerOverGameObject()) BroadcastMessage("launchFadeOut");
        else Debug.LogWarning("HideActionCards, OnMouseDown: L'input a été détecté sur une partie du HUD");
    }

}
