using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SwitchMenuAnimation : MonoBehaviour {

    public Transform top;
    public Transform bottom;
    public AudioClip closeHatch;
    public AudioClip openHatch;
    public float openHatchAnimDuration = 1.0f;
    public float closeHatchAnimDuration = 0.6f;
    public bool launchGame
    {
        get { return _launchGame; }
        set { _launchGame = value; }
    }
    bool _launchGame = false;
    AppManager app;
    ScreenShake screen;
    Transform wheel;
    Vector3 topStartPosition;
    Vector3 bottomStartPosition;
    Vector3 topOpenPosition;
    Vector3 bottomOpenPosition;

    float rotationSpeed = 32.0f;
    float alpha = 1.0f;
    float timerFade, timerAnimation;

    GameObject logoDT, menuToOpen, menuToClose;
    List<Image> doorsImages = new List<Image>();


    void Awake()
    {
        topStartPosition = top.position;
        bottomStartPosition = bottom.position;
        topOpenPosition = new Vector3(top.position.x, transform.position.y + (float)Screen.height * 0.5f, top.position.z);
        bottomOpenPosition = new Vector3(bottom.position.x, transform.position.y - (float)Screen.height * 0.5f, bottom.position.z);
        logoDT = transform.Find("Logo DT").gameObject;
        wheel = transform.parent.Find("Wheel");
        doorsImages.AddRange(GetComponentsInChildren<Image>());
        doorsImages.Remove(logoDT.GetComponent<Image>());
        screen = transform.parent.GetComponent<ScreenShake>();
        app = AppManager.appManager;
    }

    public void openAppMenu(GameObject firtMenuToDisplay)
    {
        timerFade = Time.time;
        StartCoroutine(fadeOutDTLogo(0.5f));
        if (true) // AppManager.appManager.firstLaunchTutorialComplete) // Cyril: commented this check to avoid forcing tutorial in test versions
        {
            firtMenuToDisplay.SetActive(true);
            Invoke("openDoors", 0.8f);
        }
        else
        {
            AppManager.appManager.tutorial(true);
            AppManager.appManager.loadTutorial(AppManager.appManager.currentTutorialLevel);
            loadLevelAnimation();
        }
    }

    IEnumerator fadeOutDTLogo(float fadeDuration)
    {
        float valueProgression = (Time.time - timerFade) / fadeDuration;
        logoDT.transform.localScale = logoDT.transform.localScale * 1.01f;
        alpha = 1 - valueProgression;
        logoDT.GetComponent<Image>().color = new Color(1, 1, 1, alpha);
        yield return new WaitForSeconds(0.01f);
        if (Time.time - timerFade < fadeDuration) StartCoroutine(fadeOutDTLogo(fadeDuration));
        else
        {
            logoDT.SetActive(false);
            alpha = 1.0f;
        }
    }

	public void openDoors () {
        AudioSource.PlayClipAtPoint(openHatch, Vector3.zero);
        timerAnimation = Time.time;
        StartCoroutine(openDoorsCoroutine(openHatchAnimDuration));
	}

    IEnumerator openDoorsCoroutine(float animDuration)
    {
        float valueProgression = (Time.time - timerAnimation) / animDuration;
        top.position = Vector3.Lerp(topStartPosition, topOpenPosition, valueProgression);
        bottom.position = Vector3.Lerp(bottomStartPosition, bottomOpenPosition, valueProgression);
        yield return new WaitForSeconds(0.01f);
        if (Time.time - timerAnimation < animDuration) StartCoroutine(openDoorsCoroutine(animDuration));
        else
        {
            setDoorsToOpenPosition();
        }
    }

    public void setDoorsToOpenPosition()
    {
        top.position = topOpenPosition;
        bottom.position = bottomOpenPosition;
        foreach (Image i in doorsImages)
        {
            i.enabled = false;
        }
        //StartCoroutine(hideMasks());
        if (menuToOpen != null && menuToOpen.GetComponent<Multi.Status>() != null)
        {
            menuToOpen.GetComponent<Multi.Status>().Open();
        }
    }

    IEnumerator hideMasks()
    {
        alpha -= 0.03f;
        top.parent.GetComponent<Image>().color = new Color(0, 0, 0, alpha);
        bottom.parent.GetComponent<Image>().color = new Color(0, 0, 0, alpha);
        yield return new WaitForSeconds(0.01f);
        if (alpha > 0) StartCoroutine(hideMasks());
        else
        {
            alpha = 0;
            foreach (Image i in doorsImages)
            {
                i.enabled = false;
            }
        }
    }

    public void openMenu(GameObject menu)
    {
        menuToOpen = menu;
        app.currentMenuOpened = menuToOpen.name;
        closeDoors();
        //StartCoroutine(displayMasks());
    }

    public void closeMenu(GameObject menu)
    {
        menuToClose = menu;
    }

    public void closeDoors()
    {
        foreach (Image i in doorsImages)
        {
            i.enabled = true;
        }
        AudioSource.PlayClipAtPoint(closeHatch, Vector3.zero);
        timerAnimation = Time.time;
        StartCoroutine(closeDoor(closeHatchAnimDuration));
        //StartCoroutine(displayMasks());
    }

    IEnumerator displayMasks()
    {
        alpha += 0.03f;
        top.parent.GetComponent<Image>().color = new Color(0, 0, 0, alpha);
        bottom.parent.GetComponent<Image>().color = new Color(0, 0, 0, alpha);
        yield return new WaitForSeconds(0.01f);
        if (alpha < 1) StartCoroutine(displayMasks());
        else
        {
            timerAnimation = Time.time;
            StartCoroutine(closeDoor(0.6f));
        }
    }

    IEnumerator closeDoor(float animDuration)
    {
        if (menuToClose != null && menuToClose.GetComponent<Multi.Status>() != null)
        {
            menuToClose.GetComponent<Multi.Status>().Close();
        }

        float valueProgression = (Time.time - timerAnimation) / animDuration;
        top.position = Vector3.Lerp(topOpenPosition, topStartPosition, valueProgression);
        bottom.position = Vector3.Lerp(bottomOpenPosition, bottomStartPosition, valueProgression);
        yield return new WaitForSeconds(0.01f);
        if (Time.time - timerAnimation < animDuration) StartCoroutine(closeDoor(animDuration));
        else
        {
            top.position = topStartPosition;
            bottom.position = bottomStartPosition;
            menuToClose.SetActive(false);
            screen.launchShake(0.75f);
            if (!launchGame)
            {
                menuToOpen.SetActive(true);
                Invoke("openDoors", 0.5f);
            }
            else loadLevelAnimation();
        }
    }

    void loadLevelAnimation()
    {
        Invoke("openDoors", 0.8f);
        InvokeRepeating("turnWheel", 0.8f, 0.01f);
        transform.Find("Loading Text").gameObject.SetActive(true);
        Invoke("loadLevel", 1.3f);
    }

    void turnWheel()
    {
        wheel.Rotate(wheel.forward * rotationSpeed * Time.deltaTime);
    }

    void loadLevel()
    {
        //CancelInvoke("turnWheel");
        GameObject.FindGameObjectWithTag("Manager").GetComponent<MenuManager>().loadLevel();
    }

}
