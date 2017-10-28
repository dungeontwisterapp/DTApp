using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// Handle all combat related game management
public class CombatManager
{
    private enum Status { NONE, LOADING, LAUNCH, WAITING, CARDS_PLAYED, CARDS_REVEALED, RESOLUTION }
    private Status status;
    private List<CharacterBehavior> fighters;
    private int[] combatCardsValue = new int[2];
    private const int CARD_NOT_SET = -2;
    private const int CARD_UNKNOWN = -1;
    private GameManager gManager;
    private GameObject cartesP1, cartesP2;

    //// accessors related to card values ////
    public bool isCardPlayed(int playerIndex) { return combatCardsValue[playerIndex] != CARD_NOT_SET; }
    public bool isCardRevealed(int playerIndex) { return isCardPlayed(playerIndex) && combatCardsValue[playerIndex] != CARD_UNKNOWN; }
    public int getCardValue(int playerIndex) { return isCardRevealed(playerIndex) ? combatCardsValue[playerIndex] : 0; }
    public int cardPlayedCount { get { return (isCardPlayed(0) ? 1 : 0) + (isCardPlayed(1) ? 1 : 0); } }
    public int cardRevealedCount { get { return (isCardRevealed(0) ? 1 : 0) + (isCardRevealed(1) ? 1 : 0); } }
    //// accessors related to status ////
    public bool fightOnGoing { get { return status != Status.NONE; } }
    public bool combatCardsReadyToReveal { get { return status == Status.CARDS_PLAYED && cardRevealedCount == 2; } }
    public bool waitingForPlayerCard { get { return status == Status.WAITING && cardPlayedCount < 2; } }
    //// card values modifiers ////
    public void resetCombatStatus() { Debug.LogWarning("resetCombatStatus"); combatCardsValue[0] = combatCardsValue[1] = CARD_NOT_SET; status = Status.NONE; fighters = null; }
    public void setHiddenCard(int playerIndex) { Debug.LogWarning("play card of player" + playerIndex); Debug.Assert(!isCardPlayed(playerIndex)); combatCardsValue[playerIndex] = CARD_UNKNOWN; }
    public void revealCard(int playerIndex, int value) { Debug.LogWarning("reaveal card " + value + " of player" + playerIndex); Debug.Assert(!isCardRevealed(playerIndex)); combatCardsValue[playerIndex] = value; }

    public CombatManager(GameManager gManager)
    {
        this.gManager = gManager;
        resetCombatStatus();
    }

    public void initCards()
    {
        cartesP1 = new GameObject("(Dynamic) Cartes combat Player 1");
        cartesP2 = new GameObject("(Dynamic) Cartes combat Player 2");
        cartesP1.SetActive(false);
        cartesP2.SetActive(false);
        resetCombatStatus();
    }

    // start combat on load (online game only)
    public void preloadFight(CharacterBehavior enemy) // on load
    {
        Debug.Assert(status == Status.NONE, "Fight already started");
        status = Status.LOADING;

        fighters = new List<CharacterBehavior>() { enemy }; // the fighters list will be filled during loadFight
    }
    public void loadFight() // on start
    {
        Debug.Assert(status == Status.LOADING, "Fight not preloaded");
        status = Status.NONE;

        CharacterBehavior enemy = fighters[0];
        fighters = null;
        CharacterBehavior currentChar = gManager.actionCharacter.GetComponent<CharacterBehavior>();
        initFight(currentChar, enemy);
    }

    // start combat
    public void combat(GameObject enemyObject)
    {
        Debug.Assert(status == Status.NONE, "Fight already started");

        CharacterBehavior enemy = enemyObject.GetComponent<CharacterBehavior>();
        if (!enemy.freshlyWounded)
        {
            CharacterBehavior currentChar = gManager.actionCharacter.GetComponent<CharacterBehavior>();
            gManager.onlineGameInterface.RecordAction(ActionType.ATTACK, currentChar, enemy);
            initFight(currentChar, enemy);
        }
    }

