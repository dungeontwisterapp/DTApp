using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class SwitchPlayerBehavior : MonoBehaviour {

    public Sprite turnPlayerOne;
    public Sprite turnPlayerTwo;
    public Text instruction;

    bool available = false;

	GameManager gManager;
	Image fond, support;
    Text switchText;
	PlayerBehavior currentPlayer;

    Color fondStartColor, transparentWhite = new Color(1, 1, 1, 0), transparentBlack = new Color(0, 0, 0, 0);

	// Use this for initialization
    void Awake()
    {
        gManager = GameManager.gManager;
        fond = GetComponent<Image>();
        support = transform.Find("Image").GetComponent<Image>();
        switchText = transform.Find("Image/Text").GetComponent<Text>();
        if (gManager.app.gameToLaunch.isTutorial)
        {
            available = true;
            displayMissionInstructions();
        }
        else
        {
            instruction.gameObject.SetActive(false);
            if (GetComponent<BoxCollider>() != null) GetComponent<Collider>().enabled = false;
            else GetComponent<Button>().interactable = false;
        }
        fondStartColor = fond.color;
        support.color = Color.white;
        enableDisplay(true);
	}
    
    void Start()
    {
        if (!gManager.app.gameToLaunch.isTutorial)
        {
            switchText.text = currentPlayer.playerName;
            switchText.GetComponent<Shadow>().effectColor = currentPlayer.playerColor;
        }
    }

    void displayMissionInstructions()
    {
        try
        {
            PremadeBoardSetupParameters board = GameObject.Find("Board").GetComponent<PremadeBoardSetupParameters>();
            instruction.text = board.missionInstruction;
            switchText.text = gManager.app.GetComponent<LanguageManager>().tutorialsTexts.GetField(gManager.app.gameToLaunch.tutorialName).GetField("MissionTitle").GetField(gManager.app.gameLanguage.ToString()).str;
        }
        catch (System.Exception)
        {
            Invoke("displayMissionInstructions", 0.1f);
        }
    }

    public void setCurrentActivePlayer()
    {
        currentPlayer = gManager.activePlayer;
        GameObject.Find("Background").GetComponent<Renderer>().material.color = currentPlayer.playerColor;
		//display.color = new Color(currentPlayer.playerColor.r, currentPlayer.playerColor.g, currentPlayer.playerColor.b, alpha);
        if (currentPlayer.index == 0) support.sprite = turnPlayerOne;
        else support.sprite = turnPlayerTwo;
        switchText.text = currentPlayer.playerName;
        switchText.GetComponent<Shadow>().effectColor = currentPlayer.playerColor;
        // Lance l'animation de Fade In de la carte
        enableDisplay(true);
        StartCoroutine(fadeAnimation(Time.time, 0.3f, false));
	}

    public bool isDisplayEnabled() { return support.enabled; }

	void OnMouseDown() {
		if (EventSystem.current.IsPointerOverGameObject()) OnInputDown();
		else Debug.LogWarning("SwitchPlayerBehavior, OnMouseDown: L'input a été détecté sur une partie du HUD");
	}
	
	// Réaction à un clic / appui sur la carte
	public void OnInputDown() {
        if (available)
        {
            available = false;
            newPlayerTurn();
        }
	}

    void newPlayerTurn()
    {
        if (GetComponent<BoxCollider>() != null) GetComponent<Collider>().enabled = false;
        else GetComponent<Button>().interactable = false;

        switch (gManager.gameMacroState)
        {
            case GameProgression.GameStart:
                if (!gManager.app.gameToLaunch.loadExistingGame && 
                    !gManager.onlineGameInterface.isOnlineOpponent(gManager.activePlayer.index)) // online player do not need targets
                {
                    gManager.placementPersonnagesDepart();
                }
                break;
            case GameProgression.Playing:
                if (!gManager.SelectCharacterOnLoadIfNeeded())
                {
                    if (gManager.combatManager.waitingForPlayerCard)
                    {
                        gManager.combatManager.instanciateCurrentPlayerCombatCards();
                    }
                }
                break;
        }

        gManager.startTurn = true;
        if (!gManager.turnStarted) gManager.activePlayer.myTurnToPlay();
        StartCoroutine(fadeAnimation(Time.time, 0.3f, true));

        if (!gManager.onlineGame && !gManager.app.gameToLaunch.isTutorial && !gManager.combatEnCours) gManager.app.SaveGame();
        if (gManager.app.gameToLaunch.isTutorial) gManager.GetComponent<TutorialManager>().startMission();
    }

    // Animation de fade in/out de la carte
    IEnumerator fadeAnimation(float startTime, float duration, bool fadeOut)
    {
        float valueProgression = (Time.time - startTime) / duration;
        instruction.color = fadeOut ? Color.Lerp(Color.white, transparentWhite, valueProgression) : Color.Lerp(transparentWhite, Color.white, valueProgression);
        support.color = fadeOut ? Color.Lerp(Color.white, transparentWhite, valueProgression) : Color.Lerp(transparentWhite, Color.white, valueProgression);
        switchText.color = fadeOut ? Color.Lerp(Color.black, transparentBlack, valueProgression) : Color.Lerp(transparentBlack, Color.black, valueProgression);
        fond.color = fadeOut ? Color.Lerp(fondStartColor, transparentBlack, valueProgression) : Color.Lerp(transparentBlack, fondStartColor, valueProgression);
        yield return new WaitForSeconds(0.01f);
        if (Time.time - startTime < duration) StartCoroutine(fadeAnimation(startTime, duration, fadeOut));
        else
        {
            instruction.color = fadeOut ? transparentWhite : Color.white;
            support.color = fadeOut ? transparentWhite : Color.white;
            switchText.color = fadeOut ? transparentBlack : Color.black;
            fond.color = fadeOut ? transparentBlack : fondStartColor;
            if (fadeOut)
            {
                enableDisplay(false);
                instruction.enabled = false;
            }
            else enableDisplayAndInteraction();
        }
    }

    void enableDisplay(bool enable)
    {
        fond.enabled = enable;
        support.enabled = enable;
        switchText.enabled = enable;
    }

    void enableDisplayAndInteraction()
    {
        if (gManager.onlineGame)
        {
            Invoke("newPlayerTurn", 1.2f);
        }
        else
        {
            if (GetComponent<BoxCollider>() != null) GetComponent<Collider>().enabled = true;
            else GetComponent<Button>().interactable = true;
            available = true;
        }
    }

}
