using UnityEngine;
using System;
using System.Collections;

namespace Multi
{
    namespace Web
    {

        public class BgaServerData
        {
            public enum Lang { FR };

            // options
            public Lang lang = Lang.FR;
            public bool secure = false;
            public bool preprod = true;

            // accessors
            public string Url { get { return string.Format("{0}://{1}.{2}", protocol, prefix, adress); } }
            public string WSUrl { get { return string.Format("{0}://{1}", wsprotocol, wsadress); } }
            public string GameId { get { return gameId; } }
            public string GameName { get { return gameName; } }

            public string GetRequestUrl(string category, string name)
            {
                return string.Format("{0}/{1}/{1}/{2}.html", Url, category, name);
            }
            public Uri GetRequestUri(string category, string name)
            {
                string urlString = GetRequestUrl(category, name);
                Logger.Instance.Log("DETAIL", "GetRequestUri: " + urlString);
                return new Uri(urlString);
            }

            // implementation details
            private string gameId = "1048";
            private string gameName = "dungeontwister";

            private string bgaProdURL = "boardgamearena.com";
            private string bgaPreprodURL = "preprod.boardgamearena.com";
            private string bgaWSsuffix = ":8080/socket.io";

            private string protocol { get { return secure ? "https" : "http"; } }
            private string wsprotocol { get { return "ws"; } }

            private string adress { get { return preprod ? bgaPreprodURL : bgaProdURL; } }
            private string wsadress { get { return adress + bgaWSsuffix; } }

            private string prefix {
                get {
                    switch (lang) {
                        case Lang.FR:
                        default:
                            return "fr";
                    }
                }
            }
            
            //private string _socketTestURL = "http://chat.socket.io/";
            //private string _memSocketURL = "ws://preprod.boardgamearena.com:8080/socket.io/?EIO=4&transport=websocket";
        }

    }
}
