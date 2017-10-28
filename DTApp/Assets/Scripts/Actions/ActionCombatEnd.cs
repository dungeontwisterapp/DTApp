using UnityEngine;
using System.Collections.Generic;

namespace Actions
{
    public class ActionCombatEnd : Action
    {
        public string playerId;
        public string tokenName;
        public int attackerScore;
        public int defenderScore;
        public string winnerId;
        public List<int> woundedIds;
        public List<int> killedIds;
        public int attackerCardValue;
        public int defenderCardValue;
        public ActionCombatEnd(string playerId, string tokenName, int attackerScore, int defenderScore, string winnerId,
                                List<int> woundedIds, List<int> killedIds, int attackerCardValue = -1, int defenderCardValue = -1)
        {
            type = Type.COMBAT_END;
            this.playerId = playerId;
            this.tokenName = tokenName;
            this.attackerScore = attackerScore;
            this.defenderScore = defenderScore;
            this.winnerId = winnerId;
            this.woundedIds = woundedIds;
            this.killedIds = killedIds;
            this.attackerCardValue = attackerCardValue;
            this.defenderCardValue = defenderCardValue;
        }

        public bool isCombatResult { get { return attackerCardValue == -1; } }
    }
}

