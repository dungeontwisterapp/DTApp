using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using SimpleJSON;
using System.IO;

public class DisplayRules : MonoBehaviour {

	GameManager gManager;

	public GameObject rulesExcerpt;
	public TextAsset TextFile;
	public string fileName = "Rules.json";
	string tokenName;

    void Start()
    {
        gManager = GameManager.gManager;
	}

	public void displayCurrentSelection_sRules () {
		tokenName = gManager.actionCharacter.name.Split('_')[1];
		rulesExcerpt.transform.Find("Name").GetComponent<Text>().text = tokenName;
		rulesExcerpt.transform.Find("Extension").GetComponent<Text>().text = "Jeu de base";
		rulesExcerpt.transform.Find("Sketch").GetComponent<Image>().sprite = gManager.actionCharacter.GetComponent<CharacterBehaviorIHM>().fullCharacterSprite;
		rulesExcerpt.transform.Find("Rules text").GetComponent<Text>().text = "";
		rulesExcerpt.SetActive(true);

        string encodedString = TextFile.text;
        if (gManager.updatedRulesAvailable)
        {
            StreamReader sr = new StreamReader(fileName);
            encodedString = sr.ReadToEnd();
        }
        JSONNode jsonData = JSONNode.Parse(encodedString);
        rulesExcerpt.transform.Find("Rules text").GetComponent<Text>().text = jsonData["BaseGame"][tokenName]["Abilities"][0]["Ability"]["French"]["text"];

	}

	string readTextFileLines(string selector) {
		string text = "";
		string[] linesInFile = TextFile.text.Split('\n');
		
		foreach (string line in linesInFile) {
			//Debug.Log(line);
			text += line;
		}
		return text;
	}

}
