using UnityEngine;
using System.Collections.Generic;

namespace Actions
{
    public class ActionRemove : Action
    {
        public string playerId;
        public string tokenName;
        public int score;
        public int charPoints;
        public List<int> removeTokenIds;
        public ActionRemove(string playerId, string tokenName, int score, int charPoints, List<int> removeTokenIds)
        {
            type = Type.REMOVE;
            this.playerId = playerId;
            this.tokenName = tokenName;
            this.score = score;
            this.charPoints = charPoints;
            this.removeTokenIds = removeTokenIds;
        }
    }
}

