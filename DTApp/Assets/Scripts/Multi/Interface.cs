using UnityEngine;
using System.Collections.Generic;

using Actions;

namespace Multi
{
    /* 
     * class handling the interface between the gamemanager and the network handling.
     */
    public class Interface
    {
        private GameManager gManager;
        private BGA.GameData gameData;
        private ActionPlayer actionPlayer;
        private GameState gamestate;

        private ActionType delayedActionType;
        private CharacterBehavior delayedActionCharacter;
        private CaseBehavior delayedActionTarget;

        public Interface()
        {
            Clear();
        }

        public void Clear()
        {
            gameData = new BGA.GameData();
            ClearDelayedAction();
        }

        public void SetOnlineGame(string viewerId, BGA.TableData table, JSONObject bgaData)
        {
            Logger.Instance.Log("LOG", "NWInterface: SetOnlineGame()");
            try
            {
                Clear();

                BGA.BoardData board = new BGA.BoardData(viewerId, table);

                string bgaDataDebugString = JSONTools.FormatJsonDisplay(bgaData);
                foreach (string chunk in splitInLineChunks(bgaDataDebugString, 500))
                    Logger.Instance.Log("DETAIL", chunk);

                GameStateReader gameStateReader = new GameStateReader(bgaData, board);
                if (!gameStateReader.IsValid)
                    Logger.Instance.Log("ERROR", "invalid gamestate received from server");
                gamestate = gameStateReader.convertToGameState();

                gameData.SetOnlineGame(board, gameStateReader.initMoveId, gameStateReader.initPacketId);
            }
            catch(System.Exception e)
            {
                Logger.Instance.Log("ERROR", e.ToString());
                BGA.BoardData board = new BGA.BoardData(viewerId, table);
                gamestate = null;
                gameData.SetOnlineGame(board, 0, 0);
            }
        }

        public void Start(GameManager gManager_)
        {
            Logger.Instance.Log("LOG", "NWInterface: Start()");
            gManager = gManager_;
            actionPlayer = new ActionPlayer(gManager_);
        }

        public void LoadGame()
        {
            Logger.Instance.Log("LOG", "NWInterface: LoadGame()");
            if (gamestate != null) gamestate.loadGameState();
            gameData.Start();
            EnableAction();
        }

        public void Update()
        {
            if (isOnline)
            {
                gameData.Update();

                if (!ActionEnabled() && !gManager.progression.IsGameProcessing())
                {
                    EnableAction();
                }

                if (ActionEnabled())
                {
                    if (gameData.HasWaitingNotification())
                    {
                        BGA.NotificationData notif = gameData.ProcessWaitingNotification();
                        Action action = ExtractAction(notif);
                        actionPlayer.ExecuteAction(action);
                    }
                }
            }
        }

        //---- Common Helpers ----//
        public bool isOnline { get { return gameData.isOnline; } }

        public string playerName(int index) { return gameData.GetPlayerName(index); }
        public string playerId(int index) { return gameData.GetPlayerId(index); }
        public int playerIndex(string id) { return gameData.GetPlayerIndex(id); }
        public string viewerId { get { return gameData.viewerId; } }
        public bool isOnlineOpponent(int index) { return isOnline && (playerId(index) != viewerId); }
        public bool doRecordInteractions(int playerIndex) { return isOnline && !isOnlineOpponent(playerIndex); }
        public bool doReplayInteractions(int playerIndex) { return isOnlineOpponent(playerIndex); }

        public bool blockUserInteractions() { return !gameData.isBoardReady || gManager.progression.isBlockingInteractions(); }
        //---- Common Helpers ----//


        //---- Player Order ----//
        public int firstPlayerToPlace1 { get { Debug.Assert(isOnline); return 0; } }
        public int firstPlayerToPlace2 { get { Debug.Assert(isOnline); return gameData.board.GetPlayerIndexFromId(gameData.board.firstPlayerToPlaceId); } }
        public int firstPlayerToPlay { get { Debug.Assert(isOnline); return gameData.board.GetPlayerIndexFromId(gameData.board.firstPlayerToPlayId); } }

        public void ForfeitGame() { if (isOnline) gameData.ForfeitGame(); }
        //---- Player Order ----//


