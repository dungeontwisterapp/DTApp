using UnityEngine;
using System.Collections;

namespace Actions
{
    public class Action
    {
        public enum Type { UNKNOWN, CARD, STATECHANGE, TOKENSUPDATE, MULTIPLAYERSTATE, SIMPLENOTE, ROOMDISCOVERED, UPDATETIMER, ROOMROTATED, MOVE, HEAL, REMOVE, DOOR, COMBAT_START, COMBAT_END, PASS }
        public Type type;
        public ActionType action;

        public Action()
        {
            type = Type.UNKNOWN;
        }
    }
}
