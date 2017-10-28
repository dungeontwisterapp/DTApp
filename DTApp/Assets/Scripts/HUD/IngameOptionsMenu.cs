using UnityEngine;
using System.Collections;

public class IngameOptionsMenu : MonoBehaviour {

    public GameObject skipTurnButton;
    public GameObject nextLevelButton;

    public GameObject dropGameButton;
    public GameObject forfeitButton;
    public GameObject retryButton;

	// Use this for initialization
    void Start()
    {
        skipTurnButton.SetActive(!GameManager.gManager.app.gameToLaunch.isTutorial);
        nextLevelButton.SetActive(GameManager.gManager.app.gameToLaunch.isTutorial);

        dropGameButton.SetActive(!GameManager.gManager.app.gameToLaunch.isTutorial && !GameManager.gManager.onlineGame);
        forfeitButton.SetActive(GameManager.gManager.onlineGame);
        retryButton.SetActive(GameManager.gManager.app.gameToLaunch.isTutorial);
	}

}
