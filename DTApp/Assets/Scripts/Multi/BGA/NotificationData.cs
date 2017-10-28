using UnityEngine;
using System;
using System.Globalization;
using System.Collections.Generic;

namespace Multi
{
    namespace BGA
    {
        public class NotificationData
        {
            public enum Type { UNKNOWN, SIMPLE_NOTE, GAMESTATE_CHANGE, LEAVE_GAMESTATE, GAMESTATE_MULTIPLEACTIVEUPDATE, UPDATE_REFLEXIONTIME, // BGA SERVER LOGIC
                TOKENS_TO_PLACE_UPDATE, TOKENS_UPDATE, CHARACTER_MOVED, MOVINGCHARACTER_CONTINUE, CHOOSE_COMBATCARD, CHOOSE_MYCOMBATCARD, COMBAT_START, JUMP, // DT ACTIONS
                ACTIONCARD_PLAYED, COMBATCARD_REVEALED, ROOM_DISCOVERED, CHARACTER_HEALED, HERSE_MANIPULATED, ROOM_ROTATED, REMOVE_TOKENS, CHARACTER_GET_OUT, REVEAL_COMBATCARD, PASS, // DT ACTIONS
                UPDATE_MECHANISMLOCATIONS, DUMMY, TOKENS_TO_PLACE_LIST, NEWACTIONCARDS // UNUSED
            }
            public enum Status { UNKNOWN, SLEEPING, WAITING, PROCESSING, RESOLVED }


            // accessible data
            public string uid;
            public string log;
            public Type type = Type.UNKNOWN;

            // other
            private JSONObject args;
            private Status _status = Status.UNKNOWN;
            //private DateTime? _time;

           // public DateTime time { get { return _time.GetValueOrDefault(); } }
            public Status status { get { return _status; } }
            public bool isValid { get { return _status != Status.UNKNOWN; } }
            public string typeAsString { get { return TypeToString(type); } }

            public NotificationData(JSONObject json)
            {
                ParseData(json);
            }

            public void MarkAsWaiting() { Debug.Assert(status == Status.SLEEPING); _status = Status.WAITING; }
            public void MarkAsProcessing() { Debug.Assert(status == Status.WAITING); _status = Status.PROCESSING; }
            public void MarkAsResolved() { Debug.Assert(status == Status.PROCESSING); _status = Status.RESOLVED; }

            #region implementation

            private bool ParseData(JSONObject json)
            {
                /*if (_HasFieldOfTypeString(json, "time"))
                {
                    DateTime outtime;
                    if (DateTime.TryParseExact(json.GetField("time").str, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out outtime))
                    {
                        _time = outtime;
                    }
                }*/
                if (JSONTools.HasFieldOfTypeString(json, "uid")) uid = json.GetField("uid").str;
                if (JSONTools.HasFieldOfTypeString(json, "log")) log = json.GetField("log").str;
                if (JSONTools.HasFieldOfTypeString(json, "type")) type = StringToType( json.GetField("type").str );
                if (JSONTools.HasFieldOfTypeContainer(json, "args")) args = json.GetField("args");
                
                if (CheckValidity())
                {
                    _status = Status.SLEEPING;
                }

                return isValid;
            }

            private bool CheckValidity()
            {
                return /*_time.HasValue
                    && */uid != null
                    && log != null
                    && type != Type.UNKNOWN
                    && args != null;
            }

            private static string TypeToString(Type type)
            {
                switch (type)
                {
                    case Type.ACTIONCARD_PLAYED:
                        return "actionCardPlayed";
                    case Type.CHARACTER_GET_OUT:
                        return "characterGetOut";
                    case Type.CHARACTER_HEALED:
                        return "characterHealed";
                    case Type.CHARACTER_MOVED:
                        return "characterMoved";
                    case Type.COMBATCARD_REVEALED:
                        return "combatCardRevealed";
                    case Type.COMBAT_START:
                        return "combatStart";
                    case Type.CHOOSE_COMBATCARD:
                        return "chooseCombatCard";
                    case Type.CHOOSE_MYCOMBATCARD:
                        return "chooseMyCombatCard";
                    case Type.LEAVE_GAMESTATE:
                        return "leaveGameState";
                    case Type.HERSE_MANIPULATED:
                        return "hersManipulated";
                    case Type.MOVINGCHARACTER_CONTINUE:
                        return "movingCharacterContinue";
                    case Type.REMOVE_TOKENS:
                        return "removeTokens";
                    case Type.REVEAL_COMBATCARD:
                        return "revealCombatCard";
                    case Type.ROOM_DISCOVERED:
                        return "roomDiscovered";
                    case Type.ROOM_ROTATED:
                        return "roomRotated";
                    case Type.TOKENS_TO_PLACE_UPDATE:
                        return "tokensToPlaceUpdate";
                    case Type.TOKENS_UPDATE:
                        return "tokensUpdate";
                    case Type.GAMESTATE_CHANGE:
                        return "gameStateChange";
                    case Type.SIMPLE_NOTE:
                        return "simpleNote";
                    case Type.GAMESTATE_MULTIPLEACTIVEUPDATE:
                        return "gameStateMultipleActiveUpdate";
                    case Type.UPDATE_REFLEXIONTIME:
                        return "updateReflexionTime";
                    case Type.UPDATE_MECHANISMLOCATIONS:
                        return "updateMechanismLocations";
                    case Type.DUMMY:
                        return "dummy";
                    case Type.TOKENS_TO_PLACE_LIST:
                        return "tokenToPlaceList";
                    case Type.JUMP:
                        return "jump";
                    case Type.NEWACTIONCARDS:
                        return "newActionCards";
                    case Type.PASS:
                        return "playerPass";
                    case Type.UNKNOWN:
                    default:
                        return "unknown";
                }
            }

