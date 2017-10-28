using UnityEngine;
using System.Collections;

namespace Actions
{
    public class ActionStateChange : Action
    {
        public string statename;
        public string activePlayer;
        public bool leaveState;
        public ActionStateChange(string name, string player, bool leave)
        {
            type = Type.STATECHANGE;
            statename = name;
            activePlayer = player;
            leaveState = leave;
        }
    }
}
