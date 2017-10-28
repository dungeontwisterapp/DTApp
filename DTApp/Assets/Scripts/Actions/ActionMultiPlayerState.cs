using UnityEngine;
using System.Collections.Generic;

namespace Actions
{
    public class ActionMultiPlayerState : Action
    {
        public List<string> activePlayers;
        public ActionMultiPlayerState(List<string> players)
        {
            type = Type.MULTIPLAYERSTATE;
            activePlayers = players;
        }
    }
}