        //---- Recording ----//
        public void RecordAction(ActionType type, CharacterBehavior character) { RecordAction(type, character, character); }
        public void RecordAction(ActionType type, CharacterBehavior character, CharacterBehavior target)
        {
            if (!doRecordInteractions(activePlayerIndex)) { Logger.Instance.Log("LOG", "Skip RecordAction " + type); return; }

            Debug.Assert(gManager.progression.IsCharacterSelected());
            Debug.Assert(character == gManager.progression.GetSelectedCharacter());
            SynchronizeSelect();

            gameData.RecordAction(type, activePlayerIndex,
                character.getOwnerIndex(), character.getTokenName(),
                target.getOwnerIndex(), target.getTokenName());
        }   

        // must be called at the BEGINING of each move
        public void RecordMove(ActionType type, CharacterBehavior character, CaseBehavior target)
        {
            if (!doRecordInteractions(activePlayerIndex)) { Logger.Instance.Log("LOG", "Skip RecordMove " + type); return; }

            Debug.Assert(gManager.progression.IsCharacterSelected());
            Debug.Assert(character == gManager.progression.GetSelectedCharacter());
            SynchronizeMove();

            if (type == ActionType.JUMP)
                StoreDelayedAction(type, character, target);
            else
                DoRecordMove(type, character, target, false);
        }
        private void DoRecordMove(ActionType type, CharacterBehavior character, CaseBehavior target, bool endAction)
        {
            CaseBehavior dest = target.GetComponent<CaseBehavior>();
            gameData.RecordMove(type, activePlayerIndex, dest.column, dest.row, endAction);
        }

        // must be called when a move is canceled
        public void RecordCancelMove()
        {
            if (!doRecordInteractions(activePlayerIndex)) { Logger.Instance.Log("LOG", "Skip RecordCancelMove"); return; }

            if (HasDelayedAction())
            {
                Logger.Instance.Log("WARNING", "Skipped record of delayed move");
                ClearDelayedAction();
            }

            if (!isSelected) { Logger.Instance.Log("WARNING", "Skipped cancel of non initialized move"); return; }

            gameData.RecordCancelMove(activePlayerIndex);
        }

        // must be called when a move is confirmed
        public void RecordEndMove()
        {
            if (!doRecordInteractions(activePlayerIndex)) { Logger.Instance.Log("LOG", "Skip RecordEndMove"); return; }

            if (HasDelayedAction())
            {
                RecordDelayedAction();
            }
            else
            {
                if (!isSelected) { Logger.Instance.Log("WARNING", "Skipped ending of non initialized move"); return; }

                SynchronizeMove();

                gameData.RecordEndMove(activePlayerIndex);
            }
        }

        public void RecordPass()
        {
            if (!doRecordInteractions(activePlayerIndex)) { Logger.Instance.Log("LOG", "Skip RecordPass"); return; }

            gameData.RecordPass(activePlayerIndex);
        }

        public void RecordCard(ActionType type, int value)
        {
            if (!doRecordInteractions(activePlayerIndex)) { Logger.Instance.Log("LOG", "Skip RecordCard " + type); return; }

            gameData.RecordCard(type, activePlayerIndex, value);
        }

        public void RecordPlacementCharacterChoice(Token token, PlacementTokens target)
        {
            if (!doRecordInteractions(activePlayerIndex)) { Logger.Instance.Log("LOG", "Skip RecordPlacementCharacterChoice"); return; }

            gameData.RecordPlacementCharacterChoice(activePlayerIndex, token.getOwnerIndex(), token.getTokenName(),
                target.caseActuelle.column, target.caseActuelle.row);
        }

        public void RecordPlacement(Token token, PlacementTokens target)
        {
            if (!doRecordInteractions(activePlayerIndex)) { Logger.Instance.Log("LOG", "Skip RecordPlacement"); return; }

            gameData.RecordPlacement(activePlayerIndex, token.getOwnerIndex(), token.getTokenName(),
                target.transform.parent.GetComponent<HiddenTileBehavior>().tileAssociated.GetComponent<TileBehavior>().index);
        }

