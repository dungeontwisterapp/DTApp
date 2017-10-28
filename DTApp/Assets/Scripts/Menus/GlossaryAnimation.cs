using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GlossaryAnimation : MonoBehaviour {

    public Button buttonShowList;
    [HideInInspector]
    public bool hidden = true;
    Vector3 displayedGlossaryPosition, hiddenGlossaryPosition, leftGlossaryPosition, rightGlossaryPosition;

    float timerMoveAnimation;
    MenuManager menuManager;
    ScreenShake screen;
    [HideInInspector]
    public RectTransform contentBar;

	// Use this for initialization
	void Awake () {
        displayedGlossaryPosition = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
        hiddenGlossaryPosition = new Vector3(Screen.width * 0.5f, Screen.height * 1.5f, 0);

        leftGlossaryPosition = new Vector3(-Screen.width * 0.5f, Screen.height * 0.5f, 0);
        rightGlossaryPosition = new Vector3(Screen.width * 1.5f, Screen.height * 0.5f, 0);

        transform.position = hiddenGlossaryPosition;
        menuManager = GameObject.FindGameObjectWithTag("Manager").GetComponent<MenuManager>();
        Transform contenus = transform.Find("Contenus");
        if (contenus != null)
        {
            contentBar = contenus.GetChild(0).GetComponent<RectTransform>();
            /*
            if (menuManager.contentElementSeparationDistance == 0)
            {
                menuManager.contentElementStartingPoint = contentBar.position.x;
                menuManager.contentElementSeparationDistance = Mathf.Abs((contentBar.GetChild(0).position - contentBar.GetChild(1).position).x);
            }
            */
        }
        hidden = true;

        float currentRatio = (float)Screen.width / (float)Screen.height;
        float expectedRatio = 16.0f / 9.0f;
        if (currentRatio < expectedRatio)
        {
            //float multiplier = expectedRatio / currentRatio;
            float multiplier2 = currentRatio / expectedRatio;
            //transform.localScale = new Vector3(2 - multiplier, 2 - multiplier, 1);
            transform.localScale = new Vector3(multiplier2, multiplier2, 1);
        }
	}

    void Start()
    {
        screen = transform.parent.GetComponent<ScreenShake>();
        if (contentBar != null)
        {
            if (menuManager.contentElementSeparationDistance == 0)
            {
                menuManager.contentElementStartingPoint = contentBar.position.x;
                menuManager.contentElementSeparationDistance = Mathf.Abs((contentBar.GetChild(0).position - contentBar.GetChild(1).position).x);
            }
        }
    }

    private void SetButtonInteractable(bool enable)
    {
        if (buttonShowList != null) buttonShowList.interactable = enable;
    }

    public void playRevealAnimation ()
    {
        if (hidden)
        {
            hidden = false;
            SetButtonInteractable(false);
            timerMoveAnimation = Time.time;
            StartCoroutine(revealGlossary(0.6f));
        }
    }

    IEnumerator revealGlossary(float animDuration)
    {
        if (!hidden)
        {
            float valueProgression = (Time.time - timerMoveAnimation) / animDuration;
            transform.position = Vector3.Lerp(hiddenGlossaryPosition, displayedGlossaryPosition, valueProgression);
            //transform.position = Vector3.MoveTowards(transform.position, displayedGlossaryPosition, Screen.width * 1.5f * Time.deltaTime);
            yield return new WaitForSeconds(0.01f);
            if (Time.time - timerMoveAnimation < animDuration) StartCoroutine(revealGlossary(animDuration));
            else
            {
                transform.position = displayedGlossaryPosition;
                screen.launchShake();
                SetButtonInteractable(true);
            }
        }
    }

    void playDisplayFromSideAnimation(bool fromRight)
    {
        if (hidden)
        {
            hidden = false;
            SetButtonInteractable(false);
            buttonShowList.interactable = false;
            //float xValue = (float)Screen.width * (float)Screen.width / 1000.0f - contentBar.rect.xMin - GetComponent<DisplayGlossaryInfo>().currentIndex * menuManager.contentElementSeparationDistance;
            //float xValue = (float)Screen.width * 0.72f - contentBar.rect.xMin - GetComponent<DisplayGlossaryInfo>().currentIndex * menuManager.contentElementSeparationDistance;
            //float xValue = (float)Screen.width * 1.76f - contentBar.rect.xMin - GetComponent<DisplayGlossaryInfo>().currentIndex * menuManager.contentElementSeparationDistance;
            //float xValue = menuManager.contentElementStartingPoint + Mathf.Abs((displayedGlossaryPosition - rightGlossaryPosition).x) - (GetComponent<DisplayGlossaryInfo>().currentIndex * menuManager.contentElementSeparationDistance);
            //Debug.Log(((float)Screen.width * 0.72f - contentBar.rect.xMin - GetComponent<DisplayGlossaryInfo>().currentIndex * menuManager.contentElementSeparationDistance) + " = " + (Screen.width * 0.72f) + " - " + contentBar.rect.xMin + " - " + (GetComponent<DisplayGlossaryInfo>().currentIndex * menuManager.contentElementSeparationDistance));
            //Debug.Log((menuManager.contentElementStartingPoint - (GetComponent<DisplayGlossaryInfo>().currentIndex * menuManager.contentElementSeparationDistance)) + " = " + menuManager.contentElementStartingPoint + " - " + (GetComponent<DisplayGlossaryInfo>().currentIndex * menuManager.contentElementSeparationDistance));
            //Debug.Log(xValue);
            //contentBar.position = new Vector3(xValue, contentBar.position.y, contentBar.position.z);
            timerMoveAnimation = Time.time;
            Vector3 origin = leftGlossaryPosition;
            if (fromRight) origin = rightGlossaryPosition;
            StartCoroutine(swipeGlossary(0.4f, origin, displayedGlossaryPosition));
        }
    }

    void playHideToLeftAnimation()
    {
        if (!hidden)
        {
            hidden = true;
            SetButtonInteractable(false);
            timerMoveAnimation = Time.time;
            StartCoroutine(swipeGlossary(0.4f, displayedGlossaryPosition, leftGlossaryPosition));
        }
    }

    void playHideToRightAnimation()
    {
        if (!hidden)
        {
            hidden = true;
            SetButtonInteractable(false);
            timerMoveAnimation = Time.time;
            StartCoroutine(swipeGlossary(0.4f, displayedGlossaryPosition, rightGlossaryPosition));
        }
    }

    IEnumerator swipeGlossary(float animDuration, Vector3 origin, Vector3 destination)
    {
        float valueProgression = (Time.time - timerMoveAnimation) / animDuration;
        transform.position = Vector3.Lerp(origin, destination, valueProgression);
        yield return new WaitForSeconds(0.01f);
        if (Time.time - timerMoveAnimation < animDuration) StartCoroutine(swipeGlossary(animDuration, origin, destination));
        else
        {
            transform.position = destination;
            SetButtonInteractable(!hidden);
        }
    }

    public void playCloseAnimation()
    {
        if (!hidden)
        {
            hidden = true;
            SetButtonInteractable(false);
            timerMoveAnimation = Time.time;
            StartCoroutine(hideGlossary(0.3f, hiddenGlossaryPosition));
        }
    }

    IEnumerator hideGlossary(float animDuration, Vector3 destination)
    {
        if (hidden)
        {
            float valueProgression = (Time.time - timerMoveAnimation) / animDuration;
            transform.position = Vector3.Lerp(displayedGlossaryPosition, destination, valueProgression);
            yield return new WaitForSeconds(0.01f);
            if (Time.time - timerMoveAnimation < animDuration) StartCoroutine(hideGlossary(animDuration, destination));
            else
            {
                if (name.Contains("(Clone)")) Destroy(gameObject);
                else transform.position = destination;
            }
        }
    }

    public void goToHiddenPosition()
    {
        hidden = true;
        SetButtonInteractable(false);
        transform.position = hiddenGlossaryPosition;
    }

    public bool isInPlace()
    {
        return (transform.position == displayedGlossaryPosition);
    }
}
