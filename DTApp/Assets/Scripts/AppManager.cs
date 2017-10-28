using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public enum Language {french, english, german};

public class AppManager : MonoBehaviour {

    public static AppManager appManager;
    string applicationDataEndPath = "/appdata.dat";

    [HideInInspector]
    public DataManager data;
    public GameSpecs gameToLaunch;
    public Language gameLanguage;
    public TextAsset bgaTableData;
    public JSONObject bgaData;
    public Multi.Interface onlineGameInterface;

    public bool firstLaunchTutorialComplete = false;
    public string currentTutorialLevel = "tuto01";
    public bool musicOn = true;
    public bool soundOn = true;
    public string bgaid = null;
    public string bgapwd = null; // WARNING: remove before release
    public bool bgaRememberLogin = true;
    //AppData persistentData;

    public string currentMenuOpened = "N/A";
    public bool onlineWaiting = false;
    private GameState gameSaved;

    bool initializationDone = false;

    void Awake()
    {
        if (appManager == null)
        {
            appManager = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (appManager != this) Destroy(gameObject);
    }

	// Use this for initialization
    void Start()
    {
        data = GetComponent<DataManager>();
        //persistentData = new AppData();
        bgaData = new JSONObject(bgaTableData.text);
        gameToLaunch = new GameSpecs();
        onlineGameInterface = new Multi.Interface();
        gameSaved = new GameState();

#if !UNITY_EDITOR || PERSISTENT_DATA
        loadAppData();
#endif
        if (musicOn) GameObject.FindGameObjectWithTag("BGM").GetComponent<AudioSource>().Play();
        else
        {
            Transform musicButtons = GameObject.Find("MenuUI").transform.Find("Options menu/Music slider");
            musicButtons.Find("On").gameObject.SetActive(false);
            musicButtons.Find("Off").gameObject.SetActive(true);
        }

        initializationDone = true;
    }

    void initGameSpecs()
    {
        gameToLaunch.resetSpecsValues();
    }

    public void loadTEST_IA()
    {
        gameToLaunch.opponentIsAI = true;

        List<int> roomNumbers = new List<int>();
        roomNumbers.Add(0);
        roomNumbers.Add(1);
        loadCustomRoomSet(roomNumbers);

        List<string> characterNames = new List<string>();
        characterNames.Add("CT_Goblin");
        characterNames.Add("CT_Warrior");
        loadCharacterSet(characterNames);

        List<string> itemsNames = new List<string>();
        itemsNames.Add("IT_Rope");
        itemsNames.Add("IT_Treasure");
        loadItemSet(itemsNames);

        // Load character sprites skins
        gameToLaunch.spritesChosen[0] = "yellow";
        gameToLaunch.spritesChosen[1] = "blue";
    }

    public void loadBaseGame()
    {
        loadBaseGameRooms();
        loadBaseGameCharacters();
        loadBaseGameItems();

        // Load character sprites skins
        gameToLaunch.spritesChosen[0] = "yellow";
        gameToLaunch.spritesChosen[1] = "blue";
    }

    public void loadTutorial(string tutoName)
    {
        gameToLaunch.tutorialName = tutoName;

        // Load character sprites skins
        gameToLaunch.spritesChosen[0] = "yellow";
        gameToLaunch.spritesChosen[1] = "blue";
    }

    public void tutorial(bool isTutorial)
    {
        gameToLaunch.isTutorial = isTutorial;
        gameToLaunch.doBoardSetup = !isTutorial;
    }

    public void loadGameFromData()
    {
        gameToLaunch.loadExistingGame = true;
        gameToLaunch.doBoardSetup = false;
    }

    void loadBaseGameRooms() { loadRoomSet(0); }

    void loadBaseGameCharacters()
    {
        List<string> characterNames = new List<string>();
        characterNames.Add("CT_Cleric");
        characterNames.Add("CT_Goblin");
        characterNames.Add("CT_Mechanork");
        characterNames.Add("CT_Thief");
        characterNames.Add("CT_Troll");
        characterNames.Add("CT_Wall-Walker");
        characterNames.Add("CT_Warrior");
        characterNames.Add("CT_Wizard");
        loadCharacterSet(characterNames);
    }

    void loadBaseGameItems()
    {
        List<string> itemsNames = new List<string>();
        itemsNames.Add("IT_Armor");
        itemsNames.Add("IT_FireballWand");
        itemsNames.Add("IT_Rope");
        itemsNames.Add("IT_SpeedPotion");
        itemsNames.Add("IT_Sword");
        itemsNames.Add("IT_Treasure");
        loadItemSet(itemsNames);
    }

    void loadPaladinsAndDragonsRooms() { loadRoomSet(8); }

    void loadPaladinsAndDragonsCharacters()
    {
        List<string> characterNames = new List<string>();
        characterNames.Add("CT_ElfScout");
        characterNames.Add("CT_Illusionist");
        characterNames.Add("CT_Ghost");
        characterNames.Add("CT_Golem");
        characterNames.Add("CT_Paladin");
        characterNames.Add("CT_Pickpocket");
        characterNames.Add("CT_RedDragon");
        characterNames.Add("CT_WeaponMaster");
        loadCharacterSet(characterNames);
    }

    void loadPaladinsAndDragonsItems()
    {
        List<string> itemsNames = new List<string>();
        itemsNames.Add("IT_CharmScroll");
        itemsNames.Add("IT_DragonSlayer");
        itemsNames.Add("IT_FireShield");
        itemsNames.Add("IT_Key");
        itemsNames.Add("IT_Rope");
        itemsNames.Add("IT_TeleportationRing");
        loadItemSet(itemsNames);
    }

    void loadCharacterSet (List<string> characterNames)
    {
        List<GameObject> characters = data.characters;
        gameToLaunch.charactersToLoad.Clear();
        foreach (GameObject chara in characters)
        {
            if (characterNames.Contains(chara.name)) gameToLaunch.charactersToLoad.Add(chara);
        }
    }

    void loadItemSet(List<string> itemNames)
    {
        List<GameObject> items = data.items;
        gameToLaunch.itemsToLoad.Clear();
        foreach (GameObject item in items)
        {
            if (itemNames.Contains(item.name)) gameToLaunch.itemsToLoad.Add(item);
        }
    }

    void loadRoomSet(int firstRoomNumber)
    {
        List<GameObject> rooms = data.rooms;
        gameToLaunch.roomsToLoad.Clear();
        for (int i = firstRoomNumber; i < firstRoomNumber+8; i++)
        {
            gameToLaunch.roomsToLoad.Add(rooms[i]);
        }
    }

    void loadCustomRoomSet(List<int> roomNumbers)
    {
        List<GameObject> rooms = data.rooms;
        gameToLaunch.roomsToLoad.Clear();
        foreach (int index in roomNumbers)
        {
            gameToLaunch.roomsToLoad.Add(rooms[index]);
        }
    }

    public void playOrStopBGM(bool playMusic)
    {
        musicOn = playMusic;
        AudioSource currentMusic = GameObject.FindGameObjectWithTag("BGM").GetComponent<AudioSource>();
        if (playMusic)
        {
            if (!currentMusic.isPlaying) currentMusic.Play();
        }
        else currentMusic.Stop();
    }

    public void changeLanguage (bool nextLanguage)
    {
        switch (gameLanguage)
        {
            case Language.french :
                if (nextLanguage) gameLanguage = Language.english;
                else gameLanguage = Language.german;
                break;
            case Language.english:
                if (nextLanguage) gameLanguage = Language.german;
                else gameLanguage = Language.french;
                break;
            case Language.german:
                if (nextLanguage) gameLanguage = Language.french;
                else gameLanguage = Language.english;
                break;
            default:
                Debug.LogError("App Manager, changeLanguage: erreur d'interprétation de la langue actuelle");
                break;
        }
    }

    public void LoadGame()
    {
        if (onlineGameInterface.isOnline)
        {
            onlineGameInterface.LoadGame();
        }
        else
        {
            Debug.Assert(gameSaved != null);
            if (gameSaved.isGameSaved()) gameSaved.loadGameState();
        }
    }

    public void SaveGame()
    {
        Debug.Assert(gameSaved != null);
        gameSaved.saveGameState();
        saveAppData();
    }

    public void deleteSavedGame()
    {
        gameSaved = new GameState();
        Debug.Log("Save deleted");
        saveAppData();
    }

    public void saveAppData()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + applicationDataEndPath);

