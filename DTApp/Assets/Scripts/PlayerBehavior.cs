using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PlayerBehavior : MonoBehaviour {

    protected GameManager gManager;
	protected GameObject cardsPanel;
    protected List<GameObject> actionCardsOnPanel = new List<GameObject>();
    protected List<GameObject> jumpCardsOnPanel = new List<GameObject>();
    protected List<GameObject> combatCardsOnPanel = new List<GameObject>();

    [HideInInspector]
    public string onlineID;
    [HideInInspector]
    public string playerName;
    public int index = 0;
	public int victoryPoints = 0;
	public int nbSauts = 3;
	public bool[] usedActionCards;
	public bool[] combatCardsAvailable;
	public List<GameObject> combatCards = new List<GameObject>();
	public Color playerColor = Color.white;
	Text textVP;
	public float tempsRestant;
	float tempsRestantBackup;
    bool timeUp = false;
    public bool myTurn = false;


    void Awake()
    {
        initialization();
    }

	// Use this for initialization
    void Start()
    {
        GameObject victoryPointsDisplay = GameObject.Find("VP-" + (index + 1));
        if (victoryPointsDisplay != null) textVP = victoryPointsDisplay.GetComponent<Text>();
        else Debug.LogWarning("PlayerBehavior, Start: victoryPointsDisplay VP-" + (index + 1) + " not found");
        assignUIPanels();
	}

    public virtual void initialization()
    {
        gManager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameManager>();
        usedActionCards = new bool[4];
        usedActionCards[0] = usedActionCards[1] = usedActionCards[2] = usedActionCards[3] = false;
        combatCardsAvailable = new bool[gManager.combatCards.Length];
        for (int i = 0; i < combatCardsAvailable.Length; i++)
        {
            combatCardsAvailable[i] = true;
        }
    }

    protected void assignUIPanels() {
        if (index == 0)
        {
            playerColor = new Color(0.8f, 0.4f, 0.2f);
            GameObject leftSide = GameObject.Find("Left side swipe");
            if (leftSide != null) cardsPanel = leftSide.transform.Find("Cartes").gameObject;
            else Debug.LogWarning("PlayerBehavior, assignUIPanels: Le panneau d'affichage de " + gameObject.name + " n'a pas été trouvé");
        }
        else
        {
            playerColor = new Color(0.5f, 0.5f, 0.9f);
            GameObject rightSide = GameObject.Find("Right side swipe");
            if (rightSide != null) cardsPanel = rightSide.transform.Find("Cartes").gameObject;
            else Debug.LogWarning("PlayerBehavior, assignUIPanels: Le panneau d'affichage de " + gameObject.name + " n'a pas été trouvé");
        }
		if (cardsPanel != null) {
			actionCardsOnPanel.Add(cardsPanel.transform.Find("2 actions").gameObject);
			actionCardsOnPanel.Add(cardsPanel.transform.Find("3 actions").gameObject);
			actionCardsOnPanel.Add(cardsPanel.transform.Find("4 actions").gameObject);
			actionCardsOnPanel.Add(cardsPanel.transform.Find("5 actions").gameObject);
			jumpCardsOnPanel.Add(cardsPanel.transform.Find("Saut 1").gameObject);
			jumpCardsOnPanel.Add(cardsPanel.transform.Find("Saut 2").gameObject);
			jumpCardsOnPanel.Add(cardsPanel.transform.Find("Saut 3").gameObject);
			combatCardsOnPanel.Add(cardsPanel.transform.Find("Combat +0").gameObject);
			combatCardsOnPanel.Add(cardsPanel.transform.Find("Combat +1").gameObject);
			combatCardsOnPanel.Add(cardsPanel.transform.Find("Combat +2").gameObject);
			combatCardsOnPanel.Add(cardsPanel.transform.Find("Combat +2 bis").gameObject);
			combatCardsOnPanel.Add(cardsPanel.transform.Find("Combat +3").gameObject);
			combatCardsOnPanel.Add(cardsPanel.transform.Find("Combat +3 bis").gameObject);
			combatCardsOnPanel.Add(cardsPanel.transform.Find("Combat +4").gameObject);
			combatCardsOnPanel.Add(cardsPanel.transform.Find("Combat +5").gameObject);
			combatCardsOnPanel.Add(cardsPanel.transform.Find("Combat +6").gameObject);
		}
    }

    public virtual void startGame()
    {
        myTurn = true;
    }
	
	// Update is called once per frame
	void Update () {
        updateTimeAndCards();
    }

    protected void updateTimeAndCards() {
		// Si c'est au tour du joueur, faire descendre le tempsRestant
		if (gManager.timedGame && gManager.activePlayer == this && gManager.startTurn) {
			tempsRestant -= Time.deltaTime;
			if (tempsRestant <= 0) {
                if (gManager.gameMacroState != GameProgression.Playing)
                {
                    if (gManager.gameMacroState == GameProgression.TokenPlacement) endStartingCharactersPlacement();
					else {
						if (!timeUp) {
							timeUp = true;
							placeOneTokenAtRandom();
						}
					}
				}
				else {
					if (tempsRestantBackup != 0) {
						// TODO
					}
					else gManager.actionPoints = 0;
				}
			}
		}

		if (usedActionCards[0] && usedActionCards[1] && usedActionCards[2] && usedActionCards[3]) {
			usedActionCards[0] = usedActionCards[1] = usedActionCards[2] = usedActionCards[3] = false;
		}

		if (cardsPanel != null) {
            for (int i = 2; i >= 0; i--)
            {
                jumpCardsOnPanel[i].GetComponent<Image>().sprite = (i >= nbSauts) ? jumpCardsOnPanel[i].GetComponent<InfoCardPanel>().usedState : jumpCardsOnPanel[i].GetComponent<InfoCardPanel>().standardState;
            }
			for (int i=0 ; i < combatCardsOnPanel.Count ; i++) {
                combatCardsOnPanel[i].GetComponent<Image>().sprite = (combatCardsAvailable[i]) ? combatCardsOnPanel[i].GetComponent<InfoCardPanel>().standardState : combatCardsOnPanel[i].GetComponent<InfoCardPanel>().usedState;
            }
            for (int i = 0; i < actionCardsOnPanel.Count; i++)
            {
                actionCardsOnPanel[i].GetComponent<Image>().sprite = (usedActionCards[i]) ? actionCardsOnPanel[i].GetComponent<InfoCardPanel>().usedState : actionCardsOnPanel[i].GetComponent<InfoCardPanel>().standardState;
            }
		}
	}
	
	void OnGUI () {
        if (textVP != null)
        {
            textVP.fontSize = Screen.height / 20;
            textVP.text = victoryPoints.ToString();
        }
		if (gManager.timedGame) {
			// Si c'est au tour du joueur
			if (gManager.activePlayer == this) {
				if (tempsRestant >= 0) {
					string min, sec;
					int minutes = Mathf.FloorToInt(tempsRestant/60);
					int seconds = Mathf.CeilToInt(tempsRestant)%60;
					if (minutes < 10) min = "0" + minutes.ToString();
					else min = minutes.ToString();
					if (seconds < 10) sec = "0" + seconds.ToString();
					else sec = seconds.ToString();
					gManager.leftTimer.text = min;
					gManager.rightTimer.text = sec;
				}
				else {
					gManager.leftTimer.text = "00";
					gManager.rightTimer.text = "00";
				}
			}
		}
	}

    // Au tour du joueur
    public virtual void myTurnToPlay()
    {
        myTurn = true;
    }

    public void renewActionCards()
    {
        if (usedActionCards[0] && usedActionCards[1] && usedActionCards[2] && usedActionCards[3])
        {
            usedActionCards[0] = usedActionCards[1] = usedActionCards[2] = usedActionCards[3] = false;
        }
        else Debug.LogError("PlayerBehavior, renewActionCards: Il reste des cartes d'action non utilisées");
    }

	public void setShortTimer (float time) {
		tempsRestantBackup = tempsRestant;
		tempsRestant = time;
	}

	public void restoreStandardTimer () {
		tempsRestant = tempsRestantBackup;
		tempsRestantBackup = 0;
	}

    public void endStartingCharactersPlacement()
    {
        GameObject[] tokens = GameObject.FindGameObjectsWithTag("Token");
        List<GameObject> characters = new List<GameObject>();
        List<CharacterBehaviorIHM> myFreeCharacters = new List<CharacterBehaviorIHM>();
        foreach (GameObject t in tokens)
        {
            if (t.GetComponent<CharacterBehavior>() != null) characters.Add(t);
        }
        foreach (GameObject c in characters)
        {
            if (c.GetComponent<CharacterBehavior>().affiliationJoueur == gameObject && c.GetComponent<CharacterBehavior>().cibleToken == null)
                myFreeCharacters.Add(c.GetComponent<CharacterBehaviorIHM>());
        }
        GameObject[] cibles = GameObject.FindGameObjectsWithTag("PlacementToken");
        int index;
        if (myFreeCharacters.Count >= cibles.GetLength(0))
        {
            for (int i = 0; i < cibles.GetLength(0); i++)
            {
                if (cibles[i].GetComponent<PlacementTokens>().tokenAssociated == null)
                {
                    index = UnityEngine.Random.Range(0, myFreeCharacters.Count);
                    myFreeCharacters[index].placeToken(cibles[i]);
                    myFreeCharacters[index].tokenPutOnBoard();
                    myFreeCharacters.Remove(myFreeCharacters[index]);
                }
            }
        }
        else
        {
            int spacesAvailable = cibles.GetLength(0);
            List<int> indexes = new List<int>();
            foreach (CharacterBehaviorIHM chara in myFreeCharacters)
            {
                do
                {
                    index = UnityEngine.Random.Range(0, spacesAvailable);
                } while (indexes.Contains(index));

                indexes.Add(index);
                chara.placeToken(cibles[index]);
                chara.tokenPutOnBoard();
            }
        }
        gManager.placeStartingCharacters();
    }

	public void placeOneTokenAtRandom () {
		GameObject[] tokens = GameObject.FindGameObjectsWithTag("Token");
		List<TokenIHM> myFreeTokens = new List<TokenIHM>();
		foreach (GameObject token in tokens) {
			Token t = token.GetComponent<Token>();
			if (t.affiliationJoueur == gameObject && t.cibleToken == null && !t.tokenPlace)
				myFreeTokens.Add(t.GetComponent<TokenIHM>());
		}
		GameObject[] cibles = GameObject.FindGameObjectsWithTag("PlacementToken");
		int indexToken = UnityEngine.Random.Range(0, myFreeTokens.Count);
		int indexCible = UnityEngine.Random.Range(0, cibles.GetLength(0));
		while (cibles[indexCible].GetComponent<PlacementTokens>().tokenAssociated != null) { // WARNING: possible infinite loop
			indexCible = UnityEngine.Random.Range(0, cibles.GetLength(0));
		}
		myFreeTokens[indexToken].placeToken(cibles[indexCible]);
	}

	public void updatePanelAppearance () {
		if (cardsPanel != null) {
			// Cartes action
			for (int i=0 ; i < 4 ; i++) {
				if (usedActionCards[i]) actionCardsOnPanel[i].GetComponent<Image>().sprite = actionCardsOnPanel[i].GetComponent<InfoCardPanel>().usedState;
				else actionCardsOnPanel[i].GetComponent<Image>().sprite = actionCardsOnPanel[i].GetComponent<InfoCardPanel>().standardState;
			}
		}
	}

	public int getAvailableCombatCardsNumber () {
		int nbCards = 0;
		for (int i=0 ; i < combatCardsAvailable.GetLength(0) ; i++) {
			if (combatCardsAvailable[i]) nbCards++;
		}
		return nbCards;
	}

    public virtual bool isOnlinePlayer()
    {
        return false;
    }

    public virtual bool isScriptedPlayer()
    {
        return false;
    }


}