    public bool IsChoosingCardOnLoad()
    {
        // Warning: this test is only valid before the card is actually played, use causiously
        return (gManager.onlineGame && isCardRevealed(gManager.activePlayer.index)); // loading when current player has already played
    }

    // Faire apparaitre les cartes combat disponibles
    public void instanciateCurrentPlayerCombatCards()
    {
        Debug.Assert(waitingForPlayerCard);

        ChooseCombatCardScreen combatCardsLayout = combatDisplay.chooseCombatCardLayout;
        combatCardsLayout.gameObject.SetActive(true);
        combatCardsLayout.leftCombatValue.text = combatDisplay.leftTotal.text;
        combatCardsLayout.rightCombatValue.text = combatDisplay.rightTotal.text;

        if (!gManager.onlineGameInterface.isOnlineOpponent(gManager.activePlayer.index))
        {
            if (IsChoosingCardOnLoad())
            {
                InstanciateAndPlayCombatCard(getCardValue(gManager.activePlayer.index));
            }
            else
            {
                GameObject cards = createCardsHolder();
                for (int i = 0; i < combatCardsLayout.combatCardsPrefabs.Length; i++)
                {
                    instantiateCardFromPrefab(cards, combatCardsLayout.combatCardsPrefabs[i], gManager.activePlayer.combatCardsAvailable[i]);
                }
                if (gManager.activePlayer.isScriptedPlayer()) AutoSelectCardForScriptedPlayer(cards);
            }
        }

        if (!gManager.activePlayer.isScriptedPlayer()) 
        {
            combatCardsLayout.fadeInScreen();
            combatCardsLayout.BroadcastMessage("launchFadeIn");
        }
    }

    public void InstanciateAndPlayCombatCard(int cardCombatValue)
    {
        GameObject cardToPlay = gManager.combatManager.instanciateOneCombatCard(cardCombatValue);
        cardToPlay.transform.rotation = Quaternion.Euler(0, 90, 0); // hide card
        combatDisplay.chooseCombatCardLayout.combatCardChosen(cardToPlay);
    }

    private GameObject instanciateOneCombatCard(int cardCombatValue)
    {
        Debug.Assert(waitingForPlayerCard);

        ChooseCombatCardScreen combatCardsLayout = combatDisplay.chooseCombatCardLayout;
        for (int i = 0; i < combatCardsLayout.combatCardsPrefabs.Length; i++)
        {
            CombatCards prefab = combatCardsLayout.combatCardsPrefabs[i].GetComponent<CombatCards>();
            if (gManager.activePlayer.combatCardsAvailable[i] && prefab.combatValue == cardCombatValue && !prefab.doublon)
            {
                return instantiateCardFromPrefab(createCardsHolder(), combatCardsLayout.combatCardsPrefabs[i], gManager.activePlayer.combatCardsAvailable[i]).gameObject;
            }
        }
        Debug.LogError("instanciateOneCombatCard: Card not available");
        return null;
    }

    public void combatCardChosen(GameObject card)
    {
        Debug.LogWarning("combatCardChosen");
        Debug.Assert(waitingForPlayerCard);

        CombatCards cardData = card.GetComponent<CombatCards>();
        if (!IsChoosingCardOnLoad())
        {
            gManager.onlineGameInterface.RecordCard(ActionType.COMBAT_CARD, cardData.combatValue);
            revealCard(cardData.indexCard, cardData.combatValue);
        }
        if (cardPlayedCount == 1)
        {
            gManager.switchPlayer();
        }
        else
        {
            status = Status.CARDS_PLAYED;
            combatDisplay.chooseCombatCardLayout.fadeOutScreen();
        }
    }

