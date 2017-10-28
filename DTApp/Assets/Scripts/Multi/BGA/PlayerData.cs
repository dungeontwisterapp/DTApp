using UnityEngine;
using System.Collections;

namespace Multi
{
    namespace BGA
    {
        public class PlayerData : JSON
        {
            public string id { get { return StringFieldAccess("id"); } }
            public string fullname { get { return StringFieldAccess("fullname"); } }
            public string rank { get { return StringFieldAccess("rank"); } }

            public PlayerData(JSONObject json) : base(json)
            {
            }

            public bool Equals(PlayerData data)
            {
                return id == data.id;
            }
        }
    }
}
