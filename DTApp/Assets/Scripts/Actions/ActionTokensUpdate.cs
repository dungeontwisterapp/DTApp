using UnityEngine;
using System.Collections.Generic;

namespace Actions
{
    public class ActionTokensUpdate : Action
    {
        public enum Destination { STARTING_LINE, ROOM, REVEAL, TO_PLACE, BOARD }
        public Destination destinationType;
        public List<TokenUpdateData> tokens;
        public ActionTokensUpdate(Destination destinationType_)
        {
            type = Type.TOKENSUPDATE;
            destinationType = destinationType_;
            tokens = new List<TokenUpdateData>();
        }
    }
}