        public void RecordPlacementOnRoomDiscovered(Token token, PlacementTokens target)
        {
            if (!doRecordInteractions(activePlayerIndex)) { Logger.Instance.Log("LOG", "Skip RecordPlacementOnRoomDiscovered"); return; }

            gameData.RecordPlacementOnRoomDiscovered(activePlayerIndex, token.getOwnerIndex(), token.getTokenName(),
                target.caseActuelle.column, target.caseActuelle.row);
        }

        public void RecordReveal(TileBehavior tile)
        {
            if (!doRecordInteractions(activePlayerIndex)) { Logger.Instance.Log("LOG", "Skip RecordReveal"); return; }

            SynchronizeSelect();

            gameData.RecordReveal(activePlayerIndex, tile.index);
        }

        public void RecordRotation(TileBehavior tile, int numberOfTurns, bool inversed)
        {
            if (!doRecordInteractions(activePlayerIndex)) { Logger.Instance.Log("LOG", "Skip RecordRotation"); return; }

            SynchronizeSelect();

            bool clockwize = (tile.clockwiseRotation != inversed);
            gameData.RecordRotation(activePlayerIndex, tile.index, numberOfTurns, clockwize);
        }
        //---- Recording ----//


        //---- Play Notifications ----//
        private bool canOnlinePlayerReplayAction(string id)
        {
            if (gameData.IsProcessingCurrentMove()) // if the online player move starts replaying, all the corresponding notifications must be played
            {
                return true;
            }
            else
            {
                int index = gameData.GetPlayerIndex(id);
                Debug.Assert(index != -1);
                return gManager.progression.IsPlayerAllowedToPlay(index);
            }
        }

        public void EnableAction(bool enable = true) { gameData.EnableAction(enable); }
        public bool ActionEnabled() { return !gameData.WaitForGameToCatchUp(); }

        public void ForceEndReplayAction() { gameData.ResolveCurrentNotification(); }

        public void EndReplayAction()
        {
            if (!doReplayInteractions(activePlayerIndex)) { Logger.Instance.Log("LOG", "Skip EndReplay"); return; }

            gameData.ResolveCurrentNotification();
        }

        public void RegisterToken(string playerId, string tokenName, int bgaId) { gameData.RegisterToken(playerId, tokenName, bgaId); }
        public void SetCarriedToken(int character, int token) { gameData.SetCarriedToken(character, token); }
        public void SetDropped(int token) { gameData.SetDropped(token); }
        public int GetCarriedToken(int character) { return gameData.GetCarriedToken(character); }
        public bool IsCarrier(int character) { return gameData.IsCarrier(character); }
        public void GetTokenFromId(int bgaId, out int ownerIndex, out string tokenName) { gameData.board.GetTokenFromId(bgaId, out ownerIndex, out tokenName); tokenName = gameData.board.ConvertToGame(tokenName); }

        public void LeaveCurrentState() { gameData.LeaveCurrentState(); }
        public void LeaveState(string statename, string activePlayer) { gameData.LeaveState(statename, activePlayer); }
        public void EnterState(string statename, string activePlayer) { gameData.EnterState(statename, activePlayer); }
        public void UpdateActivePlayers(List<string> activePlayers) { gameData.UpdateActivePlayers(activePlayers); }

        public string GetCurrentOnlineState() { return gameData.GetCurrentState(); }
        public bool WaitingForExpectedState() { return gameData.WaitingForExpectedState(); }
        //---- Play Notifications ----//


        //---- Implementation Specifics ----//
        private PlayerBehavior activePlayer { get { return gManager.activePlayer; } }
        private int activePlayerIndex { get { return activePlayer.index; } }
        private string activePlayerId { get { return activePlayer.onlineID; } }

        private bool isSelected { get { return gameData.isSelected; } }
        private CharacterBehavior movingCharacter {
            get
            {
                Debug.Assert(isSelected);
                int ownerIndex;
                string tokenName;
                GetTokenFromId(gameData.movingCharacterId, out ownerIndex, out tokenName);
                Token token = gManager.GetTokenByNameAndOwnerIndex(tokenName, ownerIndex);
                Debug.Assert(token is CharacterBehavior, "Token " + tokenName + " is not a character");
                return token.GetComponent<CharacterBehavior>();
            }
        }

