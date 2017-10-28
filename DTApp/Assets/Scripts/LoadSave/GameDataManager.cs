using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class GameDataManager {
    public enum State { SETUP, PLACEMENT1_FIRST, PLACEMENT1_SECOND, PLACEMENT2, CHOOSE_ACTION_CARD, PLAY_ACTION, SELECTION, MOVING, PLACE_TOKEN_FIRST, PLACE_TOKEN_SECOND, CHOOSE_COMBAT_CARD_FIRST, CHOOSE_COMBAT_CARD_SECOND, GAMEOVER }
    public State gamestate;
    public int indexJoueurActif;
    public int actionPoints;
    public int actionCardMaxValue;
    public int roomBeingOpenedIndex;
    // if character selected / moving
    public int selectedCharacterOwnerIndex;
    public string selectedCharacterName;
    public string combatTargetName;
    public int pointsRemaining;

    public GameDataManager(State gamestate, int currentPlayerIndex, int currentActionPoints, int currentMaxActionCardValueAllowed, int roomBeingOpenedIndex, string selectedCharacterName = null, int selectedCharacterOwnerIndex = -1, int pointsRemaining = -1, string combatTargetName = null)
    {
        this.gamestate = gamestate;
        indexJoueurActif = currentPlayerIndex;
        actionPoints = currentActionPoints;
        actionCardMaxValue = currentMaxActionCardValueAllowed;
        this.roomBeingOpenedIndex = roomBeingOpenedIndex;
        this.selectedCharacterOwnerIndex = selectedCharacterOwnerIndex;
        this.selectedCharacterName = selectedCharacterName;
        this.combatTargetName = combatTargetName;
        this.pointsRemaining = pointsRemaining;
    }

    // helpers
    public bool isPlacement1 { get { return gamestate == State.PLACEMENT1_FIRST || gamestate == State.PLACEMENT1_SECOND; } }

    public static State buildGameState(GameManager gManager)
    {
        Debug.Assert(gManager.startTurn);
        State gamestate;
        switch (gManager.gameMacroState)
        {
            case GameProgression.GameStart:
                Debug.Assert(!gManager.turnStarted);
                Debug.Assert(!gManager.combatEnCours);
                Debug.Assert(!gManager.placerTokens);
                Debug.Assert(!gManager.selectionEnCours);
                if (!gManager.otherPlayerTurnToPlace) gamestate = State.PLACEMENT1_FIRST;
                else gamestate = State.PLACEMENT1_SECOND;
                break;
            case GameProgression.TokenPlacement:
                Debug.Assert(!gManager.turnStarted);
                Debug.Assert(!gManager.combatEnCours);
                Debug.Assert(!gManager.placerTokens);
                Debug.Assert(!gManager.otherPlayerTurnToPlace);
                Debug.Assert(!gManager.selectionEnCours);
                gamestate = State.PLACEMENT2;
                break;
            case GameProgression.Playing:
                if (gManager.combatEnCours)
                {
                    Debug.Assert(!gManager.placerTokens);
                    Debug.Assert(gManager.turnStarted);
                    Debug.Assert(gManager.selectionEnCours);
                    if (!gManager.otherPlayerTurnToPlace) gamestate = State.CHOOSE_COMBAT_CARD_FIRST;
                    else gamestate = State.CHOOSE_COMBAT_CARD_SECOND;
                }
                else if (gManager.placerTokens)
                {
                    Debug.Assert(gManager.turnStarted);
                    Debug.Assert(gManager.selectionEnCours);
                    if (!gManager.otherPlayerTurnToPlace) gamestate = State.PLACE_TOKEN_FIRST;
                    else gamestate = State.PLACE_TOKEN_SECOND;
                }
                else if (gManager.turnStarted)
                {
                    Debug.Assert(!gManager.otherPlayerTurnToPlace);
                    Debug.Assert(!gManager.selectionEnCours);
                    gamestate = State.PLAY_ACTION;
                }
                else
                {
                    Debug.Assert(!gManager.otherPlayerTurnToPlace);
                    Debug.Assert(gManager.selectionEnCours);
                    gamestate = State.CHOOSE_ACTION_CARD;
                }
                break;
            case GameProgression.GameOver:
                Debug.Assert(!gManager.turnStarted);
                Debug.Assert(!gManager.combatEnCours);
                Debug.Assert(!gManager.placerTokens);
                Debug.Assert(!gManager.otherPlayerTurnToPlace);
                Debug.Assert(!gManager.selectionEnCours);
                gamestate = State.GAMEOVER;
                break;
            default:
                Debug.Assert(false); // not supposed to happen
                gamestate = State.SETUP;
                break;
        }
        return gamestate;
    }

    public static void applyGameState(GameManager gManager, State gamestate)
    {
        gManager.actionPointCost = 0;
        switch (gamestate)
        {
            case State.SETUP:
                Debug.Assert(false); // not supposed to happen
                break;
            case State.PLACEMENT1_FIRST:
            case State.PLACEMENT1_SECOND:
                gManager.gameMacroState = GameProgression.GameStart;
                gManager.turnStarted = false;
                gManager.placerTokens = false;
                gManager.otherPlayerTurnToPlace = (gamestate == State.PLACEMENT1_SECOND);
                gManager.selectionEnCours = false;
                break;
            case State.PLACEMENT2:
                gManager.gameMacroState = GameProgression.TokenPlacement;
                gManager.turnStarted = false;
                gManager.placerTokens = false;
                gManager.otherPlayerTurnToPlace = false;
                gManager.selectionEnCours = false;
                break;
            case State.CHOOSE_ACTION_CARD:
            case State.PLAY_ACTION:
            case State.PLACE_TOKEN_FIRST:
            case State.PLACE_TOKEN_SECOND:
            case State.CHOOSE_COMBAT_CARD_FIRST:
            case State.CHOOSE_COMBAT_CARD_SECOND:
                gManager.gameMacroState = GameProgression.Playing;
                gManager.turnStarted = gamestate != State.CHOOSE_ACTION_CARD;
                gManager.placerTokens = (gamestate == State.PLACE_TOKEN_FIRST || gamestate == State.PLACE_TOKEN_SECOND);
                gManager.otherPlayerTurnToPlace = (gamestate == State.PLACE_TOKEN_SECOND || gamestate == State.CHOOSE_COMBAT_CARD_SECOND);
                gManager.selectionEnCours = (gamestate != State.PLAY_ACTION);
                /*if (gManager.placerTokens)
                {
                    Debug.LogError("Loading during placement on Revealed Tile");
                    if (gManager.otherPlayerTurnToPlace) Debug.LogError("Phase II - turn of player " + gManager.activePlayer.index);
                    else Debug.LogError("Phase I - turn of player " + gManager.activePlayer.index);
                }*/
                if (gamestate != State.CHOOSE_ACTION_CARD && gamestate != State.PLAY_ACTION) gManager.actionPointCost = 1;
                break;
            case State.SELECTION:
            case State.MOVING:
                gManager.gameMacroState = GameProgression.Playing;
                gManager.turnStarted = true;
                gManager.placerTokens = false;
                gManager.otherPlayerTurnToPlace = false;
                gManager.selectionEnCours = (gamestate == State.MOVING);
                gManager.deplacementEnCours = (gamestate == State.MOVING);
                if (gManager.deplacementEnCours) gManager.actionPointCost = 1;
                break;
            case State.GAMEOVER:
                gManager.gameMacroState = GameProgression.GameOver;
                gManager.turnStarted = false;
                gManager.placerTokens = false;
                gManager.otherPlayerTurnToPlace = false;
                gManager.selectionEnCours = false;
                break;
        }

        gManager.startTurn = true;
    }
}
