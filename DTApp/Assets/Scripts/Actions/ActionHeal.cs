using UnityEngine;
using System.Collections;

namespace Actions
{
    public class ActionHeal : Action
    {
        public string playerId;
        public string tokenName;
        public int targetId;
        public ActionHeal(string playerId, string tokenName, int targetId)
        {
            type = Type.HEAL;
            this.playerId = playerId;
            this.tokenName = tokenName;
            this.targetId = targetId;
        }
    }
}