    // Attente de la fin des animations de sÃ©lection de carte de combat
    public IEnumerator resolveCombat(float time)
    {
        Debug.LogWarning("resolveCombat animation");
        Debug.Assert(combatCardsReadyToReveal);
        status = Status.CARDS_REVEALED;

        string leftFinalValue = GetFighterGroupFinalCombatValue(0).ToString();
        string rightFinalValue = GetFighterGroupFinalCombatValue(1).ToString();
        List<Text> textDisplays = new List<Text>();
        textDisplays.Add(combatDisplay.leftTotal);
        textDisplays.Add(combatDisplay.rightTotal);
        textDisplays.Add(combatDisplay.chooseCombatCardLayout.leftCombatValue);
        textDisplays.Add(combatDisplay.chooseCombatCardLayout.rightCombatValue);
        Color startColor = textDisplays[0].color, highlightColor = Color.white;
        for (float i = 0.1f; i < 1; i += 0.1f)
        {
            foreach (Text t in textDisplays)
            {
                t.color = Color.Lerp(startColor, highlightColor, i);
            }
            yield return new WaitForSeconds(0.025f);
        }
        combatDisplay.leftTotal.text = leftFinalValue;
        combatDisplay.rightTotal.text = rightFinalValue;
        combatDisplay.chooseCombatCardLayout.leftCombatValue.text = leftFinalValue;
        combatDisplay.chooseCombatCardLayout.rightCombatValue.text = rightFinalValue;
        foreach (Text t in textDisplays)
        {
            t.color = highlightColor;
        }
        yield return new WaitForSeconds(0.05f);

        for (float i = 0.9f; i > 0; i -= 0.15f)
        {
            foreach (Text t in textDisplays)
            {
                t.color = Color.Lerp(startColor, highlightColor, i);
            }
            yield return new WaitForSeconds(0.02f);
        }
        foreach (Text t in textDisplays)
        {
            t.color = startColor;
        }

        yield return new WaitForSeconds(time - 0.37f);
        resolveCombat();
    }


    //// Implementation Details ////
    
    private void initFight(CharacterBehavior currentChar, CharacterBehavior enemy)
    {
        Debug.Assert(status == Status.NONE, "Fight already started");
        Debug.Assert(fighters == null, "fighters are set before a fight");

        status = Status.LAUNCH;
        fighters = currentChar.getFighters(enemy);

        currentChar.selected = false;
        currentChar.clearDeplacementHUD();
        currentChar.clearUnresolvedActions();

        displayUI();
    }

    #region displayUI
    private void displayUI()
    {
        Debug.Assert(status == Status.LAUNCH, "Fight is not in initial state");
        //DebugSelectRandomFighters(1, 1);

        combatDisplay.gameObject.SetActive(true);
        combatDisplay.combatBackground.closePanelsForCombat();
        combatDisplay.leftTotal.text = GetFighterGroupCombatValue(0).ToString();
        combatDisplay.rightTotal.text = GetFighterGroupCombatValue(1).ToString();
        combatDisplay.chooseCombatCardLayout.leftCombatValue.text = "0";
        combatDisplay.chooseCombatCardLayout.rightCombatValue.text = "0";

        displayFighters();

        combatDisplay.BroadcastMessage("launchFadeIn");

        launchAnimation();
    }

    private void DebugSelectRandomFighters(int leftCount, int rightCount)
    {
        fighters = new List<CharacterBehavior>();
        foreach (var token in GameObject.FindGameObjectsWithTag("Token"))
        {
            var character = token.GetComponent<CharacterBehavior>();
            if (character != null)
            {
                if (character.getOwnerIndex() == 0 && leftCount > 0)
                {
                    --leftCount;
                    fighters.Add(character);
                }

                if (character.getOwnerIndex() == 1 && rightCount > 0)
                {
                    --rightCount;
                    fighters.Add(character);
                }
            }
        }
    }

