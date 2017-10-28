using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public delegate void SpecificAction ();

public class ActionWheel : MonoBehaviour {

	public int currentActionButton = 1;
	List<ActionType> actionsAvailable = new List<ActionType>();

	// Use this for initialization
	void Start () {
		hideWheel();
	}
	
	// Update is called once per frame
	void Update () {
		if (currentActionButton > 1) {
			transform.GetChild(0).GetComponent<Image>().enabled = true;
			for (int i=1 ; i < currentActionButton ; i++) {
				transform.GetChild(i).GetComponent<Button>().interactable = true;
				transform.GetChild(i).GetComponent<Image>().enabled = true;
                transform.GetChild(i).GetChild(0).GetComponent<Image>().enabled = true;
                transform.GetChild(i).GetChild(1).GetComponent<Text>().enabled = true;
			}
		}
		else hideWheel();
	}

	public bool isActionDisplayed (ActionType action) {
		return actionsAvailable.Contains(action);
	}
	
	// Faire disparaitre la roue d'action
	public void hideWheel () {
		Image[] sprites = GetComponentsInChildren<Image>();
		foreach (Image sprite in sprites) {
			sprite.enabled = false;
        }
        Text[] texts = GetComponentsInChildren<Text>();
        foreach (Text text in texts)
        {
            text.enabled = false;
        }
    }

    public void activateOneButtonIfNeeded(ActionType action, Sprite actionSprite, SpecificAction functionToCall)
    {
        if (GameManager.gManager.playerInteractionAvailable() && !isActionDisplayed(action))
        {
            activateOneButton(action, actionSprite, functionToCall);
        }
    }

    public void activateOneButton (ActionType action, Sprite actionSprite, SpecificAction functionToCall) {
		if (currentActionButton <= 3) {
			actionsAvailable.Add(action);
            transform.GetChild(currentActionButton).GetChild(0).GetComponent<Image>().sprite = actionSprite;
            string actionName = GameManager.getActionTypeName(action);
            Text text = transform.GetChild(currentActionButton).GetChild(1).GetComponent<Text>();
            text.text = actionName;
            if (actionName.Contains(" ")) text.horizontalOverflow = HorizontalWrapMode.Wrap;
            else text.horizontalOverflow = HorizontalWrapMode.Overflow;
			transform.GetChild(currentActionButton).GetComponent<Button>().onClick.RemoveAllListeners();
			transform.GetChild(currentActionButton).GetComponent<Button>().onClick.AddListener(() => { functionToCall(); });
			currentActionButton++;
		}
		else Debug.LogError("ActionWheel, activateOneButton: Trois boutons ont déjà été activés !");
	}

	public void resetButtonsActions () {
		actionsAvailable.Clear();
		for (int i=1 ; i <= 3 ; i++) {
			transform.GetChild(i).GetComponent<Button>().onClick.RemoveAllListeners();
			transform.GetChild(i).GetComponent<Button>().interactable = false;
		}
		currentActionButton = 1;
	}
}
