using UnityEngine;
using System.Collections;

namespace Actions
{
    public class ActionRoomDiscovered : Action
    {
        public string playerId;
        //public int tokenId;
        public int tileIndex;
        public string tileName;
        public int orientation;
        public ActionRoomDiscovered(string playerId/*, int tokenId*/,int tileIndex, string tileName, int orientation)
        {
            type = Type.ROOMDISCOVERED;
            this.playerId = playerId;
            //this.tokenId = tokenId;
            this.tileIndex = tileIndex;
            this.tileName = tileName;
            this.orientation = orientation;
        }
    }
}
