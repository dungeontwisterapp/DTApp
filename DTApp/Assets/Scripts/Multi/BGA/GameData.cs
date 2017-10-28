using UnityEngine;
using System.Collections.Generic;
using Actions;

namespace Multi
{
    namespace BGA
    {
        /// This class has the following purposes:
        /// - managing network information (through the control of packethandler and boarddata)
        /// - provide an access to common boarddata methods
        /// - give access to server notifications to be processed
        /// - send record notifications
        public class GameData {

            private PacketHandler packetHandler;
            private BoardData _board;


            public GameData()
            {
                packetHandler = new PacketHandler();

                Clear();
            }

            public void Clear()
            {
                _board = new BoardData();
                packetHandler.Clear();
            }

            public void SetOnlineGame(BoardData board, int initMoveId, int initPacketId)
            {
                Clear();
                _board = board;
                packetHandler.Reset(initMoveId, initPacketId);
                packetHandler.ForceUpdate();
            }
    
            public void Start()
            {
                if (isOnline)
                {
                    packetHandler.Start();
                }
            }

            public void Update()
            {
                Debug.Assert(isOnline);
                {
                    packetHandler.Update(table.id);
                }
            }

            ////// data accessors //////
            public BoardData board { get { return _board; } }
            public bool isOnline { get { return board.isOnline; } }
            public string viewerId { get { return board.viewerId; } }
            public TableData table { get { Debug.Assert(isOnline, "online game is not set"); return board.table; } }
            public int playerCount { get { Debug.Assert(isOnline, "online game is not set"); return board.playerCount; } }
            public bool isObs { get { Debug.Assert(isOnline, "online game is not set"); return board.isObs; } }
            public bool isBoardReady { get { return board.isReady; } }
            public bool isSelected { get { return board.movingToken != BoardData.NO_TOKEN; } }
            public int movingCharacterId { get { Debug.Assert(isSelected); return board.movingToken; } }

            public string GetPlayerName(int index) { return board.GetPlayerName(index); }
            public string GetPlayerId(int index) { return board.GetPlayerId(index); }
            public int GetPlayerIndex(string playerId) { return board.GetPlayerIndexFromId(playerId); }
            ////// data accessors //////

                
            ////// Notifications Processing //////
            public bool HasProcessingNotification()
            {
                NotificationData notif = packetHandler.GetCurrentNotification();
                return (notif != null && notif.status == NotificationData.Status.PROCESSING);
            }

            public bool HasWaitingNotification()
            {
                if (!HasProcessingNotification())
                {
                    NotificationData notif = packetHandler.GetCurrentNotification();
                    return (notif != null && notif.status == NotificationData.Status.WAITING);
                }
                return false;
            }

            public NotificationData ProcessWaitingNotification()
            {
                Debug.Assert(HasWaitingNotification());
                return packetHandler.ProcessCurrentNotification();
            }

            public void ResolveCurrentNotification()
            {
                Debug.Assert(HasProcessingNotification());
                packetHandler.ResolveCurrentNotification();
            }

            public bool IsProcessingCurrentMove()
            {
                return packetHandler.IsProcessingCurrentMove();
            }

            public void EnableAction(bool enable)
            {
                if (enable)
                {
                    Logger.Instance.Log("WARNING", "Enable Actions");
                    board.GameIsReady();
                }
                else
                {
                    Logger.Instance.Log("WARNING", "Disable Actions");
                    Debug.Assert(false, "TODO: implement the new method to freeze notif execution when game is processins");
                }
            }
            ////// Notifications Processing //////


            ////// GAME STATE //////
            public string GetCurrentState() { return board.GetCurrentState(); }
            public bool WaitingForExpectedState() { return board.WaitingForExpectedState(); }

