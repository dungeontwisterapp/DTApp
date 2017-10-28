using UnityEngine;
using System;
using System.Collections.Generic;

namespace Multi
{
    namespace BGA
    {
        /// This class has the following purposes:
        /// - keeping informations concerning bga game state up to date
        /// - providing access to bga game state informations
        /// - providing methods to convert between app and bga states
        public class BoardData
        {
            public const int NO_TOKEN = 0;

            // private members list
            private TableData _table;
            private string _viewerId;
            private bool _online;
            private string _bgaFirstPlayerId;

            private string _currentState;
            private bool _stateLess;
            private bool _criticalState;
            private int _movingToken;
            private Queue<string> _expectedStates;
            private string _lastExpectedState;
            private List<string> _activePlayers;

            private Dictionary<string, string> mapGameToBGA, mapBGAToGame;
            private List<Dictionary<string, int>> tokenIdMaps;
            private Dictionary<int, int> tokenCarriedMap, tokenCarriedBackup;
            private Dictionary<int, int> tileOrientationMap;
            private Dictionary<string, List<string>> stateTransitions;
            private Dictionary<string, StateType> stateTypes;

            private enum StateType { UNKNOWN, MANAGER, GAME, ACTIVE, MULTIPLE }
            // private members list

            public BoardData()
            {
                Prepare();
                ClearOnlineData();

                _online = false;
                _viewerId = "0";
            }

            public BoardData(string viewerId, TableData data)
            {
                Prepare();
                ClearOnlineData();

                _online = true;
                _viewerId = viewerId;

                _table = data;
            }

            public void Reset(string firstPlayerId, string gamestate, List<string> activePlayers, int selectedId)
            {
                _bgaFirstPlayerId = firstPlayerId;
                _currentState = gamestate;
                _activePlayers = new List<string>(activePlayers);
                _criticalState = true;
                _stateLess = false; // Assumption: game loaded from bga is never stateless
                _movingToken = selectedId;
            }

            ////// data accessors //////
            public bool flipBoardDisplay { get { Debug.Assert(!isOnline || _bgaFirstPlayerId != null); return (!isOnline) ? false : (_bgaFirstPlayerId != _viewerId) ; } }
            public string viewerId { get { return _viewerId; } }
            public TableData table { get { return _table; } }
            public bool isOnline { get { return _online; } }
            public string firstPlayerToPlaceId { get { return TokenPlacementReady() ? _activePlayers[0] : null; } }
            public string firstPlayerToPlayId { get { return PlayerTurnStart() ? _activePlayers[0] : null; } }
            public int playerCount { get { return table.playerCount; } }
            public bool isObs { get { Debug.Assert(table.playerCount == 2); return (table.player1.id != viewerId) && (table.player2.id != viewerId); } }
            public bool isReady { get { return !isOnline || _bgaFirstPlayerId != null; } }
            ////// data accessors //////


            ////// bga data conversion methods /////
            public string ConvertToBGA(string name)
            {
                Debug.Assert(mapGameToBGA.ContainsKey(name), name + " is not a referenced name on app side");
                return mapGameToBGA[name];
            }

            public string ConvertToGame(string name)
            {
                Debug.Assert(mapBGAToGame.ContainsKey(name), name + " is not a referenced name on bga side");
                return mapBGAToGame[name];
            }

            public string ActionTypeToString(ActionType type)
            {
                return System.Enum.GetName(typeof(ActionType), type);
            }

            private PlayerData GetOnlinePlayer(int index)
            {
                Debug.Assert(table.playerCount == 2);
                Debug.Assert(index >= 0 && index < 2);

                // if current player is player 2, then switch players so that current player POV is always player 1

                if (table.player2.id == viewerId)
                {
                    return (index == 0) ? table.player2 : table.player1;
                }
                else
                {
                    return (index == 0) ? table.player1 : table.player2;
                }
            }

            public string GetPlayerName(int index)
            {
                if (isOnline)
                {
                    return GetOnlinePlayer(index).fullname;
                }
                else
                {
                    return (index == 0) ? "Joueur Jaune" : "Joueur Bleu";
                }
            }

