using UnityEngine;

namespace Actions
{
    public class TokenUpdateData
    {
        private int _id;
        private int _x, _y;
        private string _playerid;
        private string _token;
        private bool _isItem;
        private int _holdingCharacterId;

        public bool isHidden { get { return token == "hidden"; } }
        public bool onRoom { get { return _y == -1; } }
        public bool isItem { get { Debug.Assert(!isHidden); return _isItem; } }
        public bool isCharacter { get { Debug.Assert(!isHidden); return !_isItem; } }
        public bool isCarried { get { return _holdingCharacterId != 0; } }

        public int id { get { return _id; } }
        public string playerId { get { return _playerid; } }
        public string token { get { return _token; } }
        public int x { get { Debug.Assert(!onRoom); return _x; } }
        public int y { get { Debug.Assert(!onRoom); return _y; } }
        public int room { get { Debug.Assert(onRoom); return _x; } }
        public int holdingCharacterId { get { Debug.Assert(isCarried); return _holdingCharacterId; } }

        public TokenUpdateData(int id, string playerid, string token, bool isItem, int x, int y, int holdingCharacterId = 0)
        {
            _id = id;
            _playerid = playerid;
            _token = token;
            _x = x;
            _y = y;
            _isItem = isItem;
            _holdingCharacterId = holdingCharacterId;
        }

        public TokenUpdateData(int id, string playerid, string token, bool isItem, int room)
        {
            _id = id;
            _playerid = playerid;
            _token = token;
            _x = room;
            _y = -1;
            _isItem = isItem;
            _holdingCharacterId = 0;
        }

        public override string ToString()
        {
            string result = formatToken(id, playerId, token);

            if (isCarried)
                result += " is carried by " + formatToken(holdingCharacterId);
            else if (onRoom)
                result += " is to be placed on room " + room;
            else
                result += " is on cell " + x + "," + y;

            return result;
        }

        static private string formatToken(int tokenId)
        {
            int owner;
            string name;
            GameManager.gManager.onlineGameInterface.GetTokenFromId(tokenId, out owner, out name);
            return formatToken(tokenId, owner, name);
        }

        static private string formatToken(int tokenId, string ownerId, string name)
        {
            return formatToken(tokenId, GameManager.gManager.onlineGameInterface.playerIndex(ownerId), name);
        }

        static private string formatToken(int tokenId, int owner, string name)
        {
            return string.Format("{0} of {1} (id {2}) ",
                GameManager.gManager.onlineGameInterface.playerName(owner),
                name, tokenId);
        }
    }
}
