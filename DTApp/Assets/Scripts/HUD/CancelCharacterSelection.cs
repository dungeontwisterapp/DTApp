using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class CancelCharacterSelection : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {

    GameManager gManager;

    float LONG_TOUCH_THRESHOLD = 0.1f;
    float timerTouch;

	// Use this for initialization
    void Start()
    {
        gManager = GameManager.gManager;
	}

    public void OnPointerDown(PointerEventData e)
    {
        timerTouch = Time.time;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (gManager.playerInteractionAvailable())
        {
            if (Time.time - timerTouch < LONG_TOUCH_THRESHOLD && gManager.actionCharacter != null)
            {
                if (gManager.isInformationAndExchangeUIOpen())
                {
                    gManager.closeInformationAndExchangeUI();
                }
                else if (!gManager.deplacementEnCours && !gManager.rotationEnCours)
                {
                    if (!gManager.isActivePlayer(gManager.actionCharacter.GetComponent<CharacterBehavior>().affiliationJoueur))
                    {
                        gManager.actionCharacter = null;
                    }
                    else
                    {
                        gManager.actionCharacter.GetComponent<CharacterBehaviorIHM>().OnInputDown();
                        gManager.actionCharacter.GetComponent<CharacterBehaviorIHM>().OnInputUp();
                    }
                }
            }
        }
    }
    /*
    void OnMouseDown()
    {
        timerTouch = Time.time;
    }
    
    void OnMouseUp()
    {
        if (Time.time - timerTouch < LONG_TOUCH_THRESHOLD && gManager.actionCharacter != null)
        {
            if (!gManager.deplacementEnCours)
            {
                gManager.actionCharacter.GetComponent<CharacterBehaviorIHM>().OnInputDown();
                gManager.actionCharacter.GetComponent<CharacterBehaviorIHM>().OnInputUp();
            }
            else
            {

            }
        }
    }
    */
}
