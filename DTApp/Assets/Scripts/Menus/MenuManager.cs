using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour {

    public AudioClip[] buttonClicSounds;
    bool hasClicSounds;
    string levelToLoad = "game";
    [HideInInspector]
    public float contentElementSeparationDistance = 0;
    [HideInInspector]
    public float contentElementStartingPoint = 0;
    AppManager app;
    new AudioSource audio;

    void Awake()
    {
        DontDestroyOnLoad(GameObject.Find("Canvas Main Menu"));
    }

	// Use this for initialization
    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
        audio = GetComponent<AudioSource>();
        app = GameObject.FindGameObjectWithTag("App").GetComponent<AppManager>();
        hasClicSounds = (buttonClicSounds.GetLength(0) > 0);

        if (app.currentMenuOpened != "N/A")
        {
            GameObject title = GameObject.Find("Canvas Main Menu/MenuUI/Title");
            title.GetComponent<Button>().enabled = false;
            title.GetComponent<SwitchMenuAnimation>().openAppMenu(GameObject.Find("Canvas Main Menu/MenuUI/" + app.currentMenuOpened));
            if (app.gameToLaunch.isTutorial) GameObject.Find("Canvas/Tutoriel menu").GetComponent<SmallMenuAnimation>().openMenu();
        }
        app.SendMessage("initGameSpecs");
	}

	public void loadLevel() {
        SceneManager.LoadSceneAsync(levelToLoad);
	}

    public void loadBaseGame()
    {
        app.loadBaseGame();
    }

    public void loadTutorial(string tutoName)
    {
        app.loadTutorial(tutoName);
    }

    public void tutorial(bool isTutorial)
    {
        app.tutorial(isTutorial);
    }

    public void playButtonClicSound()
    {
        if (hasClicSounds && app.soundOn)
        {
            audio.PlayOneShot(buttonClicSounds[UnityEngine.Random.Range(0, buttonClicSounds.Length)]);
        }
    }

}