    private void displayFighters()
    {
        for (int side = 0; side < 2; ++side)
        {
            Transform sideLayout = getSideLayout(side);

            sideLayout.gameObject.SetActive(true);

            List<CharacterBehavior> fighterGroup = GetFighterGroup(side);

            int n = fighterGroup.Count;
            if (n > 4) // must instantiate mini sprites
            {
                for (int i = 0; i < n; i++)
                {
                    Vector3 position = sideLayout.position;
                    position.x += 50.0f * (float)i / (float)(n / 2);
                    position.y += (i % 2 == 0) ? 50.0f : -50.0f;
                    instantiateSmallFighterSprite(sideLayout, fighterGroup[i], position, side == 1);
                }
            }

            foreach (var fighter in fighterGroup)
                displayFighter(fighter);

            sideLayout.BroadcastMessage("launchFadeIn");
        }
    }

    private string[] sideDisplay = { "LeftSide", "RightSide" };
    private string[] countDisplay = { "OneFighter", "TwoFighters", "ThreeFighters", "FourFighters", "ManyFighters", "ManyFighters", "ManyFighters", "ManyFighters" };
    private string[] specificFighter = { "First_", "Second_", "Third_", "Fourth_" };
    private Transform getSideLayout(int side)
    {
        var fighterGroup = GetFighterGroup(side);
        string groupDisplay = sideDisplay[side] + "/" + countDisplay[fighterGroup.Count-1];
        return combatDisplay.transform.Find(groupDisplay);
    }

    private Transform GetFighterSpriteTransform(CharacterBehavior fighter, out string prefix)
    {
        int side = fighter.getOwnerIndex();
        var fighterGroup = GetFighterGroup(side);
        int n = fighterGroup.Count;
        int i = fighterGroup.IndexOf(fighter);
        Transform sideLayout = getSideLayout(side);

        prefix = (n > 1 && n < 4) ? specificFighter[i] : "";
        return (n < 4) ? sideLayout : sideLayout.GetChild(i);
    }

    private void launchAnimation()
    {
        Debug.Assert(status == Status.LAUNCH, "Fight is not in initial state");
        float animationDuration = 2.0f;
        GameObject[] combatBonuses = GameObject.FindGameObjectsWithTag("CombatAnimation");
        // Si au moins un joueur ÃƒÂ  une animation ÃƒÂ  jouer, on la lance et on augmente le temps d'attente en consÃƒÂ©quence
        if (combatBonuses.GetLength(0) > 0)
        {
            animationDuration += 1.0f;
            foreach (GameObject cB in combatBonuses) cB.SendMessage("playAnimation");
        }
        StartCoroutine(waitForCombatAnimation(animationDuration));
    }

    private IEnumerator waitForCombatAnimation(float animationDuration)
    {
        Debug.Assert(status == Status.LAUNCH, "Fight is not in initial state");
        yield return new WaitForSeconds(animationDuration);
        status = Status.WAITING;
        if (gManager.onlineGameInterface.isOnlineOpponent(gManager.activePlayer.index))
            gManager.switchPlayer();
        else
            gManager.startPlayerTurn();
    }

