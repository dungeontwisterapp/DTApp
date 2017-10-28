using UnityEngine;
using System;
using System.Collections;

[Serializable]
public struct GameDataPlayers
{
    public const int NO_REFERENCE = -1;

    public int victoryPoints;
    public int nbSauts;
    public bool[] usedActionCards;
    public bool[] combatCardsAvailable;
    public bool combatCardPlayed;
    public int combatCardValue;

    public GameDataPlayers(int VP, int nbJumps, bool[] actionCardsState, bool[] combatCardsState, bool combatCardPlayed = false, int combatCardValue = NO_REFERENCE)
    {
        victoryPoints = VP;
        nbSauts = nbJumps;

        int nbActionCards = actionCardsState.GetLength(0), nbCombatCards = combatCardsState.GetLength(0);

        usedActionCards = new bool[nbActionCards];
        for (int i = 0; i < nbActionCards; i++)
        {
            usedActionCards[i] = actionCardsState[i];
        }

        combatCardsAvailable = new bool[nbCombatCards];
        for (int i = 0; i < nbCombatCards; i++)
        {
            combatCardsAvailable[i] = combatCardsState[i];
        }

        this.combatCardPlayed = combatCardPlayed;
        this.combatCardValue = combatCardValue;
    }

}