            public void RegisterToken(string playerId, string tokenName, int bgaId) { board.RegisterToken(playerId, board.ConvertToBGA(tokenName), bgaId); }
            public void SetCarriedToken(int character, int token) { board.SetCarriedToken(character, token); }
            public void SetDropped(int token) { board.SetCarriedToken(BoardData.NO_TOKEN, token); }
            public int GetCarriedToken(int character) { return board.GetCarriedToken(character); }
            public bool IsCarrier(int character) { return board.IsCarrier(character); }
            public void LeaveCurrentState() { board.LeaveCurrentState(); }
            public void LeaveState(string statename, string activePlayer) { board.LeaveState(statename, activePlayer); }
            public void EnterState(string statename, string activePlayer) { board.EnterState(statename, activePlayer); }
            public void UpdateActivePlayers(List<string> activePlayers) { board.UpdateActivePlayers(activePlayers); }
            public bool WaitForGameToCatchUp() { return board.WaitForGameToCatchUp(); }
            ////// GAME STATE //////


            ////// RECORDING / SEND NOTIFICATIONS //////
            public void RecordAction(ActionType type, int player, int affiliation, string charname, int affiliationTarget, string charnameTarget)
            {
                Logger.Instance.Log("LOG", FormatRecord(player) + FormatToken(affiliation, charname) + " do " + board.ActionTypeToString(type) + " to " + FormatToken(affiliationTarget, charnameTarget));
                Debug.Assert(isSelected);
                Debug.Assert(packetHandler.notifSender.buildingType == "", "two notifications are built at the same time");
                Debug.Assert(packetHandler.notifSender.buildingArgs.Count == 0, "two notifications are built at the same time");

                if (type == ActionType.ATTACK) board.AddExpectedState("combatChooseCard");
                else board.AddExpectedStates(new List<string> { "endAction", "playerChooseAction" });

                board.UnselectToken();
                board.RemoveCarriedTokenBackup();

                switch (type)
                {
                    case ActionType.ATTACK:
                        packetHandler.notifSender.buildingType = "attack";
                        packetHandler.notifSender.buildingArgs["target_id"] = GetTokenStrId(affiliationTarget, charnameTarget);
                        break;
                    case ActionType.CLOSEDOOR:
                    case ActionType.OPENDOOR:
                    case ActionType.DESTROYDOOR:
                        packetHandler.notifSender.buildingType = "manipulateHers";
                        packetHandler.notifSender.buildingArgs["hersaction"] = (type == ActionType.CLOSEDOOR) ? "close" : (type == ActionType.OPENDOOR) ? "open" : "break";
                        packetHandler.notifSender.buildingArgs["character_id"] = GetTokenStrId(affiliation, charname);
                        break;
                    case ActionType.FIREBALL:
                        packetHandler.notifSender.buildingType = "useFireStaff";
                        packetHandler.notifSender.buildingArgs["target"] = GetTokenStrId(affiliationTarget, charnameTarget);
                        packetHandler.notifSender.buildingArgs["character"] = GetTokenStrId(affiliation, charname);
                        break;
                    case ActionType.HEAL:
                    case ActionType.REGENERATE:
                        packetHandler.notifSender.buildingType = "heal";
                        packetHandler.notifSender.buildingArgs["target"] = GetTokenStrId(affiliationTarget, charnameTarget);
                        packetHandler.notifSender.buildingArgs["character"] = GetTokenStrId(affiliation, charname);
                        break;
                    case ActionType.SPEEDPOTION:
                        packetHandler.notifSender.buildingType = "useSpeedPotion";
                        packetHandler.notifSender.buildingArgs["character"] = GetTokenStrId(affiliation, charname);
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }

                packetHandler.notifSender.buildingArgs["table"] = table.id;
                packetHandler.notifSender.EnqueueNotification();
            }

            public void RecordStartMove(int player, int affiliation, string charname)
            {
                if (isSelected)
                {
                    Debug.Assert(GetTokenId(affiliation, charname) == movingCharacterId);
                    Logger.Instance.Log("LOG", "RecordStartMove skipped: already moving");
                    return;
                }

                Logger.Instance.Log("LOG", FormatRecord(player) + "starts moving " + FormatToken(affiliation, charname));
                Debug.Assert(packetHandler.notifSender.buildingType == "", "two notifications are built at the same time");
                Debug.Assert(packetHandler.notifSender.buildingArgs.Count == 0, "two notifications are built at the same time");

                board.AddExpectedState("movingCharacter");
                board.CreateCarriedTokenBackup();
                board.SelectToken(GetTokenId(affiliation, charname));

                packetHandler.notifSender.buildingType = "startMoving";
                packetHandler.notifSender.buildingArgs["token_id"] = movingCharacterId.ToString();
                packetHandler.notifSender.buildingArgs["table"] = table.id;
                packetHandler.notifSender.EnqueueNotification();
            }

