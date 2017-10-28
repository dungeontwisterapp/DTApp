using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Language_temp : MonoBehaviour {

    Text displayText;
    AppManager app;

	// Use this for initialization
	void Start () {
        displayText = GetComponent<Text>();
        app = GameObject.FindGameObjectWithTag("App").GetComponent<AppManager>();
	}
	
	// Update is called once per frame
	void Update () {
        switch (app.gameLanguage)
        {
            case Language.french: displayText.text = "Français";
                break;
            case Language.english: displayText.text = "English";
                break;
            case Language.german: displayText.text = "Deutsch";
                break;
            default: Debug.LogError("App Manager, changeLanguage: erreur d'interprétation de la langue actuelle");
                break;
        }
	}
}
