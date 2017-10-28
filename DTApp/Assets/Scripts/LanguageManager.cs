using UnityEngine;
using System.Collections;

public class LanguageManager : MonoBehaviour {

    public TextAsset menusFile;
    public TextAsset rulesFile;
    public TextAsset tutorialsFile;

    public JSONObject tutorialsTexts { get { return JSONObject.Create(tutorialsFile.text); } }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
