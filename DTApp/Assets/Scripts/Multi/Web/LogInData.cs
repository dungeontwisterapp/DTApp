using UnityEngine;
using System.Collections.Generic;


namespace Multi
{
    namespace Web
    {

        public class LogInData
        {
            // login form
            private string _username = "";
            private string _password = "";
            private string _email = "";
            private string _lang = "fr";
            private string _key = "fa7FajAPCV9873FDG";
            private bool _rememberMe = false;

            // user infos
            private string _bgaUserId = "";
            private string _bgaUserName = "";
            //private string _bgaUserSeal = "";
            //private string _currentName = "";
            //private string _currentTable = "";
            //private string _currentGame = "";
            //private string _currentGameName = "";

            public string Username
            {
                get { return _username; }
                set { _username = value; Logger.Instance.Log("DETAIL", "username: " + _username); }
            }

            public string Password
            {
                get { return _password; }
                set { _password = value; Logger.Instance.Log("DETAIL", "password: " + _password); }
            }

            public string Email
            {
                get { return _email; }
                set { _email = value; Logger.Instance.Log("DETAIL", "email: " + _email); }
            }

            public bool RememberMe
            {
                get { return _rememberMe; }
                set { _rememberMe = value; Logger.Instance.Log("DETAIL", "_rememberMe: " + (_rememberMe ? "true" : "false")); }
            }

            public string BgaUserId { get { return _bgaUserId; } }
            public string BgaUserName { get { return _bgaUserName; } }

            public Dictionary<string, string> GetSignInForm()
            {
                var result = new Dictionary<string, string>();
                result["username"] = _username;
                result["password"] = _password;
                result["email"] = _email;
                result["lang"] = _lang;
                result["key"] = _key;
                return result;
            }

            public Dictionary<string, string> GetLogInForm()
            {
                var result = new Dictionary<string, string>();
                result["email"] = _username;
                result["password"] = _password;
                result["form_id"] = "loginform";
                return result;
            }

            public void ClearForm()
            {
                _username = "";
                _password = "";
                _email = "";
            }

            public void UpdateUserInfos(JSONObject data)
            {
                JSONObject infos = data.GetField("infos");
                _bgaUserId = infos.GetField("id").str;
                _bgaUserName = infos.GetField("name").str;
                //_bgaUserSeal = infos.GetField("new_seal").str;

                /*JSONObject userInfos = infos.GetField("user_infos");
                _currentName = userInfos.GetField("name").str;
                _currentTable = userInfos.GetField("current_table").str;
                _currentGame = userInfos.GetField("current_game").str;
                _currentGameName = userInfos.GetField("current_game_name").str;*/
            }
            public void ClearUserInfos()
            {
                _bgaUserId = "";
                _bgaUserName = "";
            }
        }

    }
}
