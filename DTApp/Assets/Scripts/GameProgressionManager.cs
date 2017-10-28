using UnityEngine;
using System.Collections;

/// This class purposes is to get details regarding the current game state
/// - tell wich state we are in
/// - tell which player can play, and what kind of action
/// - tell whether game is processing an action that needs to be terminated before starting another one
public class GameProgressionManager
{
    private GameManager gManager;
    private int firstPlayerToPlaceOffline = -1;
    private int firstPlayerToPlayOffline = -1;
    private int processCount = 0;
    private bool blockInteractions = false;

    public GameProgressionManager(GameManager gManager)
    {
        this.gManager = gManager;
        Reset();
    }

    public void Reset()
    {
        firstPlayerToPlaceOffline = -1;
        firstPlayerToPlayOffline = -1;
        processCount = 0;
        blockInteractions = false;
    }

    ////// Critical processing state //////
    public bool IsGameProcessing()
    {
        if (processCount > 0) return true;
        Debug.LogWarning("IsGameProcessing not fully used: must return true when an animation of other longer then 1 frame events are processing");
        return false;
    }

    public void StartProcess() { processCount++; if (processCount == 1) Debug.LogWarning("Start critical process"); else Debug.LogWarning(processCount.ToString() + " critical process running at the same time"); }
    public void EndProcess() { Debug.Assert(processCount > 0); processCount--; if (processCount == 0) Debug.LogWarning("End critical process"); else Debug.LogWarning(processCount.ToString() + " critical process remaining"); }

    public void BlockInteractionsUntilBgaAnswer(bool block)
    {
        blockInteractions = block;
        gManager.app.onlineWaiting = block;
    }
    public bool isBlockingInteractions()
    {
        return blockInteractions;
    }

    ////// First Player choice infos //////
    public int GetFirstPlayerToPlaceInFirstPart()
    {
        if (app.gameToLaunch.isTutorial) return 0;
        else if (onlineGame) return onlineGameInterface.firstPlayerToPlace1;
        else
        {
            if (firstPlayerToPlaceOffline == -1) firstPlayerToPlaceOffline = TossACoin();
            Debug.Assert(firstPlayerToPlaceOffline >= 0 && firstPlayerToPlaceOffline < 2);
            return firstPlayerToPlaceOffline;
        }
    }

    public int GetFirstPlayerToPlaceInSecondPart()
    {
        if (app.gameToLaunch.isTutorial) return 0;
        else if (onlineGame) return onlineGameInterface.firstPlayerToPlace2;
        else
        {
            if (firstPlayerToPlaceOffline == -1) firstPlayerToPlaceOffline = TossACoin();
            Debug.Assert(firstPlayerToPlaceOffline >= 0 && firstPlayerToPlaceOffline < 2);
            return firstPlayerToPlaceOffline;
        }
    }

    public int GetFirstPlayerToPlay()
    {
        if (app.gameToLaunch.isTutorial) return 0;
        else if (onlineGame) return onlineGameInterface.firstPlayerToPlay;
        else
        {
            if (firstPlayerToPlayOffline == -1) firstPlayerToPlayOffline = TossACoin();
            Debug.Assert(firstPlayerToPlayOffline >= 0 && firstPlayerToPlayOffline < 2);
            return firstPlayerToPlayOffline;
        }
    }


    ////// Character Selection //////
    public bool IsCharacterSelected() { return actionCharacter != null; }

    public CharacterBehavior GetSelectedCharacter() { Debug.Assert(IsCharacterSelected()); return actionCharacter.GetComponent<CharacterBehavior>(); }


    ////// CharacterPlacement //////
    private int CharacterPlacedOnStartingLinesCount()
    {
        int nbPlayersPlacedOnStartingLines = 0;
        GameObject[] tokens = GameObject.FindGameObjectsWithTag("Token");
        foreach (GameObject token in tokens)
        {
            if (token.GetComponent<Token>().tokenPlace) nbPlayersPlacedOnStartingLines++;
        }
        return nbPlayersPlacedOnStartingLines;
    }

    public bool IsCharacterPlacementPhase() { return gManager.gameMacroState == GameProgression.GameStart; }

    public bool IsCharacterPlacementDone()
    {
        return IsCharacterPlacementPhase() && (GetFirstPlayerToPlaceInSecondPart() != -1) && (CharacterPlacedOnStartingLinesCount() == 8);
    }

    public bool IsGameInInitialPhase()
    {
        return IsCharacterPlacementPhase() && CharacterPlacedOnStartingLinesCount() == 0;
    }

    ////// TokenPlacement //////
    private bool AllTokensArePlaced()
    {
        bool allTokensPlaced = true;
        GameObject[] ciblesTokens = GameObject.FindGameObjectsWithTag("PlacementToken");
        for (int i = 0; i < ciblesTokens.Length; i++)
        {
            if (ciblesTokens[i].GetComponent<PlacementTokens>().tokenAssociated == null) allTokensPlaced = false;
        }
        return allTokensPlaced;
    }

    public bool IsTokenPlacementPhase(){ return gManager.gameMacroState == GameProgression.TokenPlacement; }

    public bool IsTokenPlacementDone()
    {
        return IsTokenPlacementPhase() && (GetFirstPlayerToPlay() != -1) && AllTokensArePlaced();
    }

    ////// Playing //////
    public bool IsGameOver()
    {
        return activePlayer.victoryPoints >= gManager.VICTORY_POINTS_LIMIT;
    }


    ////// Temporary //////
    public bool AllStartingCharactersPlaced() { return AllStartingCharactersPlaced(0) && AllStartingCharactersPlaced(1); }

    public bool AllStartingCharactersPlaced(int playerIndex)
    {
        Debug.Assert(playerIndex == 0 || playerIndex == 1);
        Debug.Assert(IsCharacterPlacementPhase());
        int nbPlayersPlacedOnStartingLines = 0;
        GameObject[] tokens = GameObject.FindGameObjectsWithTag("Token");
        foreach (GameObject o in tokens)
        {
            Token token = o.GetComponent<Token>();
            if (token.getOwnerIndex() == playerIndex && token.tokenPlace) nbPlayersPlacedOnStartingLines++;
        }
        Debug.Assert(nbPlayersPlacedOnStartingLines <= 4);
        return nbPlayersPlacedOnStartingLines == 4;
    }

    public bool IsCombatCardPhase() { return gManager.gameMacroState == GameProgression.GameStart; }

    public bool AllCombatCardChoosen() { return CombatCardChoosen(0) && CombatCardChoosen(1); }
    public bool CombatCardChoosen(int playerIndex) { return false; } // TODO

    public bool IsPlayerAllowedToPlay(int playerIndex)
    {
        Debug.Assert(playerIndex == 0 || playerIndex == 1);

        if (IsGameProcessing()) return false;
        
        if (IsCharacterPlacementPhase())
        {
            return !AllStartingCharactersPlaced(playerIndex);
        }
        else if (IsCombatCardPhase())
        {
            return !CombatCardChoosen(playerIndex);
        }
        else // any other phases
        {
            return (activePlayer.index == playerIndex) && activePlayer.myTurn;
        }
    }
    //

    ////// implementation details //////
    private int TossACoin() { return UnityEngine.Random.Range(0, 2); }

    ////// gManager wrappers //////
    private bool onlineGame { get { return gManager.onlineGame; } }
    private Multi.Interface onlineGameInterface { get { return gManager.onlineGameInterface; } }
    private AppManager app { get { return gManager.app; } }
    private GameObject actionCharacter { get { return gManager.actionCharacter; } }
    private PlayerBehavior activePlayer { get { return gManager.activePlayer; } }
}
