using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PremadeBoardSetupParameters : MonoBehaviour {

    public int nbTiles;
    public int actionPoints;
    public int boardLength;
    public int requiredVictoryPoints = 1;

    public bool specialValues = false;
    public bool lockCamera = false;
    public bool opponentStartFirst = false;
    public bool useActionCards = true;
    public int[] availableActionCards;
    public bool useStrongestCombatCards = false;
    //public string missionName = "MISSION";
    [TextArea]
    public string missionInstruction = "";
    public CharacterBehavior missionFailureCondition;
    public CharacterBehavior mainOpposingCharacter;
    public List<GameObject> opponentObjectives = new List<GameObject>();

    GameManager gManager;
    bool missionFailed = false;

    void Start()
    {
        gManager = GameManager.gManager;
        if (specialValues)
        {
            gManager.VICTORY_POINTS_LIMIT = requiredVictoryPoints;
            Transform refCamera = transform.Find("Ref Camera");
            Camera mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            mainCamera.orthographicSize = refCamera.GetComponent<Camera>().orthographicSize;
            mainCamera.transform.position = refCamera.position;
            if (lockCamera)
            {
                mainCamera.GetComponent<ZoomControl>().enabled = false;
                float currentRatio = (float)Screen.width / (float)Screen.height;
                float expectedRatio = 16.0f / 9.0f;
                if (currentRatio < expectedRatio)
                {
                    float multiplier = expectedRatio / currentRatio;
                    mainCamera.orthographicSize *= multiplier;
                }
            }
            if (useActionCards)
            {
                if (availableActionCards.GetLength(0) > 0)
                {
                    gManager.valeurMaxCarteAction = 5;
                    int cpt = 0;
                    Transform actionCards = GameObject.Find("Action Cards").transform;
                    // Désactiver toutes les cartes action et activer seulement les cartes action disponibles
                    for (int i = 0; i < actionCards.childCount; i++)
                    {
                        if (actionCards.GetChild(i).GetComponent<ActionCards>().actionPointsValue != availableActionCards[cpt]) actionCards.GetChild(i).gameObject.SetActive(false);
                        else
                        {
                            if (cpt + 1 < availableActionCards.GetLength(0))
                            {
                                cpt++;
                            }
                        }
                    }
                }
            }
            else
            {
                Transform actionCards = GameObject.Find("Action Cards").transform;
                for (int i = 0; i < actionCards.childCount; i++)
                {
                    actionCards.GetChild(i).gameObject.SetActive(false);
                }
            }
        }
    }

    public GameObject getLeftStartZone()
    {
        return (transform.Find("Yellow Tutorial Starting Line").gameObject);
    }

    public GameObject getRightStartZone()
    {
        return (transform.Find("Blue Tutorial Starting Line").gameObject);
    }

    void Update()
    {
        if (!missionFailed && missionFailureCondition != null)
        {
            if (missionFailureCondition.wounded)
            {
                missionFailed = true;
                Invoke("missionFailure", 2); // Using Invoke to wait for combat UI to close
            }
        }
    }

    void missionFailure()
    {
        gManager.SendMessage("tutorialMissionFailed");
    }

}