            public string GetPlayerId(int index)
            {
                if (isOnline)
                {
                    return GetOnlinePlayer(index).id;
                }
                else
                {
                    return index.ToString();
                }
            }

            public int GetPlayerIndexFromId(string id)
            {
                if (GetPlayerId(0) == id) return 0;
                else if (GetPlayerId(1) == id) return 1;
                else return -1;
            }

            public int GetPlayerIndexFromName(string name)
            {
                if (GetPlayerName(0) == name) return 0;
                else if (GetPlayerName(1) == name) return 1;
                else return -1;
            }

            public int ConvertX(int x)
            {
                return flipBoardDisplay ? 21 - x : x;
            }
            public int ConvertY(int y)
            {
                return flipBoardDisplay ? 9 - y : y;
            }
            public int ConvertTile(int index)
            {
                return flipBoardDisplay ? 7 - index : index;
            }
            public int ConverTileOrientation(int orientation, int tileId)
            {
                if (tileId == -1) return 0; // back of a tile should not be reoriented
                Debug.Assert(tileId > 0 && tileId <= 8);
                int correctedOrientation = (orientation + tileOrientationMap[tileId]) % 4;
                return flipBoardDisplay ? (correctedOrientation + 2) % 4 : correctedOrientation;
            }
            public int ConverHerseDirection(int direction)
            {
                return flipBoardDisplay ? (direction + 2) % 4 : direction;
            }
            ////// bga data conversion methods /////


            ////// state machine handling /////
            public void LeaveCurrentState()
            {
                Logger.Instance.Log("INFO", "FORCING Leave game state " + _currentState);
                _stateLess = true;
                _activePlayers.Clear();
                _criticalState = CheckForCriticalState();
            }