        AppData saveData = new AppData(gameLanguage, firstLaunchTutorialComplete, currentTutorialLevel, musicOn, bgaid, bgapwd, bgaRememberLogin, gameSaved);

        bf.Serialize(file, saveData);
        file.Close();
    }

    public void loadAppData()
    {
        if (File.Exists(Application.persistentDataPath + applicationDataEndPath))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + applicationDataEndPath, FileMode.Open);

            AppData loadData = (AppData) bf.Deserialize(file);
            file.Close();

            gameLanguage = loadData.selectedLanguage;
            firstLaunchTutorialComplete = loadData.tutorialCompleted;
            currentTutorialLevel = loadData.tutorialLevel;
            musicOn = loadData.music;
            bgaid = loadData.bgaid;
            bgapwd = loadData.bgapwd;
            bgapwd = loadData.bgapwd;
            gameSaved = (loadData.localGame != null) ? loadData.localGame : new GameState();
        }
    }

    // Fonction Windows Phone au cas où
#if UNITY_WSA
    void OnApplicationFocus(bool isFocused)
    {
        // Si perte de focus de l'application
        if (!isFocused) appDataAutosave();
    }
#endif

    void OnApplicationPause()
    {
        if (initializationDone) appDataAutosave();
    }

    void OnApplicationQuit()
    {
        if (initializationDone) appDataAutosave();
    }

    void appDataAutosave()
    {
        if (appManager == this) saveAppData();
    }
}

[Serializable]
class AppData
{
    public Language selectedLanguage;
    public bool tutorialCompleted;
    public string tutorialLevel;
    public bool music;
    public string bgaid;
    public string bgapwd; // WARNING: remove before release
    public bool bgaRememberLogin;
    public GameState localGame;

    public AppData(Language lang, bool tutorialStatus, string tuturialToLoad, bool musicStatus, string bga_id, string bga_pwd, bool bga_mem, GameState gameToSave)
    {
        selectedLanguage = lang;
        tutorialCompleted = tutorialStatus;
        tutorialLevel = tuturialToLoad;
        music = musicStatus;
        bgaid = bga_id;
        bgapwd = bga_pwd;
        bgaRememberLogin = bga_mem;
        localGame = gameToSave;
    }
}