            public void RecordCarriedToken(int tokenPickOwner, string tokenPickType)
            {
                Debug.Assert(isSelected);
                int tokenPickId = (tokenPickType != null) ? GetTokenId(tokenPickOwner, tokenPickType) : BoardData.NO_TOKEN;
                int synchTokenPickId = board.IsCarrier(movingCharacterId) ? board.GetCarriedToken(movingCharacterId) : BoardData.NO_TOKEN;
                if (tokenPickId != synchTokenPickId) // must synchronise bga side
                {
                    if (tokenPickId == BoardData.NO_TOKEN) // must drop on bga side
                    {
                        RecordDropToken(synchTokenPickId);
                    }
                    else if (synchTokenPickId == BoardData.NO_TOKEN) // must pick on bga side
                    {
                        if (board.IsCarried(tokenPickId)) // must be picked by exchange with another character
                        {
                            RecordExchangeToken(tokenPickId, BoardData.NO_TOKEN);
                        }
                        else // must be picked on the ground
                        {
                            RecordPickToken(tokenPickId);
                        }
                    }
                    else // exchange 2 tokens
                    {
                        if (board.IsCarried(tokenPickId)) // must be exchanged with another character
                        {
                            RecordExchangeToken(tokenPickId, synchTokenPickId);
                        }
                        else // must be dropped then picked on the ground
                        {
                            RecordDropToken(synchTokenPickId);
                            RecordPickToken(tokenPickId);
                        }
                    }
                }
                board.SetCarriedToken(movingCharacterId, tokenPickId); // update synchronised version
            }

            public void RecordMove(ActionType type, int player, int x, int y, bool endAction)
            {
                Logger.Instance.Log("LOG", FormatRecord(player) + FormatToken(movingCharacterId) + " do " + board.ActionTypeToString(type) + " to " + FormatCell(x, y));
                Debug.Assert(packetHandler.notifSender.buildingType == "", "two notifications are built at the same time");
                Debug.Assert(packetHandler.notifSender.buildingArgs.Count == 0, "two notifications are built at the same time");
                Debug.Assert(type == ActionType.WALK || type == ActionType.JUMP || type == ActionType.WALLWALK);

                if (endAction)
                {
                    board.AddExpectedStates(new List<string> { "endAction", "playerChooseAction" });
                    board.UnselectToken();
                    board.RemoveCarriedTokenBackup();
                }
                else
                {
                    board.AddExpectedState("movingCharacterContinue");
                }

                packetHandler.notifSender.buildingType = "move";
                packetHandler.notifSender.buildingArgs["x"] = board.ConvertX(x).ToString();
                packetHandler.notifSender.buildingArgs["y"] = board.ConvertY(y).ToString();
                packetHandler.notifSender.buildingArgs["table"] = table.id;
                packetHandler.notifSender.EnqueueNotification();
            }

            public void RecordCancelMove(int player)
            {
                Logger.Instance.Log("LOG", FormatRecord(player) + "cancel move");
                Debug.Assert(isSelected);
                Debug.Assert(packetHandler.notifSender.buildingType == "", "two notifications are built at the same time");
                Debug.Assert(packetHandler.notifSender.buildingArgs.Count == 0, "two notifications are built at the same time");

                board.AddExpectedState("playerChooseAction");

                board.UnselectToken();
                board.RestoreCarriedTokenBackup();

                packetHandler.notifSender.buildingType = "cancelMove";
                packetHandler.notifSender.buildingArgs["table"] = table.id;
                packetHandler.notifSender.EnqueueNotification();
            }

            public void RecordEndMove(int player)
            {
                Logger.Instance.Log("LOG", FormatRecord(player) + "end move");
                Debug.Assert(isSelected);
                Debug.Assert(packetHandler.notifSender.buildingType == "", "two notifications are built at the same time");
                Debug.Assert(packetHandler.notifSender.buildingArgs.Count == 0, "two notifications are built at the same time");

                board.AddExpectedStates(new List<string> { "endAction", "playerChooseAction" });

                board.UnselectToken();
                board.RemoveCarriedTokenBackup();

                packetHandler.notifSender.buildingType = "endMove";
                packetHandler.notifSender.buildingArgs["table"] = table.id;
                packetHandler.notifSender.EnqueueNotification();
            }