            private static Type StringToType(string type)
            {
                switch (type)
                {
                    case "actionCardPlayed":
                        return Type.ACTIONCARD_PLAYED;
                    case "characterGetOut":
                        return Type.CHARACTER_GET_OUT;
                    case "characterHealed":
                        return Type.CHARACTER_HEALED;
                    case "characterMoved":
                        return Type.CHARACTER_MOVED;
                    case "combatCardRevealed":
                        return Type.COMBATCARD_REVEALED;
                    case "chooseCombatCard":
                        return Type.CHOOSE_COMBATCARD;
                    case "chooseMyCombatCard":
                        return Type.CHOOSE_MYCOMBATCARD;
                    case "combatStart":
                        return Type.COMBAT_START;
                    case "hersManipulated":
                        return Type.HERSE_MANIPULATED;
                    case "leaveGameState":
                        return Type.LEAVE_GAMESTATE;
                    case "movingCharacterContinue":
                        return Type.MOVINGCHARACTER_CONTINUE;
                    case "removeTokens":
                        return Type.REMOVE_TOKENS;
                    case "revealCombatCard":
                        return Type.REVEAL_COMBATCARD;
                    case "roomDiscovered":
                        return Type.ROOM_DISCOVERED;
                    case "roomRotated":
                        return Type.ROOM_ROTATED;
                    case "tokensToPlaceUpdate":
                        return Type.TOKENS_TO_PLACE_UPDATE;
                    case "tokensUpdate":
                        return Type.TOKENS_UPDATE;
                    case "gameStateChange":
                        return Type.GAMESTATE_CHANGE;
                    case "simpleNote":
                        return Type.SIMPLE_NOTE;
                    case "gameStateMultipleActiveUpdate":
                        return Type.GAMESTATE_MULTIPLEACTIVEUPDATE;
                    case "updateReflexionTime":
                        return Type.UPDATE_REFLEXIONTIME;
                    case "updateMechanismLocations":
                        return Type.UPDATE_MECHANISMLOCATIONS;
                    case "tokenToPlaceList":
                        return Type.TOKENS_TO_PLACE_LIST;
                    case "dummy":
                        return Type.DUMMY;
                    case "jump":
                        return Type.JUMP;
                    case "newActionCards":
                        return Type.NEWACTIONCARDS;
                    case "playerPass":
                        return Type.PASS;
                    default:
                        return Type.UNKNOWN;
                }
            }

            private string GetStrValue(JSONObject obj, string key)
            {
                return JSONTools.GetStrValue(obj, key);
            }

            private int GetIntValue(JSONObject obj, string key)
            {
                return JSONTools.GetIntValue(obj, key, -1);
            }

            private string GetArgStrValue(int index, string key) { return GetStrValue(GetArg(index), key); }
            private int GetArgIntValue(int index, string key) { return GetIntValue(GetArg(index), key); }
            
            private bool CheckArgStrValue(int index, string key, string value)
            {
                string s = GetArgStrValue(0, key);
                return s != null && s == value;
            }
            #endregion

            public int GetArgsCount()
            {
                if (args.IsArray) return args.Count;
                else return 1;
            }
            public JSONObject GetArg(int index)
            {
                Debug.Assert(index < GetArgsCount());

                if (args.IsArray) return args[index];
                else return args;
            }

