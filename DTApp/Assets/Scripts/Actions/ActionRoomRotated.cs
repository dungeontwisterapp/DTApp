using UnityEngine;
using System.Collections;

namespace Actions
{
    public class ActionRoomRotated : Action
    {
        public string playerId;
        public string tokenName;
        public int tileIndex;
        public bool clockwise;
        public int nbAction;
        public int direction;
        public ActionRoomRotated(string playerId, string tokenName, int tileIndex, bool clockwise, int nbAction, int direction)
        {
            type = Type.ROOMROTATED;
            this.playerId = playerId;
            this.tokenName = tokenName;
            this.tileIndex = tileIndex;
            this.clockwise = clockwise;
            this.nbAction = nbAction;
            this.direction = direction;
        }
    }
}
