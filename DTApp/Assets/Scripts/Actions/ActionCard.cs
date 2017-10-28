using UnityEngine;
using System.Collections;

namespace Actions
{
    public class ActionCard : Action
    {
        public const int UNKNOWN = -1;
        public string playerId;
        public int value;
        public int total;
        public ActionCard(ActionType actiontype, string playerId, int value_ = UNKNOWN, int total_ = UNKNOWN)
        {
            Debug.Assert(actiontype == ActionType.ACTION_CARD || actiontype == ActionType.COMBAT_CARD || actiontype == ActionType.JUMP);
            type = Type.CARD;
            action = actiontype;
            this.playerId = playerId;
            value = value_;
            total = total_;
        }
    }
}