    private void displayFighter(CharacterBehavior fighter)
    {
        string prefix;
        Transform fighterDisplay = GetFighterSpriteTransform(fighter, out prefix);
        fighterDisplay.Find(prefix + "FighterSprite").GetComponent<Image>().sprite = fighter.GetComponent<CharacterBehaviorIHM>().fullCharacterSprite;
        fighterDisplay.Find(prefix + "FighterSprite").GetComponent<FadeSprite>().fighter = fighter;
        //fighterDisplay.Find(prefix+"FighterName").GetComponent<Image>().sprite = fighter.getTokenNameSprite();
        fighterDisplay.Find(prefix + "CombatValue/Text").GetComponent<Text>().text = GetCombatValue(fighter).ToString();

        int bonus = GetItemBonus(fighter);
        if (bonus > 0)
        {
            Debug.Assert(!fighter.wounded);
            fighterDisplay.Find(prefix + "CombatItemsBackground").gameObject.SetActive(true);
            fighterDisplay.Find(prefix + "CombatItem1Sprite").gameObject.SetActive(true);
            fighterDisplay.Find(prefix + "CombatItem1Sprite").GetComponent<Image>().sprite = fighter.tokenTranporte.GetComponent<ItemIHM>().getTokenIcon().GetComponent<SpriteRenderer>().sprite;
            //fighterDisplay.Find(prefix+"CombatItem1Name").gameObject.SetActive(true);
            //fighterDisplay.Find(prefix+"CombatItem1Name").GetComponent<Image>().sprite = fighter.tokenTranporte.GetComponent<ItemIHM>().getTokenNameSprite();
            fighterDisplay.Find(prefix + "CombatBonus").gameObject.SetActive(true);
            fighterDisplay.Find(prefix + "CombatBonus/Text").GetComponent<Text>().text = "+" + bonus.ToString();
        }
        else
        {
            fighterDisplay.Find(prefix + "CombatItemsBackground").gameObject.SetActive(false);
            fighterDisplay.Find(prefix + "CombatItem1Sprite").gameObject.SetActive(false);
            //fighterDisplay.Find(prefix+"CombatItem1Name").gameObject.SetActive(false);
            fighterDisplay.Find(prefix + "CombatBonus").gameObject.SetActive(false);
        }
    }

    private Transform instantiateSmallFighterSprite(Transform side, CharacterBehavior fighter, Vector3 position, bool rightSide)
    {
        GameObject instanceFighter = (GameObject)Instantiate(gManager.fighterPrefab, position, Quaternion.identity);
        Transform fighterDisplay = instanceFighter.transform;
        fighterDisplay.parent = side;
        if (rightSide)
        {
            fighterDisplay.localScale = new Vector3(1, 1, 1);
            Vector3 mirror = new Vector3(-1, 1, 1);
            fighterDisplay.Find("FighterToken").GetComponent<Image>().sprite = fighter.GetComponent<SpriteRenderer>().sprite;
            //fighterDisplay.Find("FighterToken/FighterName").localScale = mirror;
            //fighterDisplay.Find("CombatItem1Name").localScale = mirror;
            fighterDisplay.Find("CombatValue").localScale = mirror;
            fighterDisplay.Find("CombatBonus").localScale = mirror;
        }
        return fighterDisplay;
    }

    private CombatCards instantiateCardFromPrefab(GameObject parent, GameObject cardPrefab, bool cardAvailable)
    {
        GameObject cardObject = (GameObject)Instantiate(cardPrefab, cardPrefab.transform.position, Quaternion.identity);
        cardObject.transform.SetParent(parent.transform);
        cardObject.transform.localScale = new Vector3(1, 1, 1);
        CombatCards card = cardObject.GetComponent<CombatCards>();
        if (cardAvailable || !card.doublon)
        {
            cardObject.SetActive(true);
            if (!cardAvailable || card.doublon) card.GetComponent<Button>().interactable = false;
            if (!cardAvailable) card.image.sprite = card.usedCardSprite;
            card.indexCard = gManager.activePlayer.index;
        }
        return card;
    }

    private GameObject createCardsHolder()
    {
        GameObject cards = new GameObject("(Dynamic) Cards"); // memory leak ? -> destroyed by cleanUpScreen ?
        cards.transform.SetParent(combatDisplay.chooseCombatCardLayout.transform);
        cards.transform.SetSiblingIndex(combatDisplay.chooseCombatCardLayout.transform.Find("Cards").GetSiblingIndex() + 1);
        cards.transform.localScale = new Vector3(1, 1, 1);
        return cards;
    }
    #endregion

    #region scriptedplayer
    private List<CombatCards> GetAvailableCards(GameObject cards)
    {
        List<CombatCards> availableCards = new List<CombatCards>();
        for (int i = 0; i < cards.transform.childCount; i++)
        {
            CombatCards card = cards.transform.GetChild(i).gameObject.GetComponent<CombatCards>();
            bool cardAvailable = gManager.activePlayer.combatCardsAvailable[i];
            if (cardAvailable && !card.doublon)
                availableCards.Add(card);
        }
        Debug.Assert(availableCards.Count > 0, "There should be at least 1 available card.");
        return availableCards;
    }