            public void RecordPass(int player)
            {
                if (isSelected)
                {
                    Logger.Instance.Log("LOG", "Force cancel move to pass");
                    RecordCancelMove(player);
                }

                Logger.Instance.Log("LOG", FormatRecord(player) + "pass");
                Debug.Assert(packetHandler.notifSender.buildingType == "", "two notifications are built at the same time");
                Debug.Assert(packetHandler.notifSender.buildingArgs.Count == 0, "two notifications are built at the same time");

                board.AddExpectedStates(new List<string> { "endAction", "endTurn", "playerChooseCard" } );

                packetHandler.notifSender.buildingType = "pass";
                packetHandler.notifSender.buildingArgs["table"] = table.id;
                packetHandler.notifSender.EnqueueNotification();
            }

            public void RecordCard(ActionType type, int player, int value)
            {
                Logger.Instance.Log("LOG", FormatRecord(player) + "plays " + board.ActionTypeToString(type) + " with value " + value);
                Debug.Assert(packetHandler.notifSender.buildingType == "", "two notifications are built at the same time");
                Debug.Assert(packetHandler.notifSender.buildingArgs.Count == 0, "two notifications are built at the same time");
                if (type == ActionType.ACTION_CARD)
                {
                    Debug.Assert(!isSelected);

                    board.AddExpectedState("playerChooseAction");

                    packetHandler.notifSender.buildingType = "playActionCard";
                }
                else if (type == ActionType.COMBAT_CARD)
                {
                    //Debug.Assert(isSelected);
                    Debug.Assert(board.GetCurrentExpectedState() == "combatChooseCard");

                    packetHandler.notifSender.buildingType = "playCombatCard";
                }
                else Debug.Assert(false);
                packetHandler.notifSender.buildingArgs["table"] = table.id;
                packetHandler.notifSender.buildingArgs["value"] = value.ToString();
                packetHandler.notifSender.EnqueueNotification();
            }

            public void RecordPlacementCharacterChoice(int player, int affiliation, string tokenname, int x, int y)
            {
                Logger.Instance.Log("LOG", FormatRecord(player) + FormatToken(affiliation, tokenname) + " placed at " + FormatCell(x, y));
                Debug.Assert(!isSelected);
                Debug.Assert(board.GetCurrentExpectedState() == "characterChoice", "Invalid state: " + board.GetCurrentExpectedState());
                if (packetHandler.notifSender.buildingType == "")
                {
                    Debug.Assert(packetHandler.notifSender.buildingArgs.Count == 0, "two notifications are built at the same time");
                    packetHandler.notifSender.buildingType = "characterChoice";
                    packetHandler.notifSender.buildingArgs["table"] = table.id;
                    packetHandler.notifSender.buildingArgs["tokens"] = "";
                }
                Debug.Assert(packetHandler.notifSender.buildingType == "characterChoice", "two notifications are built at the same time");
                packetHandler.notifSender.buildingArgs["tokens"] += y.ToString() + ',' + board.ConvertToBGA(tokenname) + ';';
                if (packetHandler.notifSender.buildingArgs["tokens"].Split(';').Length - 1 == 4)
                {
                    packetHandler.notifSender.EnqueueNotification();
                }
            }

