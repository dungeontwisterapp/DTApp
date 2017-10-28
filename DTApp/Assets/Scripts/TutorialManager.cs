using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour {

    AppManager app;
    GameManager gManager;
    Text textInfo;
    JSONObject tutorialInstructionsData;
    int nbInfos = 0, currentInfoIndex;
    public GameObject infoUI;

    delegate bool CheckStep();
    CheckStep checkForkNextStep;
    delegate void ExecuteStep();
    ExecuteStep doNextStep;

    bool missionTitlePassed = false;

    enum Tuto01 { Presentation, Start, SelectCharacter, MoveCharacter, EndMovement, ActionPoints, GameGoal, End };
    Tuto01 tuto01Progression = Tuto01.Presentation;

	// Use this for initialization
    void Start()
    {
        app = AppManager.appManager;
        gManager = GameManager.gManager;
        if (!app.gameToLaunch.isTutorial) this.enabled = false;
        else
        {
            currentInfoIndex = 0;
            textInfo = infoUI.transform.Find("Text").GetComponent<Text>();
            tutorialInstructionsData = app.GetComponent<LanguageManager>().tutorialsTexts.GetField(app.gameToLaunch.tutorialName).GetField("ContextualInfo").GetField(app.gameLanguage.ToString());
            nbInfos = tutorialInstructionsData.Count;
            if (nbInfos > 0)
            {
                textInfo.text = tutorialInstructionsData[currentInfoIndex].str;
            }
            else this.enabled = false;
            checkForkNextStep = tuto01checks;
            doNextStep = tuto01actions;
        }
	}

    public void startMission()
    {
        missionTitlePassed = true;
    }

	// Update is called once per frame
	void Update () {
        if (checkForkNextStep())
        {
            doNextStep();
        }
	}

    public void displayNextInfo()
    {
        if (currentInfoIndex < nbInfos)
        {
            currentInfoIndex++;
            textInfo.text = tutorialInstructionsData[currentInfoIndex].str;
            infoUI.SetActive(true);
        }
        else this.enabled = false;
    }

    public void hideInfo()
    {
        infoUI.SetActive(false);
    }

    #region SpecificTutorial

    void tuto01actions()
    {
        switch (tuto01Progression)
        {
            case Tuto01.Presentation:
                tuto01Progression = Tuto01.Start;
                break;
            case Tuto01.Start:
                GameObject[] tokens = GameObject.FindGameObjectsWithTag("Token");
                tokens[0].GetComponent<CharacterBehaviorIHM>().boardEntry();
                infoUI.SetActive(true);
                tuto01Progression = Tuto01.SelectCharacter;
                break;
            case Tuto01.SelectCharacter:
                displayNextInfo();
                tuto01Progression = Tuto01.MoveCharacter;
                break;
            case Tuto01.MoveCharacter:
                displayNextInfo();
                tuto01Progression = Tuto01.EndMovement;
                break;
            case Tuto01.EndMovement:
                displayNextInfo();
                tuto01Progression = Tuto01.ActionPoints;
                break;
            case Tuto01.ActionPoints:
                displayNextInfo();
                tuto01Progression = Tuto01.GameGoal;
                break;
            case Tuto01.GameGoal:
                displayNextInfo();
                tuto01Progression = Tuto01.End;
                break;
            case Tuto01.End:
                hideInfo();
                break;
        }
    }

    bool tuto01checks()
    {
        switch (tuto01Progression)
        {
            case Tuto01.Presentation: return missionTitlePassed;
            case Tuto01.Start: return missionTitlePassed;
            case Tuto01.SelectCharacter: return gManager.selectionEnCours;
            case Tuto01.MoveCharacter: return gManager.deplacementEnCours;
            case Tuto01.EndMovement: return (GameManager.gManager.pointsAction == 4);
            case Tuto01.ActionPoints: break;
            case Tuto01.GameGoal: break;
            case Tuto01.End: break;
        }
        return false;
    }

    #endregion
}