    private CombatCards SelectRandomCombatCardValue(GameObject cards)
    {
        List<CombatCards> availableCards = GetAvailableCards(cards);
        int randomIndex = UnityEngine.Random.Range(0, availableCards.Count);
        return availableCards[randomIndex];
    }

    private CombatCards SelectStrongestCombatCardValue(GameObject cards)
    {
        CombatCards strongestCard = null;
        foreach (var card in GetAvailableCards(cards))
            if (strongestCard == null || card.combatValue > strongestCard.combatValue)
                strongestCard = card;
        return strongestCard;
    }

    private void AutoSelectCardForScriptedPlayer(GameObject cards)
    {
        Debug.Assert(gManager.activePlayer.isScriptedPlayer());
        cards.transform.Translate(Vector3.down * Screen.height); // hide cards
        if (GameObject.Find("Board").GetComponent<PremadeBoardSetupParameters>().useStrongestCombatCards) SelectStrongestCombatCardValue(cards).cardSelection();
        else SelectRandomCombatCardValue(cards).cardSelection();
    }
    #endregion

    #region resolution
    private void resolveCombat()
    {
        Debug.LogWarning("resolveCombat");
        Debug.Assert(status == Status.CARDS_REVEALED);
        status = Status.RESOLUTION;

        int attackerIndex = gManager.activePlayer.index;
        int defenderIndex = 1 - attackerIndex;
        int attackerValue = getCardValue(attackerIndex) + GetFighterGroupCombatValue(attackerIndex);
        int defenderValue = getCardValue(defenderIndex) + GetFighterGroupCombatValue(defenderIndex);

        if (attackerValue != defenderValue)
        {
            int looserIndex = (attackerValue < defenderValue) ? attackerIndex : defenderIndex;
            foreach (var fighter in GetFighterGroup(looserIndex))
            {
                if (!fighter.wounded)
                {
                    fighter.characterWounded();
                    string prefix;
                    Transform parent = GetFighterSpriteTransform(fighter, out prefix);
                    StartCoroutine(woundCharacterFeedback(parent.Find(prefix + "FighterSprite").gameObject));
                }
                else
                {
                    fighter.GetComponent<CharacterBehaviorIHM>().killCharacterIHM();
                }
            }
        }

        StartCoroutine(endCombat(1.0f));
    }

    private IEnumerator woundCharacterFeedback(GameObject character)
    {
        //Debug.Log(character);
        yield return new WaitForSeconds(0.01f);
        if (character != null)
        {
            Image characterImage = character.GetComponent<Image>();
            if (characterImage == null) Debug.LogError("Game Manager, woundCharacterFeedback: Animation de feedback du joueur blessÃƒÂ© manquante !");
            else
            {
                Color currentRed = new Color(1, 0, 0, characterImage.color.a);
                if (characterImage.color != currentRed)
                {
                    characterImage.color = Color.Lerp(characterImage.color, currentRed, 0.1f);
                    StartCoroutine(woundCharacterFeedback(character));
                }
                else Debug.Log(Time.time);
            }
        }
    }

    // Attente de la fin des animations de rÃ©solution du combat
    private IEnumerator endCombat(float time)
    {
        Debug.LogWarning("endCombat animation");
        Debug.Assert(status == Status.RESOLUTION);

        yield return new WaitForSeconds(time);
        combatDisplay.BroadcastMessage("launchFadeOut");
        yield return new WaitForSeconds(time);
        endCombat();
    }

