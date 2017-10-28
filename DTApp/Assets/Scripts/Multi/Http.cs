using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using BestHTTP;

namespace Multi {

    public class Http
    {
        // Singleton
        static private Http _instance = null;

        public static Http Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Http();
                }
                return _instance;
            }
        }

        // public members
        public enum Mode { OFFLINE, ONLINE, LOBBY, LOGIN, SIGNIN, INGAME }

        public Web.LobbyManager Lobby;

        // set/get
        public void ChangeMode(Mode mode_) { ChangeMode_(mode_); }

        public void SetUsername(string value) { logInData.Username = value; }
        public void SetPassword(string value) { logInData.Password = value; }
        public void SetEmail(string value) { logInData.Email = value; }
        public void SetRememberMe(bool value) { logInData.RememberMe = value; }

        public string BgaUserId { get { return logInData.BgaUserId; } }
        public string BgaUserName { get { return logInData.BgaUserName; } }
        public bool isConnected { get { return BgaUserName != ""; } }
        public string Status { get { return isConnected ? "Connected As " + BgaUserName : "Not Connected"; } }

        public void LoadPersistantData()
        {
            logInData.Username = app.bgaid;
            logInData.Password = app.bgapwd;
            logInData.RememberMe = app.bgaRememberLogin;
        }

        public void SavePersistantData()
        {
            app.bgaid = logInData.Username;
            app.bgapwd = logInData.Password;
            app.bgaRememberLogin = logInData.RememberMe;
        }

        /////// Send Requests ////////
        public bool LoadTables(bool background = false)
        {
            //args["status"] = "open";
            return sendRequest("tablemanager", "tableinfos", null, LoadTablesCallBack, "Loading", background) != null;
        }

        public void CreateGame()
        {
            Debug.Assert(mode == Mode.LOBBY);
            var args = new Dictionary<string, string>();
            args["game"] = serverData.GameId;
            args["gamemode"] = "async";
            args["meeting"] = "false";
            sendRequest("tablemanager", "createnew", args, CreateGameCallBack, "Creating Game");
        }

        public void OpenGame(string tableId)
        {
            Debug.Assert(mode == Mode.LOBBY);
            sendTableRequest("openTableNow", tableId, "Opening Game", OpenGameCallBack);
        }

        public void JoinGame(string tableId)
        {
            Debug.Assert(mode == Mode.LOBBY);
            sendTableRequest("joingame", tableId, "Joining Game", JoinGameCallBack);
        }

        public void QuitGame(string tableId)
        {
            Debug.Assert(mode == Mode.LOBBY || mode == Mode.INGAME);
            sendTableRequest("quitgame", tableId, "Quitting Game", QuitGameCallBack);
        }

        public void SignIn()
        {
            Debug.Assert(mode == Mode.SIGNIN);
            var args = logInData.GetSignInForm();
            sendRequest("account", "signinapi", args, SignInCallBack, "Signing In");
        }

        public void LogIn()
        {
            Debug.Assert(mode == Mode.LOGIN || mode == Mode.ONLINE);
            var args = logInData.GetLogInForm();
            sendRequest("account", "login", args, LogInCallBack, "Connecting");
        }

        public void LogOut()
        {
            Debug.Assert(mode == Mode.LOGIN);
            var args = new Dictionary<string, string>();
            sendRequest("account", "logout", args, LogOutCallBack, "Disconnecting");
        }

        public void WatchGame(string tableId)
        {
            Debug.Assert(mode == Mode.LOBBY);

            activeTableId = tableId;
            GetGameData(activeTableId, GameDataOnStartCallBack);
        }

        public bool GetGameData(string tableId, Web.RequestHandle.DataCallBackType callback)
        {
            Debug.Assert(mode == Mode.LOBBY || mode == Mode.INGAME);
            var args = new Dictionary<string, string>();
            args["table"] = tableId;
            Web.RequestHandle handle = sendRequest(serverData.GameName, "gamedataonly", args, GetGameDataCallBack, "GetGameData");
            if (handle != null) handle.dataCallback = callback;
            return (handle != null);
        }

        public bool GetNotificationHistory(string tableId, int from, bool simpleNoteHistoric, Web.RequestHandle.DataCallBackType callback)
        {
            Debug.Assert(mode == Mode.INGAME);
            var args = new Dictionary<string, string>();
            args["table"] = tableId;
            args["from"] = from.ToString();
            args["privateinc"] = "1";
            args["history"] = simpleNoteHistoric ? "1" : "0";
            Web.RequestHandle handle = sendRequest(serverData.GameName, "notificationHistory", args, GetNotificationHistoryCallBack, "GetHistory");
            if (handle != null) handle.dataCallback = callback;
            return (handle != null);
        }

        public bool SendNotification(string type, Dictionary<string, string> args, Web.RequestHandle.SuccessCallBackType callback)
        {
            Debug.Assert(mode == Mode.INGAME);
            Web.RequestHandle handle = sendRequest(serverData.GameName, type, args, SendNotificationCallBack, "Notify");
            if (handle != null) handle.successCallback = callback;
            return (handle != null);
        }
        /////// Send Requests ////////

        /////// Request callbacks ////////
        void LoadTablesCallBack(Web.RequestHandle handle)
        {
            Debug.Assert(mode == Mode.LOBBY || mode == Mode.INGAME);
            if (handle.Success)
            {
                JSONObject data = handle.ResultData;
                JSONObject tables = data.GetField("tables");
                Logger.Instance.Log("DETAIL", tables.ToString());
                Lobby.UpdateTables(tables);
            }
        }

        void CreateGameCallBack(Web.RequestHandle handle)
        {
            Debug.Assert(mode == Mode.LOBBY);
            if (handle.Success)
            {
                JSONObject data = handle.ResultData;
                string activeTable = data.GetField("table").ToString();
                OpenGame(activeTable);
            }
        }

        void JoinGameCallBack(Web.RequestHandle handle)
        {
            Debug.Assert(mode == Mode.LOBBY);
            if (handle.Success)
            {
                JSONObject data = handle.ResultData;
                if (data.str == "ok")
                {
                    LoadTables();
                }
                else
                {
                    Logger.Instance.Log("NW_WARNING", "Server Error");
                }
            }
        }

        void OpenGameCallBack(Web.RequestHandle handle)
        {
            Debug.Assert(mode == Mode.LOBBY);
            if (handle.Success)
            {
                LoadTables();
            }
        }

        void QuitGameCallBack(Web.RequestHandle handle)
        {
            Debug.Assert(mode == Mode.LOBBY || mode == Mode.INGAME);
            if (handle.Success)
            {
                LoadTables();
            }
        }

        void SignInCallBack(Web.RequestHandle handle)
        {
            Debug.Assert(mode == Mode.SIGNIN);
            if (handle.Success)
            {
            }
        }

        void LogInCallBack(Web.RequestHandle handle)
        {
            Debug.Assert(mode == Mode.LOGIN || mode == Mode.ONLINE);
            if (handle.Success)
            {
                JSONObject data = handle.ResultData;
                if (data.GetField("success").b)
                {
                    logInData.UpdateUserInfos(data);
                    SavePersistantData();
                }
                else
                {
                    Logger.Instance.Log("NW_WARNING", "Incorrect username or password");
                    logInData.ClearUserInfos();
                }
            }
            else
            {
                logInData.ClearUserInfos();
            }
        }

        void LogOutCallBack(Web.RequestHandle handle)
        {
            Debug.Assert(mode == Mode.LOGIN);
            logInData.ClearUserInfos();
            if (handle.Success)
            {
                JSONObject data = handle.ResultData;
                if (data.GetField("success").b)
                {
                }
                else
                {
                    Logger.Instance.Log("NW_WARNING", "Not properly disconnected");
                }
            }
        }

        void GetGameDataCallBack(Web.RequestHandle handle)
        {
            Debug.Assert(mode == Mode.LOBBY || mode == Mode.INGAME);
            if (handle.Success)
            {
                JSONObject data = handle.ResultData;
                Logger.Instance.Log("DETAIL", "gamedata: " + data.ToString());
                handle.dataCallback(data);
            }
        }

        void GetNotificationHistoryCallBack(Web.RequestHandle handle)
        {
            Debug.Assert(mode == Mode.INGAME);
            if (handle.Success)
            {
                JSONObject data = handle.ResultData;
                if (data.GetField("valid").n == 1)
                {
                    JSONObject history = data.GetField("data");
                    Logger.Instance.Log("DETAIL", "history: " + history.ToString());
                    handle.dataCallback(history);
                }
                else
                {
                    Logger.Instance.Log("WARNING", "Server History Result Invalid");
                    Logger.Instance.Log("DETAIL", "data: " + data.ToString());
                    handle.dataCallback(null);
                }
            }
        }

        void SendNotificationCallBack(Web.RequestHandle handle)
        {
            Debug.Assert(mode == Mode.INGAME);
            bool success = false;
            if (handle.Success)
            {
                JSONObject data = handle.ResultData;
                if (data.HasField("valid") && data.GetField("valid").n == 1)
                {
                    success = true;
                }
                else
                {
                    Logger.Instance.Log("WARNING", "Server Notification Result Invalid");
                    Logger.Instance.Log("DETAIL", "data: " + data.ToString());
                }
            }

            handle.successCallback(success);
        }
        /////// Request callbacks ////////


        /////// Special callbacks ////////
        private void GameDataOnStartCallBack(JSONObject data)
        {
            Debug.Assert(mode == Mode.LOBBY);

            if (!Lobby.Tables.ContainsKey(activeTableId))
            {
                Logger.Instance.Log("NW_WARNING", "unknown table " + activeTableId);
                return;
            }

            ChangeMode(Mode.INGAME);

            app.loadBaseGame();
            app.onlineGameInterface.SetOnlineGame(logInData.BgaUserId, Lobby.Tables[activeTableId], data);

            GameObject manager = GameObject.Find("Manager");
            MenuManager menu = manager.GetComponent<MenuManager>();
            menu.loadLevel();
        }
        /////// Special callbacks ////////


        /////// Implementation details ////////
        private AppManager app;
        private Web.RequestManager _requestManager;
        private Web.LogInData logInData;
        private Web.BgaServerData serverData;
        private Mode mode;
        private string activeTableId = "";

        private Http()
        {
            mode = Mode.OFFLINE;
            logInData = new Web.LogInData();
            serverData = new Web.BgaServerData();
            Lobby = new Web.LobbyManager();

            // add a component requestmanager to Application Manager, so that it will never be destroyed
            app = GameObject.Find("Application Manager").GetComponent<AppManager>();
            _requestManager = app.gameObject.AddComponent<Web.RequestManager>() as Web.RequestManager;
        }

        private Web.RequestHandle sendRequest(string category, string name, Dictionary<string, string> args, Web.RequestHandle.CallBackType callback, string message, bool background = false)
        {
            Uri uri = serverData.GetRequestUri(category, name);

            HTTPRequest request;
            if (args == null)
            {
                request = new HTTPRequest(uri);
            }
            else
            {
                request = new HTTPRequest(uri, HTTPMethods.Post);
                string debug_message = "Request Form: ";
                foreach (KeyValuePair<string, string> arg in args)
                {
                    request.AddField(arg.Key, arg.Value);
                    debug_message += arg.Key + "=" + arg.Value + ";";
                }
                Logger.Instance.Log("DETAIL", debug_message);
            }

            return _requestManager.ScheduleRequest(request, message, callback, background);
        }

        private bool sendTableRequest(string name, string tableId, string message, Web.RequestHandle.CallBackType callback)
        {
            var args = new Dictionary<string, string>();
            args["table"] = tableId;
            return sendRequest("table", name, args, callback, message) != null;
        }

        private void ChangeMode_(Mode mode_)
        {
            switch (mode_)
            {
                case Mode.OFFLINE:
                    Debug.Assert(mode == Mode.ONLINE || mode == Mode.INGAME);
                    mode = mode_;
                    Abort();
                    break;
                case Mode.ONLINE:
                    Debug.Assert(mode != Mode.ONLINE && mode != Mode.INGAME);
                    mode = mode_;
                    Abort();
                    TryConnect();
                    break;
                case Mode.LOGIN:
                    Debug.Assert(mode == Mode.ONLINE);
                    mode = mode_;
                    Abort();
                    break;
                case Mode.SIGNIN:
                    Debug.Assert(mode == Mode.ONLINE);
                    mode = mode_;
                    Abort();
                    break;
                case Mode.LOBBY:
                    Debug.Assert(mode == Mode.ONLINE);
                    mode = mode_;
                    Abort();
                    break;
                case Mode.INGAME:
                    Debug.Assert(mode == Mode.LOBBY);
                    mode = mode_;
                    Abort();
                    break;
            }
        }
        
        // Kill requests
        private void Abort()
        {
            logInData.ClearForm();
            _requestManager.Abort();
        }

        // debug force connect
        private void TryConnect()
        {
            if (!isConnected && !_requestManager.OnGoing())
            {
                if (app.bgaid != null && app.bgaid != "" && app.bgapwd != null && app.bgapwd != "")
                {
                    LoadPersistantData();
                    LogIn();
                }
            }
        }
    }
}

