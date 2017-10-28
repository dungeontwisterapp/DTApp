using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameSpecs {

    public List<GameObject> roomsToLoad = new List<GameObject>();
    public List<GameObject> charactersToLoad = new List<GameObject>();
    public List<GameObject> itemsToLoad = new List<GameObject>();

    public bool standardGameLayout = true;
    public bool doBoardSetup = true;
    public string tutorialName;
    public bool timedGame = false;
    public bool opponentIsAI = false;
    public bool isTutorial = false;
    public bool loadExistingGame = false;

    public string[] spritesChosen = new string[2];

    public void resetSpecsValues ()
    {
        roomsToLoad.Clear();
        charactersToLoad.Clear();
        itemsToLoad.Clear();

        standardGameLayout = true;
        doBoardSetup = true;
        timedGame = false;
        opponentIsAI = false;
        isTutorial = false;
        loadExistingGame = false;
    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