            public void RecordPlacementOnRoomDiscovered(int player, int affiliation, string tokenname, int x, int y)
            {
                Logger.Instance.Log("LOG", FormatRecord(player) + FormatToken(affiliation, tokenname) + " placed at " + FormatCell(x, y));
                //Debug.Assert(!isSelected);
                Debug.Assert(packetHandler.notifSender.buildingType == "", "two notifications are built at the same time");
                Debug.Assert(packetHandler.notifSender.buildingArgs.Count == 0, "two notifications are built at the same time");

                if (board.GetCurrentExpectedState() == "discoverRoomPlaceToken") board.AddExpectedState("discoverRoomPlaceTokenNextToken");
                else board.AddExpectedStates(new List<string> { "discoverRoomPlaceToken", "discoverRoomPlaceTokenNextToken" });

                Debug.Assert(board.GetCurrentExpectedState() == "discoverRoomPlaceTokenNextToken", "Invalid state: " + board.GetCurrentExpectedState());

                packetHandler.notifSender.buildingType = "placeTokenOnMap";
                packetHandler.notifSender.buildingArgs["table"] = table.id;
                packetHandler.notifSender.buildingArgs["type"] = board.ConvertToBGA(tokenname);
                packetHandler.notifSender.buildingArgs["x"] = board.ConvertX(x).ToString();
                packetHandler.notifSender.buildingArgs["y"] = board.ConvertY(y).ToString();
                packetHandler.notifSender.buildingArgs["player_id"] = board.GetPlayerId(affiliation);
                packetHandler.notifSender.EnqueueNotification();
            }

            public void RecordPlacement(int player, int affiliation, string tokenname, int tileindex)
            {
                Logger.Instance.Log("LOG", FormatRecord(player) + FormatToken(affiliation, tokenname) + " placed on tile " + board.ConvertTile(tileindex));
                Debug.Assert(!isSelected);
                Debug.Assert(packetHandler.notifSender.buildingType == "", "two notifications are built at the same time");
                Debug.Assert(packetHandler.notifSender.buildingArgs.Count == 0, "two notifications are built at the same time");

                board.AddExpectedState("placeTokenNextPlayer");

                packetHandler.notifSender.buildingType = "placeTokenOnTile";
                packetHandler.notifSender.buildingArgs["table"] = table.id;
                packetHandler.notifSender.buildingArgs["type"] = board.ConvertToBGA(tokenname);
                packetHandler.notifSender.buildingArgs["tile_id"] = board.ConvertTile(tileindex).ToString();
                packetHandler.notifSender.EnqueueNotification();
            }

            public void RecordReveal(int player, int tileindex)
            {
                Logger.Instance.Log("LOG", FormatRecord(player) + "reveals tile " + tileindex);
                Debug.Assert(isSelected);
                Debug.Assert(packetHandler.notifSender.buildingType == "", "two notifications are built at the same time");
                Debug.Assert(packetHandler.notifSender.buildingArgs.Count == 0, "two notifications are built at the same time");

                Debug.Assert(board.GetCurrentExpectedState() == "playerChooseAction" || board.GetCurrentExpectedState() == "movingCharacter");

                packetHandler.notifSender.buildingType = "discoverRoom";
                packetHandler.notifSender.buildingArgs["table"] = table.id;
                packetHandler.notifSender.buildingArgs["tile_id"] = board.ConvertTile(tileindex).ToString();
                packetHandler.notifSender.EnqueueNotification();
            }

            public void RecordRotation(int player, int tileindex, int numberOfTurns, bool clockwize)
            {
                Logger.Instance.Log("LOG", FormatRecord(player) + "rotates tile " + board.ConvertTile(tileindex) + (clockwize ? " clockwize" : " counter-clockwize"));
                Debug.Assert(isSelected);
                Debug.Assert(packetHandler.notifSender.buildingType == "", "two notifications are built at the same time");
                Debug.Assert(packetHandler.notifSender.buildingArgs.Count == 0, "two notifications are built at the same time");

                board.AddExpectedStates(new List<string> { "endAction", "playerChooseAction" });

                board.UnselectToken();
                board.RemoveCarriedTokenBackup();

                packetHandler.notifSender.buildingType = "rotateRoom";
                packetHandler.notifSender.buildingArgs["table"] = table.id;
                packetHandler.notifSender.buildingArgs["tile_id"] = board.ConvertTile(tileindex).ToString();
                packetHandler.notifSender.buildingArgs["clockwize"] = clockwize ? "1" : "-1";
                packetHandler.notifSender.buildingArgs["nb_turn"] = numberOfTurns.ToString();
                packetHandler.notifSender.EnqueueNotification();
            }

            public void ForfeitGame()
            {
                // TODO : A lot
                Http.Instance.QuitGame(table.id);
            }
            ////// RECORDING / SEND NOTIFICATIONS //////


