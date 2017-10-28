using UnityEngine;
using System.Collections;

namespace Actions
{
    public class ActionUpdateTimer : Action
    {
        public string playerId;
        public int delta, max;
        public ActionUpdateTimer(string playerId, int delta, int max)
        {
            type = Type.UPDATETIMER;
            this.playerId = playerId;
            this.delta = delta;
            this.max = max;
        }
    }
}
