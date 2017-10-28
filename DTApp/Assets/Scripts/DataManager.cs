using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DataManager : MonoBehaviour {

    public TextAsset rulesFile;
    public List<GameObject> playerTypes = new List<GameObject>();
    public List<GameObject> rooms = new List<GameObject>();
    public List<GameObject> startingLines = new List<GameObject>();
    public List<GameObject> characters = new List<GameObject>();
    public List<GameObject> items = new List<GameObject>();
    public List<Sprite> tokenBacks = new List<Sprite>();
    public GameObject tileBack;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public GameObject getStandardPlayer()
    {
        return playerTypes[0];
    }

    public GameObject getAIPlayer()
    {
        return playerTypes[1];
    }

    public GameObject getOnlinePlayer()
    {
        return playerTypes[2];
    }

    public GameObject getScriptedPlayer()
    {
        return playerTypes[3];
    }

    public int getCharactersFullSprites(string characterCode)
    {
        switch (characterCode)
        {
            case "yellow": return 0;
            case "blue": return 1;
            case "yellow_pirate": return -1;
            case "blue_pirate": return -1;
            default: return -1;
        }
    }

    public GameObject getTokenPrefab(string tokenName)
    {
        foreach (GameObject chara in characters)
        {
            if (chara.name.Split('_')[1].ToLower() == tokenName.ToLower()) return chara;
        }
        foreach (GameObject item in items)
        {
            if (item.name.Split('_')[1].ToLower() == tokenName.ToLower()) return item;
        }
        Debug.LogError("DataManager, getTokenPrefab: Préfab du token " + tokenName + " introuvable");
        return null;
    }

    public GameObject getRoomByName(string tileName)
    {
        foreach (GameObject room in rooms)
        {
            if (room.GetComponent<TileBehavior>().getTileName() == tileName) return room;
        }
        Debug.LogError("DataManager, getRoomByName: Préfab de la salle " + tileName + " introuvable");
        return null;
    }

}
