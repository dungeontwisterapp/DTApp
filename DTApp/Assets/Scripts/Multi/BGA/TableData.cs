using UnityEngine;
using System.Collections;

namespace Multi
{
    namespace BGA
    {
        public class TableData : JSON
        {
            private static string _gameName = "dungeontwister";

            public string PlayerIdAccess(int index)
            {
                try { return _json.GetField("player_display")[index].str; } catch { return "????"; }
            }

            public PlayerData PlayerAccess(string id)
            {
                return _json.GetField("players").HasField(id) ? new PlayerData(_json.GetField("players").GetField(id)) : null;
            }

            public string id { get { return StringFieldAccess("id"); } }
            public string status { get { return StringFieldAccess("status"); } }
            public bool cancelled { get { return StringFieldAccess("cancelled") != "0"; } }
            public bool unranked { get { return StringFieldAccess("unranked") != "0"; } }
            public string gameName { get { return StringFieldAccess("game_name"); } }
            public bool isValid { get { return gameName == _gameName; } }

            public bool isOpen { get { return status == "open" || status == "asyncopen"; } }
            public int playerCount { get { return (player1 == null ? 0 : 1) + (player2 == null ? 0 : 1); } }

            public bool HasPlayer(string id) { return _json.GetField("players").HasField(id); }

            public PlayerData player1 { get { return PlayerAccess(PlayerIdAccess(0)); } }
            public PlayerData player2 { get { return PlayerAccess(PlayerIdAccess(1)); } }

            public TableData(JSONObject json) : base(json)
            {
                Update(json);
            }

            public bool Equals(TableData data)
            {
                return id == data.id;
            }

            public void Update(JSONObject json)
            {
                _json = json;
            }
        }
    }
}