            ////// RECORDING Implementation specifics //////
            private int GetTokenId(int playerIndex, string tokenName) { return board.GetTokenId(playerIndex, board.ConvertToBGA(tokenName)); }
            private string GetTokenStrId(int playerIndex, string tokenName) { return GetTokenId(playerIndex, tokenName).ToString(); }
            private string FormatRecord(int player) { return "GameData: Record " + GetPlayerName(player) + ": "; }
            private string FormatToken(int id)
            {
                if (id == BoardData.NO_TOKEN)
                {
                    return "nothing";
                }
                else
                {
                    int player; string token; board.GetTokenFromId(id, out player, out token);
                    return GetPlayerName(player) + "'s " + token;
                }
            }
            private string FormatToken(int player, string token) { return GetPlayerName(player) + "'s " + board.ConvertToBGA(token); }
            private string FormatCell(int x, int y) { return "(" + x + "," + y + ")"; }

            private void RecordPickToken(int tokenId)
            {
                Debug.Assert(tokenId != BoardData.NO_TOKEN);
                Debug.Assert(isSelected);
                Logger.Instance.Log("LOG", FormatToken(movingCharacterId) + " pick " + FormatToken(tokenId));

                board.AddExpectedState("movingCharacterContinue");

                int tokenOwner;
                string tokenType;
                board.GetTokenFromId(tokenId, out tokenOwner, out tokenType);
                packetHandler.notifSender.buildingType = "pickToken";
                packetHandler.notifSender.buildingArgs["table"] = table.id;
                packetHandler.notifSender.buildingArgs["object"] = tokenType;
                packetHandler.notifSender.buildingArgs["object_player_id"] = GetPlayerId(tokenOwner);
                packetHandler.notifSender.EnqueueNotification();
            }

            private void RecordDropToken(int tokenId)
            {
                Debug.Assert(tokenId != BoardData.NO_TOKEN);
                Debug.Assert(isSelected);
                Logger.Instance.Log("LOG", FormatToken(movingCharacterId) + " drop " + FormatToken(tokenId));

                board.AddExpectedState("movingCharacterContinue");

                int tokenOwner;
                string tokenType;
                board.GetTokenFromId(tokenId, out tokenOwner, out tokenType);
                packetHandler.notifSender.buildingType = "releaseToken";
                packetHandler.notifSender.buildingArgs["table"] = table.id;
                packetHandler.notifSender.buildingArgs["object"] = tokenType;
                packetHandler.notifSender.buildingArgs["object_player_id"] = GetPlayerId(tokenOwner);
                packetHandler.notifSender.EnqueueNotification();
            }

            // name "0" for a token means no token
            private void RecordExchangeToken(int tokenTakenId, int tokenGivenId)
            {
                Debug.Assert(tokenTakenId != BoardData.NO_TOKEN || tokenGivenId != BoardData.NO_TOKEN, "at least 1 token must be exchanged");
                Debug.Assert(isSelected);
                Logger.Instance.Log("LOG", FormatRecord(movingCharacterId) + " exchange " + FormatToken(tokenGivenId) + " for " + FormatToken(tokenTakenId));

                board.AddExpectedState("movingCharacterContinue");

                int tokenTakenOwner = 0;
                string tokenTakenType = "0";
                if (tokenTakenId != BoardData.NO_TOKEN) board.GetTokenFromId(tokenTakenId, out tokenTakenOwner, out tokenTakenType);
                int tokenGivenOwner = 0;
                string tokenGivenType = "0";
                if (tokenGivenId != BoardData.NO_TOKEN) board.GetTokenFromId(tokenGivenId, out tokenGivenOwner, out tokenGivenType);
                packetHandler.notifSender.buildingType = "exchangeToken";
                packetHandler.notifSender.buildingArgs["table"] = table.id;
                packetHandler.notifSender.buildingArgs["object"] = tokenTakenType;
                packetHandler.notifSender.buildingArgs["object_player_id"] = tokenTakenOwner.ToString();
                packetHandler.notifSender.buildingArgs["our_object"] = tokenGivenType;
                packetHandler.notifSender.buildingArgs["our_object_player_id"] = tokenGivenOwner.ToString();
                packetHandler.notifSender.EnqueueNotification();
            }
            ////// RECORDING Implementation specifics //////
        }
    }
}
