using UnityEngine;
using System.Collections.Generic;

namespace Actions
{
    public class ActionCombatStart : Action
    {
        public string playerId ;
        public List<string> attackers;
        public List<string> defenders;
        public ActionCombatStart(string playerId, List<string> attackers, List<string> defenders)
        {
            type = Type.COMBAT_START;
            this.playerId = playerId;
            this.attackers = attackers;
            this.defenders = defenders;
        }
    }
}