            public void LeaveState(string statename, string activePlayer)
            {
                Debug.Assert(!isStateLess, "Cannot leave state " + statename + ": currently stateless");
                Debug.Assert(statename == _currentState, "Trying to leave an invalid state " + statename + ": currently in state " + _currentState);
                Logger.Instance.Log("DETAIL", "Leave game state " + statename);
                _stateLess = true;
                _activePlayers.Clear();
                switch (GetStateType(_currentState))
                {
                    case StateType.ACTIVE:
                    case StateType.GAME:
                        _activePlayers.Add(activePlayer);
                        break;
                    case StateType.MANAGER: // no active player
                    case StateType.MULTIPLE: // active players are given by an update
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
                _criticalState = CheckForCriticalState();
            }

            public void EnterState(string statename, string activePlayer)
            {
                Debug.Assert(isStateLess, "Cannot enter state " + statename + ": currently in state " + _currentState);
                Logger.Instance.Log("DETAIL", "Enter game state " + statename);
                Debug.Assert(HasState(statename), "Trying to enter an invalid state " + statename);
                Debug.Assert(HasTransition(_currentState, statename), "Trying to enter an invalid state " + statename);
                _stateLess = false;
                _currentState = statename;
                _activePlayers.Clear();
                switch (GetStateType(_currentState))
                {
                    case StateType.ACTIVE:
                    case StateType.GAME:
                        _activePlayers.Add(activePlayer);
                        break;
                    case StateType.MANAGER: // no active player
                    case StateType.MULTIPLE: // active players are given by an update
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
                if (!DequeueExpectedState(_currentState))
                    _criticalState = CheckForCriticalState();
            }

            public void UpdateActivePlayers(List<string> activePlayers)
            {
                Debug.Assert(GetStateType(_currentState) == StateType.MULTIPLE);
                _activePlayers.Clear();
                foreach (string activePlayer in activePlayers)
                    _activePlayers.Add(activePlayer);
                _criticalState = (_activePlayers.Count == 2); // beginning of multiple active state.
            }

            public void AddExpectedStates(List<string> newStates) { foreach(var newState in newStates) AddExpectedState(newState); }
            public void AddExpectedState(string newState) { EnqueueExpectedState(newState); }
            public bool WaitingForExpectedState() { return _expectedStates.Count > 0; }

            public bool WaitForGameToCatchUp() { return _criticalState; } // wait for game to prepare the next state
            public void GameIsReady() { if (_criticalState) Debug.LogWarning("board is already ready for action!"); _criticalState = false; } // game must signal the board that it is ready

            public bool TokenPlacementReady() { return (_currentState == "placeTokenFirstPlayer") && isStateLess; }
            public bool RevealCharacters() { return (_currentState == "revealCharacters") && !isStateLess; }
            public bool PlayerTurnStart() { return (_currentState == "playerChooseCard") && !isStateLess; }

            public string GetCurrentExpectedState() { return WaitingForExpectedState() ? _lastExpectedState : _currentState; }
            public string GetCurrentState() { return _currentState; }

            public int movingToken { get { return _movingToken; } }
            public void UnselectToken() { SelectToken(NO_TOKEN); }
            public void SelectToken(int token) { Debug.Assert(token >= 0); _movingToken = token; }
            ////// state machine handling /////


            ////// StateMachine Implementation //////
            private void EnqueueExpectedState(string newState)
            {
                Debug.Assert(HasTransition(GetCurrentExpectedState(), newState), "State " + newState + " can not follow state " + GetCurrentExpectedState());
                _expectedStates.Enqueue(newState);
                _lastExpectedState = newState;
            }
            private bool DequeueExpectedState(string newState)
            {
                if (WaitingForExpectedState())
                {
                    // playerChooseAction is used as generic expected state, but it could be the sequence 
                    // (endTurn, playerChooseCard) or (endTurn, gameEnd) instead
                    // same for playerChooseCard that can be replaced by gameEnd
                    string expected = _expectedStates.Dequeue();
                    if (newState == "endTurn" && expected == "playerChooseAction")
                    {
                        // insert new head to the queue. This is dirty but it works
                        var tmp = _expectedStates;
                        _expectedStates = new Queue<string>();
                        _expectedStates.Enqueue("playerChooseCard");
                        foreach(var state in tmp) _expectedStates.Enqueue(state);

                        Logger.Instance.Log("LOG", "Encountered state " + newState + " instead of " + expected + " => expected state changed to playerChooseCard");
                    }
                    else if (newState == "gameEnd" && expected == "playerChooseCard")
                    {
                        Logger.Instance.Log("LOG", "Encountered state " + newState + " instead of " + expected + " => ok");
                    }
                    else
                    {
                        // TODO: Display a server error and reload game
                        Debug.Assert(expected == newState, "Expected state " + expected + ": received " + newState);
                    }
                    return true;
                }
                return false;
            }

            private bool CheckForCriticalState()
            {
                switch (_currentState)
                {
                    case "gameSetup":
                        break; // automatic state
                    case "characterChoice":
                        break; // must receive update notification
                    case "placeTokenFirstPlayer":
                        return isStateLess;    // First Coin Toss: no need to wait on leave to activate first player
                    case "placeToken":
                        return !isStateLess;    // Active State
                    case "placeTokenNextPlayer":
                        break; // automatic state
                    case "revealCharacters":
                        return !isStateLess;    // REVEAL
                    case "playerChooseCard":
                    case "playerChooseAction":
                        return !isStateLess;    // Active State
                    case "discoverRoomPlaceTokenNextToken":
                        break; // automatic state
                    case "discoverRoomPlaceToken":
                        return !isStateLess;    // REVEAL + Active State
                    case "movingCharacter":
                    case "movingCharacterContinue":
                        return !isStateLess;    // Active State
                    case "combatChooseCard":
                        break; // must receive update notification
                    case "combatResolution":
                        return !isStateLess;    // Animation
                    case "endAction":
                        return !isStateLess;    // Animation
                    case "endTurn":
                        break; // automatic state
                    case "gameEnd":
                        break; // no more notifications
                    default:
                        Debug.Assert(false, "Invalid gamestate '" + _currentState + "'");
                        break;
                }
                return false;
            }

            private bool isStateLess { get { return _stateLess; } }
            private bool HasState(string statename) { Debug.Assert(statename != null); return stateTypes.ContainsKey(statename); }
            private bool HasTransition(string previousState, string nextState) { Debug.Assert(HasState(previousState)); return stateTransitions[previousState].Contains(nextState); }
            private StateType GetStateType(string statename) { Debug.Assert(HasState(statename)); return stateTypes[statename]; }
            ////// StateMachine Implementation //////


            ////// TokenCarriedMap Implementation //////
            public void SwitchCarriedTokenBackup() { var tmp = tokenCarriedBackup; tokenCarriedBackup = tokenCarriedMap; tokenCarriedMap = tmp; } // hack for load
            public void CreateCarriedTokenBackup() { Debug.Assert(tokenCarriedBackup == null); tokenCarriedBackup = new Dictionary<int, int>(tokenCarriedMap); }
            public void RestoreCarriedTokenBackup() { Debug.Assert(tokenCarriedBackup != null); tokenCarriedMap = tokenCarriedBackup; tokenCarriedBackup = null; }
            public void RemoveCarriedTokenBackup() { Debug.Assert(tokenCarriedBackup != null); tokenCarriedBackup = null; }
            public int GetCarriedToken(int character) { Debug.Assert(IsCarrier(character)); return tokenCarriedMap[character]; }
            public bool IsCarrier(int character) { return tokenCarriedMap.ContainsKey(character); }
            public bool IsCarried(int token) { return tokenCarriedMap.ContainsValue(token); }
            public void SetCarriedToken(int character, int token)
            {
                if (character == NO_TOKEN) // item dropped (less effective than character drops for same result)
                {
                    Debug.Assert(token != NO_TOKEN);
                    rawDropToken(token);
                }
                else if (token == NO_TOKEN) // character drops
                {
                    Debug.Assert(character != NO_TOKEN);
                    tokenCarriedMap.Remove(character);
                }
                else // character picks token
                {
                    rawDropToken(token);
                    tokenCarriedMap[character] = token;
                }
            }
            private void rawDropToken(int token)
            {
                foreach (var elt in tokenCarriedMap)
                {
                    if (elt.Value == token)
                    {
                        tokenCarriedMap.Remove(elt.Key);
                        break;
                    }
                }
                Debug.Assert(!tokenCarriedMap.ContainsValue(token));
            }
            ////// TokenCarriedMap Implementation //////


            ////// TokenIdMap Implementation //////
            public void RegisterToken(string playerId, string tokenName, int bgaId) { RegisterToken(GetPlayerIndexFromId(playerId), tokenName, bgaId); }
            public int GetTokenId(int playerIndex, string tokenName)
            {
                Debug.Assert(tokenIdMaps[playerIndex].ContainsKey(tokenName));
                return tokenIdMaps[playerIndex][tokenName];
            }
            public void GetTokenFromId(int bgaId, out int playerIndex, out string tokenName)
            {
                playerIndex = -1;
                tokenName = null;
                bool found = false;
                for (int index = 0; index < 2; ++index)
                {
                    foreach (var elt in tokenIdMaps[index])
                    {
                        if (elt.Value == bgaId)
                        {
                            playerIndex = index;
                            tokenName = elt.Key;
                            found = true;
                            break;
                        }
                    }
                    if (found) break;
                }
                Debug.Assert(found);
            }
            ////// TokenIdMap Implementation //////


            ////// implementation specifics /////
            private void RegisterToken(int playerIndex, string tokenName, int bgaId)
            {
                tokenIdMaps[playerIndex][tokenName] = bgaId;
            }

            private void Register(string name, string bgaName)
            {
                mapGameToBGA[name] = bgaName;
                mapBGAToGame[bgaName] = name;
            }

            private void RegisterState(string statename, StateType type, List<string> transitions)
            {
                stateTransitions[statename] = transitions;
                stateTypes[statename] = type;
            }

            private void Prepare()
            {
                mapGameToBGA = new Dictionary<string, string>();
                mapBGAToGame = new Dictionary<string, string>();
                Register("hidden", "hidden"); // special case for unknown tokens
                // characters
                Register("Mechanork", "mekanork");
                Register("Wall-Walker", "wall-walker");
                Register("Wizard", "wizard");
                Register("Troll", "troll");
                Register("Goblin", "goblin");
                Register("Thief", "thief");
                Register("Cleric", "cleric");
                Register("Warrior", "warrior");
                // items
                Register("Armor", "armor");
                Register("Rope", "rope");
                Register("FireballWand", "fireballwand");
                Register("SpeedPotion", "speedpotion");
                Register("Sword", "sword");
                Register("Treasure", "treasure");
                // tiles
                tileOrientationMap = new Dictionary<int, int>();
                Register("unknown", "-1"); // special case for unknown tiles
                Register("Tile_1D", "1"); tileOrientationMap[1] = 0;
                Register("Tile_1G", "2"); tileOrientationMap[2] = 0;
                Register("Tile_2D", "3"); tileOrientationMap[3] = 0;
                Register("Tile_2G", "4"); tileOrientationMap[4] = 0;
                Register("Tile_3D", "5"); tileOrientationMap[5] = 0;
                Register("Tile_3G", "6"); tileOrientationMap[6] = 0;
                Register("Tile_4D", "7"); tileOrientationMap[7] = 0;
                Register("Tile_4G", "8"); tileOrientationMap[8] = 0;
                // bga state and transitions
                stateTransitions = new Dictionary<string, List<string>>();
                stateTypes = new Dictionary<string, StateType>();
                RegisterState("",                     StateType.MANAGER,      new List<string> { "gameSetup" });
                RegisterState("gameSetup",            StateType.MANAGER,      new List<string> { "characterChoice" });

                RegisterState("characterChoice",      StateType.MULTIPLE,     new List<string> { "placeTokenFirstPlayer" });

                RegisterState("placeTokenFirstPlayer",StateType.GAME,         new List<string> { "placeToken" });
                RegisterState("placeToken",           StateType.ACTIVE,       new List<string> { "placeTokenNextPlayer" });
                RegisterState("placeTokenNextPlayer", StateType.GAME,         new List<string> { "placeToken", "revealCharacters" });
                RegisterState("revealCharacters",     StateType.GAME,         new List<string> { "playerChooseCard" });

                RegisterState("playerChooseCard",     StateType.ACTIVE,       new List<string> { "playerChooseAction" });
                RegisterState("playerChooseAction",   StateType.ACTIVE,       new List<string> { "discoverRoomPlaceTokenNextToken"/*reveal*/, "movingCharacter"/*select*/, "endAction"/*pass*/ });
                
                RegisterState("discoverRoomPlaceTokenNextToken",StateType.GAME,new List<string>{ "discoverRoomPlaceToken"/*nextPlayer*/, "endAction" });
                RegisterState("discoverRoomPlaceToken",StateType.ACTIVE,      new List<string> { "discoverRoomPlaceTokenNextToken" });

                RegisterState("movingCharacter",      StateType.ACTIVE,       new List<string> { "playerChooseAction"/*cancel*/, "movingCharacterContinue"/*move*/, "discoverRoomPlaceTokenNextToken"/*reveal*/, "combatChooseCard"/*attack*/, "endAction"/*confirm*/ });
                RegisterState("movingCharacterContinue",StateType.ACTIVE,     new List<string> { "playerChooseAction"/*cancel*/, "movingCharacterContinue"/*move*/, "endAction"/*confirm*/ });
                
                RegisterState("combatChooseCard",     StateType.MULTIPLE,     new List<string> { "combatResolution" });
                RegisterState("combatResolution",     StateType.GAME,         new List<string> { "endAction" });
                
                RegisterState("endAction",            StateType.GAME,         new List<string> { "playerChooseAction", "endTurn" });
                RegisterState("endTurn",              StateType.GAME,         new List<string> { "playerChooseCard", "gameEnd" });

                RegisterState("gameEnd",              StateType.MANAGER,      new List<string> {  });
                // bga common transitions
            }

            private void ClearOnlineData()
            {
                _bgaFirstPlayerId = null;
                _currentState = "";
                _stateLess = true;
                _criticalState = true;
                _movingToken = NO_TOKEN;
                _expectedStates = new Queue<string>();
                _activePlayers = new List<string>();

                tokenIdMaps = new List<Dictionary<string, int>>();
                tokenIdMaps.Add(new Dictionary<string, int>());
                tokenIdMaps.Add(new Dictionary<string, int>());

                tokenCarriedMap = new Dictionary<int, int>();
            }
            ////// implementation specifics /////
        }
    }
}
