using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InstructionManager : MonoBehaviour {

    AppManager app;
    JSONObject tutorialInstructionsData;
    Text firstSentence, secondSentence;
    int nbSlides = 0, currentSlide;

	// Use this for initialization
	void Start () {
        app = AppManager.appManager;
        if (!app.gameToLaunch.isTutorial) gameObject.SetActive(false);
        else
        {
            currentSlide = 0;
            firstSentence = transform.Find("First Sentence").GetComponent<Text>();
            secondSentence = transform.Find("Second Sentence").GetComponent<Text>();
            tutorialInstructionsData = app.GetComponent<LanguageManager>().tutorialsTexts.GetField(app.gameToLaunch.tutorialName).GetField("Instructions").GetField(app.gameLanguage.ToString());
            nbSlides = tutorialInstructionsData.Count;
            if (nbSlides > 0)
            {
                updateSlide();
            }
            else gameObject.SetActive(false);
        }
	}

    public void proceedToNextSlide() {
        currentSlide++;
        if (currentSlide >= nbSlides) gameObject.SetActive(false);
        else
        {
            updateSlide();
        }
    }

    void updateSlide()
    {
        Debug.Assert(tutorialInstructionsData != null);
        firstSentence.text = tutorialInstructionsData[currentSlide].GetField("firstSentence").str;
        secondSentence.text = tutorialInstructionsData[currentSlide].GetField("secondSentence").str;
    }

}