    // Sort du mode combat et remet les variables concernÃƒÂ©es ÃƒÂ  la normale
    private void endCombat()
    {
        Debug.LogWarning("endCombat");
        Debug.Assert(status == Status.RESOLUTION);

        if (combatDisplay.name != "CombatLayout") Destroy(combatDisplay); // will be resolved on the next update

        SetAllChildrenActive(combatDisplay.transform.Find("LeftSide"), false);
        SetAllChildrenActive(combatDisplay.transform.Find("RightSide"), false);
        combatDisplay.combatBackground.closeCombatBackground();

        cartesP1.SetActive(false);
        cartesP2.SetActive(false);
        
        foreach (var fighter in fighters) fighter.GetComponent<CharacterBehaviorIHM>().changeSortingLayer("TokensOnBoard");

        // the active player may or may not be the player who started the fight in online game, therefore this check is made to know if the turn must be changed.
        bool isActivePlayerTurn = gManager.isActivePlayer( gManager.actionCharacter.GetComponent<CharacterBehavior>().affiliationJoueur );

        if (gManager.actionCharacter != null)
        {
            gManager.actionCharacter.GetComponent<CharacterBehavior>().selected = false;
            gManager.actionCharacter.GetComponent<CharacterBehavior>().clearUnresolvedActions();
            gManager.actionCharacter.GetComponent<CharacterBehaviorIHM>().changeSortingLayer("TokensOnBoard");
            gManager.actionCharacter.GetComponent<CharacterBehaviorIHM>().endDeplacementIHM();
        }
        else Debug.LogError("Game Manager, endCombat: Personnage actif mort");

        gManager.onlineGameInterface.EndReplayAction();

        resetCombatStatus();

        if (isActivePlayerTurn)
            gManager.startPlayerTurn();
        else
            gManager.switchPlayer();
    }

    private void SetAllChildrenActive(Transform transform, bool activate = true) { for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false); }
    #endregion

    #region fighters
    private List<CharacterBehavior> GetFighterGroup(int side)
    {
        Debug.Assert(fighters != null, "Fighters are not set");
        List<CharacterBehavior> fighterGroup = new List<CharacterBehavior>();
        foreach (CharacterBehavior fighter in fighters)
            if (fighter.indexToken == side)
            {
                Debug.Assert(!fighterGroup.Contains(fighter));
                fighterGroup.Add(fighter);
            }
        Debug.Assert(fighterGroup.Count > 0, "There must be at least 1 fighter of each side");
        return fighterGroup;
    }

    private int GetFighterGroupCombatValue(int side)
    {
        int value = 0;
        foreach (var fighter in GetFighterGroup(side)) value += GetCombatValue(fighter) + GetItemBonus(fighter);
        return value;
    }

    private int GetFighterGroupFinalCombatValue(int side) { return GetFighterGroupCombatValue(side) + getCardValue(side);  }

    private int GetCombatValue(CharacterBehavior fighter) { return fighter.wounded ? 0 : fighter.COMBAT_VALUE; }

    private bool IsAttacker(CharacterBehavior fighter) { return fighters[0].affiliationJoueur == fighter.affiliationJoueur; }

    private int GetItemBonus(CharacterBehavior fighter)
    {
        Item tokenHeld = fighter.tokenTranporte != null ? fighter.tokenTranporte.GetComponent<Item>() : null;
        if (tokenHeld != null && tokenHeld.combatItem) // hold a combat item
        {
            if (IsAttacker(fighter) && tokenHeld.attackItemBonus > 0) return tokenHeld.attackItemBonus;
            if (!IsAttacker(fighter) && tokenHeld.defenseItemBonus > 0) return tokenHeld.defenseItemBonus;
        }
        return 0;
    }
    #endregion

    ////// gManager wrappers //////
    private CombatUI combatDisplay { get { return gManager.combatDisplay; } }

    private Coroutine StartCoroutine(IEnumerator routine) { return gManager.StartCoroutine(routine); }
    public UnityEngine.Object Instantiate(UnityEngine.Object original, Vector3 position, Quaternion rotation) { return gManager.InstantiateAccessForCombatManager(original, position, rotation); }
    private void Destroy(UnityEngine.Object obj) { gManager.DestroyAccessForCombatManager(obj); }
}