            // TESTED
            public string GetArgLocation(int index) { return GetArgStrValue(index, "location"); }
            public int GetArgLocationArg(int index) { return GetArgIntValue(index, "location_arg"); }
            public int GetArgId(int index) { return GetArgIntValue(index, "id"); }
            public int GetArgX(int index) { return GetArgIntValue(index, "x"); }
            public int GetArgY(int index) { return GetArgIntValue(index, "y"); }
            public string GetArgName(int index) { return GetArgStrValue(index, "name"); }
            public string GetArgType(int index) { return GetArgStrValue(index, "type"); }
            public string GetArgCategory(int index) { return GetArgStrValue(index, "category"); }

            public string GetTokensUpdateLocation()
            {
                Debug.Assert(type == Type.TOKENS_UPDATE || type == Type.TOKENS_TO_PLACE_UPDATE);
                return GetArgLocation(0);
            }
            public string GetGameStateName()
            {
                Debug.Assert(type == Type.GAMESTATE_CHANGE || type == Type.LEAVE_GAMESTATE);
                return GetArgName(0);
            }

            public List<string> GetActivePlayersList()
            {
                Debug.Assert(type == Type.GAMESTATE_MULTIPLEACTIVEUPDATE);
                List<string> players = new List<string>();
                for (int i=0; i<GetArgsCount(); ++i)
                {
                    JSONObject arg = GetArg(i);
                    Debug.Assert(arg.IsString);
                    players.Add(arg.str);
                }
                return players;
            }

            public string GetPlayerId(int index = 0)
            {
                return GetArgStrValue(index, "player_id");
            }
            public string GetPlayerName(int index = 0)
            {
                return GetArgStrValue(index, "player_name");
            }
            public string GetCharacterName(int index = 0)
            {
                if (type == Type.REMOVE_TOKENS || type == Type.COMBATCARD_REVEALED) return GetArgStrValue(index, "character_name").ToLower();
                else return GetArgStrValue(index, "character").ToLower();
            }
            public string GetNewActivePlayer(int index = 0)
            {
                return GetArgStrValue(index, "active_player");
            }

            public bool IsCharacterToken(int index = 0)
            {
                Debug.Assert(type == Type.TOKENS_UPDATE || type == Type.TOKENS_TO_PLACE_UPDATE);
                return CheckArgStrValue(index, "category", "character");
            }
            public bool IsItemToken(int index = 0)
            {
                Debug.Assert(type == Type.TOKENS_UPDATE || type == Type.TOKENS_TO_PLACE_UPDATE);
                return CheckArgStrValue(index, "category", "item");
            }
            public int GetTokenHolderId(int index = 0)
            {
                Debug.Assert(type == Type.TOKENS_UPDATE || type == Type.TOKENS_TO_PLACE_UPDATE);
                return CheckArgStrValue(index, "location", "carried") ? GetArgLocationArg(index) : 0;
            }

            public string GetSimpleNoteMessage()
            {
                Debug.Assert(type == Type.SIMPLE_NOTE);
                string message = log;
                for (int i=0; i<GetArgsCount(); ++i)
                {
                    JSONObject arg = GetArg(i);
                    Debug.Assert(arg.IsObject);
                    foreach (string key in arg.keys)
                    {
                        string value = GetArgStrValue(i, key);
                        message.Replace("${" + key + "}", value);
                    }
                }
                return message;
            }

            public int GetTimerDelta(int index = 0)
            {
                Debug.Assert(type == Type.UPDATE_REFLEXIONTIME);
                return GetArgIntValue(index, "delta");
            }
            public int GetTimerMax(int index = 0)
            {
                Debug.Assert(type == Type.UPDATE_REFLEXIONTIME);
                return GetArgIntValue(index, "max");
            }

            public int GetActionCardValue()
            {
                Debug.Assert(type == Type.ACTIONCARD_PLAYED);
                return GetArgIntValue(0, "card_type"); //GetArgIntValue(0, "nbr");
            }
            public int GetCombatCardValue()
            {
                if (type == Type.CHOOSE_MYCOMBATCARD)
                    return GetArgIntValue(0, "card_value");
                else if (type == Type.REVEAL_COMBATCARD)
                    return GetArgIntValue(0, "value");
                Debug.Assert(false);
                return -1;
            }
            public int GetCombatTotalValue()
            {
                Debug.Assert(type == Type.REVEAL_COMBATCARD);
                return GetArgIntValue(0, "combat");
            }

            public int GetTileId()
            {
                return GetArgIntValue(0, "tile_id");
            }
            public int GetTileType()
            {
                return GetArgIntValue(0, "tile_type");
            }
            public int GetTileOrientation()
            {
                return GetArgIntValue(0, "orientation");
            }
            public bool GetTileRotationClockwize()
            {
                return GetArgIntValue(0, "clockwize") == 1 ? true : false;
            }
            public int GetTileRotationDirection()
            {
                return GetArgIntValue(0, "direction");
            }
            public int GetTileRotationNbAction()
            {
                return GetArgIntValue(0, "nb_action");
            }