        private void ClearDelayedAction()
        {
            delayedActionType = ActionType.WALK;
            delayedActionCharacter = null;
            delayedActionTarget = null;
        }
        private void StoreDelayedAction(ActionType delayedActionType, CharacterBehavior delayedActionCharacter, CaseBehavior delayedActionTarget)
        {
            Logger.Instance.Log("WARNING", "Delayed Record of Jump");
            this.delayedActionType = delayedActionType;
            this.delayedActionCharacter = delayedActionCharacter;
            this.delayedActionTarget = delayedActionTarget;
        }
        private bool HasDelayedAction()
        {
            return delayedActionCharacter != null;
        }
        private void RecordDelayedAction()
        {
            Debug.Assert(HasDelayedAction());
            DoRecordMove(delayedActionType, delayedActionCharacter, delayedActionTarget, true);
            ClearDelayedAction();
        }

        private void SynchronizeUnselect() { Synchronize(false, false); }
        private void SynchronizeSelect() { Synchronize(true, false); }
        private void SynchronizeMove() { Synchronize(true, true); }

        private void Synchronize(bool select, bool move)
        {
            // TODO: check if bga state differs from app state for selection matter (maybe unnecessary if load is properly done)
            if (select)
            {
                Debug.Assert(gManager.progression.IsCharacterSelected());
                CharacterBehavior character = gManager.progression.GetSelectedCharacter();

                if (isSelected && movingCharacter != character)
                {
                    // deselect previously selected character to select a new
                    gameData.RecordCancelMove(activePlayerIndex);
                }

                if (!isSelected)
                {
                    // select character
                    gameData.RecordStartMove(activePlayerIndex, character.getOwnerIndex(), character.getTokenName());
                }

                if (move)
                {
                    // synchronize selected character carried token
                    SynchronizeCarriedToken();
                }
            }
            else
            {
                Debug.Assert(!move);
                if (isSelected)
                {
                    // deselect previously selected character
                    gameData.RecordCancelMove(activePlayerIndex);
                }
            }
        }

        private void SynchronizeCarriedToken()
        {
            Debug.Assert(isSelected);
            Token tokenPicked = (movingCharacter.tokenTranporte != null) ? movingCharacter.tokenTranporte.GetComponent<Token>() : null;
            string tokenPickType = (tokenPicked != null) ? tokenPicked.getTokenName() : null;
            int tokenPickOwner = (tokenPicked != null) ? tokenPicked.getOwnerIndex() : 0;
            gameData.RecordCarriedToken(tokenPickOwner, tokenPickType);
        }

        private string GetPlayerIdFromName(string playerName)
        {
            int playerIndex = gameData.board.GetPlayerIndexFromName(playerName);
            Debug.Assert(playerIndex != -1);
            string playerId = gameData.board.GetPlayerId(playerIndex);
            return playerId;
        }

