using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DisplayTutorialScore : MonoBehaviour {

    PlayerBehavior joueur;
    Text score;

    string standardDisplay = " / ";
    string scoreGoal = "0";

	// Use this for initialization
	void Start () {
        if (!GameManager.gManager.app.gameToLaunch.isTutorial) transform.parent.gameObject.SetActive(false);
        joueur = GameManager.gManager.players[0].GetComponent<PlayerBehavior>();
        score = GetComponent<Text>();
        displayVictoryGoal();
	}

    void displayVictoryGoal()
    {
        try
        {
            scoreGoal = GameObject.Find("Board").GetComponent<PremadeBoardSetupParameters>().requiredVictoryPoints.ToString();
        }
        catch (System.Exception)
        {
            Invoke("displayVictoryGoal", 0.1f);
        }
    }
	
	// Update is called once per frame
	void Update () {
        score.text = joueur.victoryPoints.ToString() + standardDisplay + scoreGoal;
	}
}
