using UnityEngine;
using System.Collections;

namespace Actions
{
    public class ActionMove : Action
    {
        public string playerId;
        public int tokenId;
        public string tokenName;
        public int x;
        public int y;
        public ActionMove(string playerId, int tokenId, string tokenName, int x, int y)
        {
            type = Type.MOVE;
            this.playerId = playerId;
            this.tokenId = tokenId;
            this.tokenName = tokenName;
            this.x = x;
            this.y = y;
        }
    }
}