        private Action ExtractAction(BGA.NotificationData notif)
        {
            try
            {
                switch (notif.type)
                {
                    case BGA.NotificationData.Type.SIMPLE_NOTE:
                        {
                            return new ActionSimpleNote(notif.GetSimpleNoteMessage());
                        }
                    case BGA.NotificationData.Type.ACTIONCARD_PLAYED:
                        {
                            string playerId = notif.GetPlayerId(0);
                            int value = notif.GetActionCardValue();
                            return new ActionCard(ActionType.ACTION_CARD, playerId, value);
                        }
                    case BGA.NotificationData.Type.CHOOSE_COMBATCARD:
                        {
                            string playerId = GetPlayerIdFromName( notif.GetPlayerName() );
                            return new ActionCard(ActionType.COMBAT_CARD, playerId);
                        }
                    case BGA.NotificationData.Type.CHOOSE_MYCOMBATCARD:
                        {
                            string playerId = gameData.board.viewerId;
                            int value = notif.GetCombatCardValue();
                            return new ActionCard(ActionType.COMBAT_CARD, playerId, value);
                        }
                    case BGA.NotificationData.Type.REVEAL_COMBATCARD:
                        {
                            string playerId = notif.GetPlayerId(0);
                            int value = notif.GetCombatCardValue();
                            int total = notif.GetCombatTotalValue();
                            return new ActionCard(ActionType.COMBAT_CARD, playerId, value, total);
                        }
                    case BGA.NotificationData.Type.JUMP:
                        {
                            string playerId = notif.GetPlayerId(0);
                            return new ActionCard(ActionType.JUMP, playerId);
                        }
                    case BGA.NotificationData.Type.CHARACTER_GET_OUT: // getout and score
                        {
                            string playerId = notif.GetCharacterOwner();
                            string tokenName = gameData.board.ConvertToGame(notif.GetCharacterName());
                            int score = notif.GetCharacterPoints();
                            int charPoints = notif.GetCharacterPoints();
                            List<int> removeTokenIds = notif.GetRemoveTokenIds();
                            return new ActionRemove(playerId, tokenName, score, charPoints, removeTokenIds);
                        }
                    case BGA.NotificationData.Type.REMOVE_TOKENS: // speed potion, fireballwand
                        {
                            if (notif.GetPlayerName() != null) // speed potion
                            {
                                string playerId = GetPlayerIdFromName(notif.GetPlayerName());
                                string tokenName = gameData.board.ConvertToGame(notif.GetCharacterName());
                                int score = 0;
                                int charPoints = 0;
                                List<int> removeTokenIds = notif.GetRemoveTokenIds();
                                return new ActionRemove(playerId, tokenName, score, charPoints, removeTokenIds);
                            }
                            else // fireballwand
                            {
                                Logger.Instance.Log("WARNING", "Network Interface: Extraction of Unused notification type " + notif.typeAsString + " (for fireballwand) skipped");
                                return new Action();
                            }
                        }
                    case BGA.NotificationData.Type.CHARACTER_HEALED: // troll regeneration or clerc healing
                        {
                            string playerId = GetPlayerIdFromName(notif.GetPlayerName());
                            string tokenName = gameData.board.ConvertToGame(notif.GetHealerType()); // Troll or Clerc
                            Debug.Assert(notif.GetHealerOwnerId() == playerId);
                            int targetId = notif.GetHealerTargetId();
                            return new ActionHeal(playerId, tokenName, targetId);
                        }
                    case BGA.NotificationData.Type.CHARACTER_MOVED:
                        {
                            string playerId = notif.GetPlayerId(0);
                            int tokenId = notif.GetArgId(0);
                            string tokenName = gameData.board.ConvertToGame(notif.GetArgType(0));
                            int x = gameData.board.ConvertX(notif.GetArgX(0));
                            int y = gameData.board.ConvertY(notif.GetArgY(0));
                            return new ActionMove(playerId, tokenId, tokenName, x, y);
                        }
                    case BGA.NotificationData.Type.COMBATCARD_REVEALED: // direct kill by firewand, killed because carrier killed, fall on trap, combat draw/win/loss
                        {
                            string playerId = GetPlayerIdFromName(notif.GetPlayerName());
                            string tokenName = "unknown"; // gameData.board.ConvertToGame(notif.GetCharacterName()); // this information is not given anymore
                            int attackerScore = notif.GetAttackerScore();
                            int defenderScore = notif.GetDefenderScore();
                            string winnerId = notif.GetWinnerId();
                            List<int> woundedIds = notif.GetWoundedIds();
                            List<int> killedIds = notif.GetKilledIds();
                            int attackerCardValue = notif.GetAttackerCardValue(); // -1 if not a combat result
                            int defenderCardValue = notif.GetDefenderCardValue(); // -1 if not a combat result
                            return new ActionCombatEnd(playerId, tokenName, attackerScore, defenderScore, winnerId, woundedIds, killedIds, attackerCardValue, defenderCardValue);
                        }
                    case BGA.NotificationData.Type.COMBAT_START:
                        {
                            string playerId = GetPlayerIdFromName( notif.GetPlayerName() );
                            List<string> attackers = notif.GetAttackerNames();
                            List<string> defenders = notif.GetDefenderNames();
                            return new ActionCombatStart(playerId, attackers, defenders);
                        }
                    case BGA.NotificationData.Type.GAMESTATE_CHANGE:
                    case BGA.NotificationData.Type.LEAVE_GAMESTATE:
                        {
                            bool leave = (notif.type == BGA.NotificationData.Type.LEAVE_GAMESTATE);
                            string gameStateName = notif.GetGameStateName();
                            string activePlayer = notif.GetNewActivePlayer();
                            return new ActionStateChange(gameStateName, activePlayer, leave);
                        }
                    case BGA.NotificationData.Type.HERSE_MANIPULATED:
                        {
                            string newState = notif.GetHerseManipulatedNewState();
                            ActionType type = (newState == "broken") ? ActionType.DESTROYDOOR : ((newState == "open") ? ActionType.OPENDOOR : ActionType.CLOSEDOOR);
                            int x1 = gameData.board.ConvertX(notif.GetHerseManipulatedCharacterLocationX());
                            int y1 = gameData.board.ConvertY(notif.GetHerseManipulatedCharacterLocationY());
                            int x2 = gameData.board.ConvertX(notif.GetHerseManipulatedDirectionX());
                            int y2 = gameData.board.ConvertY(notif.GetHerseManipulatedDirectionY());
                            return new ActionDoor(type, x1, y1, x2, y2);
                        }
                    case BGA.NotificationData.Type.ROOM_DISCOVERED:
                        {
                            string playerId = notif.GetPlayerId(0);
                            //int tokenId = notif.GetArgId(0);
                            int tileIndex = gameData.board.ConvertTile( notif.GetTileId() );
                            Debug.Assert(tileIndex >= 0 && tileIndex < 8);
                            string tileName = gameData.board.ConvertToGame( notif.GetTileType().ToString() );
                            int orientation = gameData.board.ConverTileOrientation( notif.GetTileOrientation(), notif.GetTileType());
                            return new ActionRoomDiscovered(playerId/*, tokenId*/, tileIndex, tileName, orientation);
                        }
                    case BGA.NotificationData.Type.ROOM_ROTATED:
                        {
                            string playerId = GetPlayerIdFromName(notif.GetPlayerName());
                            string tokenName = gameData.board.ConvertToGame(notif.GetCharacterName());
                            int tileIndex = gameData.board.ConvertTile(notif.GetTileId());
                            Debug.Assert(tileIndex >= 0 && tileIndex < 8);
                            bool clockwize = notif.GetTileRotationClockwize();
                            int nbAction = notif.GetTileRotationNbAction();
                            int direction = gameData.board.ConverTileOrientation(notif.GetTileRotationDirection(), notif.GetTileType());
                            return new ActionRoomRotated(playerId, tokenName, tileIndex, clockwize, nbAction, direction);
                        }
                    case BGA.NotificationData.Type.TOKENS_TO_PLACE_UPDATE:
                    case BGA.NotificationData.Type.TOKENS_UPDATE:
                        {
                            ActionTokensUpdate.Destination destination;
                            switch (GetCurrentOnlineState())
                            {
                                case "characterChoice":
                                    if (notif.type == BGA.NotificationData.Type.TOKENS_TO_PLACE_UPDATE) // unnecessary update
                                    {
                                        Logger.Instance.Log("DETAIL", "Network Interface: Extraction of Unused notification type " + notif.typeAsString + " skipped");
                                        return new Action();
                                    }
                                    else
                                    {
                                        Debug.Assert(notif.GetTokensUpdateLocation() == "initial");
                                        destination = ActionTokensUpdate.Destination.STARTING_LINE;
                                    }
                                    break;
                                case "placeToken":
                                    Debug.Assert(notif.type == BGA.NotificationData.Type.TOKENS_UPDATE);
                                    Debug.Assert(notif.GetTokensUpdateLocation() == "ontile");
                                    destination = ActionTokensUpdate.Destination.ROOM;
                                    break;
                                case "revealCharacters":
                                    Debug.Assert(notif.type == BGA.NotificationData.Type.TOKENS_UPDATE);
                                    Debug.Assert(notif.GetTokensUpdateLocation() == "ingame");
                                    destination = ActionTokensUpdate.Destination.REVEAL;
                                    break;
                                case "playerChooseAction":
                                case "movingCharacter":
                                    Debug.Assert(notif.type == BGA.NotificationData.Type.TOKENS_TO_PLACE_UPDATE);
                                    Debug.Assert(notif.GetTokensUpdateLocation() == "to_place");
                                    destination = ActionTokensUpdate.Destination.TO_PLACE;
                                    break;
                                default:
                                    if (notif.type == BGA.NotificationData.Type.TOKENS_TO_PLACE_UPDATE)
                                    {
                                        Logger.Instance.Log("DETAIL", "Network Interface: Extraction of Unused notification type " + notif.typeAsString + " skipped");
                                        return new Action();
                                    }
                                    else
                                    {
                                        destination = ActionTokensUpdate.Destination.BOARD;
                                    }
                                    break;
                            }

                            var action = new ActionTokensUpdate(destination);
                            for (int i = 0; i < notif.GetArgsCount(); i++)
                            {
                                int tokenId = notif.GetArgId(i);
                                string playerId = notif.GetPlayerId(i);
                                string tokenName = gameData.board.ConvertToGame(notif.GetArgType(i));
                                bool isItem = notif.IsItemToken(i);

                                string location = notif.GetArgLocation(i);
                                switch (location)
                                {
                                    case "ontile":
                                        int tileIndex = gameData.board.ConvertTile(notif.GetArgLocationArg(i));
                                        Debug.Assert(tileIndex >= 0 && tileIndex < 8);
                                        action.tokens.Add(new TokenUpdateData(tokenId, playerId, tokenName, isItem, tileIndex));
                                        break;
                                    case "initial":
                                    case "ingame":
                                    case "carried":
                                    case "removed":
                                        int x = gameData.board.ConvertX(notif.GetArgX(i));
                                        int y = gameData.board.ConvertY(notif.GetArgY(i));
                                        Debug.Assert(x >= 0 && x < gManager.longueurPlateau() && y >= 0 && y < gManager.hauteurPlateau());
                                        int holdingCharacterId = notif.GetTokenHolderId(i);
                                        action.tokens.Add(new TokenUpdateData(tokenId, playerId, tokenName, isItem, x, y, holdingCharacterId));
                                        break;
                                    case "to_place":
                                        action.tokens.Add(new TokenUpdateData(tokenId, playerId, tokenName, isItem, 0));
                                        break;
                                    case "not_placed":
                                    default:
                                        Debug.Assert(false, "not implemented");
                                        break;
                                }
                            }
                            return action;
                        }
                    case BGA.NotificationData.Type.PASS:
                        {
                            return new ActionPass();
                        }
                    case BGA.NotificationData.Type.GAMESTATE_MULTIPLEACTIVEUPDATE:
                        {
                            List<string> players = notif.GetActivePlayersList();
                            return new ActionMultiPlayerState(players);
                        }
                    case BGA.NotificationData.Type.UPDATE_REFLEXIONTIME:
                        {
                            string playerId = notif.GetPlayerId();
                            int delta = notif.GetTimerDelta();
                            int max = notif.GetTimerMax();
                            return new ActionUpdateTimer(playerId, delta, max);
                        }
                    case BGA.NotificationData.Type.NEWACTIONCARDS:
                    case BGA.NotificationData.Type.UPDATE_MECHANISMLOCATIONS:
                    case BGA.NotificationData.Type.TOKENS_TO_PLACE_LIST:
                        Logger.Instance.Log("DETAIL", "Network Interface: Extraction of Unused notification type " + notif.typeAsString + " skipped");
                        return new Action();
                    case BGA.NotificationData.Type.DUMMY:
                        Logger.Instance.Log("WARNING", "Network Interface: Extraction of Unexpected notification type " + notif.typeAsString + " skipped");
                        return new Action();
                    default:
                        Logger.Instance.Log("WARNING", "Network Interface: Extraction of Unknown notification type " + notif.typeAsString + " skipped");
                        return new Action();
                }
            }
            catch (System.Exception e)
            {
                Logger.Instance.Log("ERROR", "Network Interface: Extraction of action from notification " + notif.typeAsString + " failed " + e.ToString());
                return new Action();
            }
        }

        private List<string> splitInLineChunks(string s, int chunk)
        {
            string[] bgaDataDebugLines = s.Split('\n');
            int n = bgaDataDebugLines.Length;
            List<string> result = new List<string>();
            string message = "";
            for (int i = 0; i < n; ++i)
            {
                message += bgaDataDebugLines[i];
                if ((i % chunk == chunk - 1) || i == n - 1)
                {
                    result.Add(message);
                    message = "";
                }
                else
                {
                    message += '\n';
                }
            }
            return result;
        }
        //---- Implementation Specifics ----//
    }
} // Multi