            public string GetCharacterOwner(int index = 0)
            {
                return GetArgStrValue(index, "character_player_id");
            }
            public int GetScore(int index = 0)
            {
                return GetArgIntValue(index, "score");
            }
            public int GetCharacterPoints(int index = 0)
            {
                return GetArgIntValue(index, "points");
            }
            public List<int> GetRemoveTokenIds()
            {
                Debug.Assert(type == Type.CHARACTER_GET_OUT || type == Type.REMOVE_TOKENS);
                var tokensField = "";
                if (type == Type.CHARACTER_GET_OUT) tokensField = "remove_tokens";
                else if (type == Type.REMOVE_TOKENS) tokensField = "tokens";
                else Debug.Assert(false);
                List<int> ids = new List<int>();
                JSONObject arg = GetArg(0);
                Debug.Assert(JSONTools.HasFieldOfTypeArray(arg, tokensField));
                foreach (var obj in arg.GetField(tokensField).list)
                {
                    int id = 0;
                    if (obj.IsString)
                    {
                        int.TryParse(obj.str, out id);
                    }
                    else if (obj.IsNumber)
                    {
                        id = Mathf.RoundToInt(obj.n);
                    }
                    Debug.Assert(id > 0);
                    ids.Add(id);
                }
                return ids;
            }

            private List<string> GetFighterNames(string group)
            {
                List<string> fighters = new List<string>();
                JSONObject arg = GetArg(0);
                Debug.Assert(JSONTools.HasFieldOfTypeObject(arg, group));
                JSONObject figthers = arg.GetField(group);
                Debug.Assert(JSONTools.HasFieldOfTypeObject(figthers, "args"));
                JSONObject args = figthers.GetField("args");
                foreach (var key in args.keys)
                {
                    fighters.Add( JSONTools.GetStrValue(args, key) );
                }
                return fighters;
            }
            public List<string> GetAttackerNames()
            {
                return GetFighterNames("attackers");
            }
            public List<string> GetDefenderNames()
            {
                return GetFighterNames("defenders");
            }

            public string GetHerseManipulatedNewState()
            {
                return GetArgStrValue(0, "newState");
            }
            private int GetHerseManipulatedCell(int side, string key)
            {
                if (JSONTools.HasFieldOfTypeArray(GetArg(0), "hers"))
                {
                    JSONObject herse = GetArg(0).GetField("hers");
                    if (side < herse.Count)
                        return GetIntValue(herse[side], key);
                }
                return -1;
            }
            public int GetHerseManipulatedCharacterLocationX() { return GetHerseManipulatedCell(0, "x"); }
            public int GetHerseManipulatedCharacterLocationY() { return GetHerseManipulatedCell(0, "y"); }
            public int GetHerseManipulatedDirectionX() { return GetHerseManipulatedCell(1, "x"); }
            public int GetHerseManipulatedDirectionY() { return GetHerseManipulatedCell(1, "y"); }

            public int GetAttackerScore()
            {
                return GetArgIntValue(0, "attaquer_score_win");
            }
            public int GetDefenderScore()
            {
                return GetArgIntValue(0, "defender_score_win");
            }
            public string GetWinnerId()
            {
                return GetArgStrValue(0, "winner_id");
            }
            private List<int> GetVictimIds(string group)
            {
                List<int> victims = new List<int>();
                JSONObject arg = GetArg(0);
                Debug.Assert(JSONTools.HasFieldOfTypeArray(arg, group));
                foreach (var victim in arg.GetField(group).list)
                {
                    Debug.Assert(JSONTools.GetIntValue(victim, "id", -1) != -1);
                    victims.Add(JSONTools.GetIntValue(victim, "id"));
                }
                return victims;
            }
            public List<int> GetWoundedIds()
            {
                return GetVictimIds("wounded_list");
            }
            public List<int> GetKilledIds()
            {
                return GetVictimIds("dead_list");
            }
            public int GetAttackerCardValue()
            {
                return GetArgIntValue(0, "attaquer_card_type");
            }
            public int GetDefenderCardValue()
            {
                return GetArgIntValue(0, "defender_card_type");
            }


            public string GetHealerOwnerId()
            {
                return GetArgStrValue(0, "healer_player_id");
            }
            public string GetHealerType()
            {
                return GetArgStrValue(0, "healer_character_type");
            }
            public int GetHealerTargetId()
            {
                return GetArgIntValue(0, "character_id");
            }
        }
	}
}
