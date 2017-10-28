using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

public enum GlossaryInfoCategory { character, item, room };

public class DisplayGlossaryInfo : MonoBehaviour {

    AppManager app;
    public string currentInfoShown = "Cleric";
    public Text subjectInfo;
    public TextAsset rulesFile;
    public GameObject abilityPrefab;
    public List<string> dictionaryKeys = new List<string>();
    public List<Sprite> dictionaryValues = new List<Sprite>();
    Dictionary<string, Sprite> pictos;
    [HideInInspector]
    public int currentIndex = 0;

	// Use this for initialization
	void Start () {
        app = GameObject.FindGameObjectWithTag("App").GetComponent<AppManager>();
        pictos = new Dictionary<string, Sprite>();
        for (int i = 0; i < dictionaryKeys.Count; i++)
        {
            pictos.Add(dictionaryKeys[i], dictionaryValues[i]);
        }
	}

    public void displayAnotherInfo(int newIndex, string caller, GlossaryInfoCategory category)
    {
        if (caller.ToLower() != currentInfoShown.ToLower())
        {
            GameObject temp = (GameObject)Instantiate(gameObject);
            temp.transform.SetParent(transform.parent, false);
            temp.transform.SetSiblingIndex(transform.parent.childCount-2);
            temp.name = "Glossary Content (Clone)";
            DisplayGlossaryInfo newGlossary = temp.GetComponent<DisplayGlossaryInfo>();
            newGlossary.currentIndex = newIndex;
            newGlossary.currentInfoShown = caller;

            string language = app.gameLanguage.ToString();
            string encodedString = rulesFile.text;
            JSONNode jsonData = JSONNode.Parse(encodedString);
            newGlossary.subjectInfo.text = ((string)jsonData["BaseGame"][caller]["Name"][language]).ToUpper();

            Transform displayContainer = null;
            GameObject displayCharacter = temp.transform.Find("Display character").gameObject;
            displayCharacter.SetActive(false);
            GameObject displayItem = temp.transform.Find("Display item").gameObject;
            displayItem.SetActive(false);
            GameObject displayRoom = temp.transform.Find("Display room").gameObject;
            displayRoom.SetActive(false);

            switch (category)
            {
                case GlossaryInfoCategory.character:
                    displayCharacter.SetActive(true);
                    displayContainer = displayCharacter.transform;
                    break;
                case GlossaryInfoCategory.item:
                    displayItem.SetActive(true);
                    displayContainer = displayItem.transform;
                    break;
                case GlossaryInfoCategory.room:
                    displayRoom.SetActive(true);
                    displayContainer = displayRoom.transform;
                    break;
                default: Debug.LogError("DisplayGlossaryInfo, displayAnotherInfo: L'énumération possède une valeur inconnue");
                    break;
            }
            Image[] otherSprites = displayContainer.GetComponentsInChildren<Image>();
            foreach (Image sprite in otherSprites)
            {
                if (sprite.gameObject.name != "Cadre" && sprite.gameObject.name != "Reflet") sprite.enabled = false;
            }
            displayContainer.Find(caller).GetComponent<Image>().enabled = true;

            int nbAbilities = jsonData["BaseGame"][caller]["Abilities"].AsArray.Count;
            Transform abilitiesContainer = temp.transform.Find("Abilities");
            if (abilitiesContainer.childCount > 1)
            {
                for (int i = 1; i < abilitiesContainer.childCount; i++)
                {
                    Destroy(abilitiesContainer.GetChild(i).gameObject);
                }
            }
            Transform firstAbility = abilitiesContainer.Find("Ability");
            for (int i = 0; i < nbAbilities; i++)
            {
                if (i == 0)
                {
                    temp.transform.Find("Abilities/Ability/Description").GetComponent<Text>().text = jsonData["BaseGame"][caller]["Abilities"][i]["Ability"][language]["text"];
                    string key = jsonData["BaseGame"][caller]["Abilities"][0]["Ability"][language]["icon"];
                    Sprite value;
                    Image picto = temp.transform.Find("Abilities/Ability/Picto").GetComponent<Image>();
                    if (pictos.TryGetValue(key, out value))
                    {
                        picto.sprite = value;
                        picto.enabled = true;
                    }
                    else picto.enabled = false;
                }
                else
                {
                    GameObject tempMenu = (GameObject)Instantiate(abilityPrefab);
                    tempMenu.transform.SetParent(abilitiesContainer);
                    tempMenu.transform.position = firstAbility.position;
                    tempMenu.transform.Translate(Vector3.down * Screen.height * 0.15f);
                    tempMenu.transform.localScale = new Vector3(1, 1, 1);
                    tempMenu.transform.Find("Description").GetComponent<Text>().text = jsonData["BaseGame"][caller]["Abilities"][i]["Ability"][language]["text"];
                    string key = jsonData["BaseGame"][caller]["Abilities"][i]["Ability"][language]["icon"];
                    Sprite value;
                    Image picto = tempMenu.transform.Find("Picto").GetComponent<Image>();
                    if (pictos.TryGetValue(key, out value))
                    {
                        picto.sprite = value;
                        picto.enabled = true;
                    }
                    else picto.enabled = false;
                }
            }

            if (newIndex > currentIndex)
            {
                SendMessage("playHideToLeftAnimation");
                temp.SendMessage("playDisplayFromSideAnimation", true);
            }
            else
            {
                SendMessage("playHideToRightAnimation");
                temp.SendMessage("playDisplayFromSideAnimation", false);
            }
            Invoke("cleanScene", 1.0f);
        }
    }

    void cleanScene()
    {
        if (name.Contains("(Clone)")) Destroy(gameObject);
        else GetComponent<GlossaryAnimation>().goToHiddenPosition();
    }
}
