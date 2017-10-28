using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Actions
{
    public class ActionPlayer
    {
        GameManager gManager;
        HiddenTileBehaviorIHM tileToDiscover = null;
        bool fireballTargetsDealtWith = false;

        public ActionPlayer(GameManager gManager_)
        {
            gManager = gManager_;
        }

        public void ExecuteAction(Action action)
        {
            switch (action.type)
            {
                case Action.Type.CARD:
                    ExecuteActionCard((ActionCard)action);
                    break;
                case Action.Type.STATECHANGE:
                    ExecuteActionStateChange((ActionStateChange)action);
                    break;
                case Action.Type.TOKENSUPDATE:
                    ExecuteActionTokensUpdate((ActionTokensUpdate)action);
                    break;
                case Action.Type.MULTIPLAYERSTATE:
                    ExecuteActionMultiPlayerState((ActionMultiPlayerState)action);
                    break;
                case Action.Type.SIMPLENOTE:
                    ExecuteActionSimpleNote((ActionSimpleNote)action);
                    break;
                case Action.Type.ROOMDISCOVERED:
                    ExecuteActionRoomDiscovered((ActionRoomDiscovered)action);
                    break;
                case Action.Type.ROOMROTATED:
                    ExecuteActionRoomRotated((ActionRoomRotated)action);
                    break;
                case Action.Type.MOVE:
                    ExecuteActionMove((ActionMove)action);
                    break;
                case Action.Type.UPDATETIMER:
                    ExecuteActionUpdateTimer((ActionUpdateTimer)action);
                    break;
                case Action.Type.COMBAT_START:
                    ExecuteActionCombatStart((ActionCombatStart)action);
                    break;
                case Action.Type.COMBAT_END:
                    ExecuteActionCombatEnd((ActionCombatEnd)action);
                    break;
                case Action.Type.DOOR:
                    ExecuteActionDoor((ActionDoor)action);
                    break;
                case Action.Type.HEAL:
                    ExecuteActionHeal((ActionHeal)action);
                    break;
                case Action.Type.REMOVE:
                    ExecuteActionRemove((ActionRemove)action);
                    break;
                case Action.Type.PASS:
                    ExecuteActionPass((ActionPass)action);
                    break;
                case Action.Type.UNKNOWN:
                    Multi.Logger.Instance.Log("LOG", "Skip unknown action");
                    gManager.onlineGameInterface.ForceEndReplayAction();
                    break;
            }
        }

        private void ExecuteActionPass(ActionPass action)
        {
            if (gManager.onlineGameInterface.WaitingForExpectedState()) { gManager.onlineGameInterface.ForceEndReplayAction(); return; }

            gManager.skipTurn();
            gManager.onlineGameInterface.ForceEndReplayAction();
        }

        private void ExecuteActionCombatStart(ActionCombatStart action)
        {
            if (gManager.onlineGameInterface.WaitingForExpectedState()) { gManager.onlineGameInterface.ForceEndReplayAction(); return; }

            Debug.Assert(action.attackers.Count > 0);
            Debug.Assert(action.defenders.Count > 0);
            CharacterBehaviorIHM character = GetTokenByNameAndPlayerId(action.attackers[0], action.playerId).GetComponent<CharacterBehaviorIHM>();
            string opponentId = gManager.onlineGameInterface.playerId(1 - gManager.onlineGameInterface.playerIndex(action.playerId));
            CharacterBehaviorIHM target = GetTokenByNameAndPlayerId(action.defenders[0], opponentId).GetComponent<CharacterBehaviorIHM>();
            character.characterSelection();
            gManager.combatManager.combat(target.gameObject);
            gManager.onlineGameInterface.ForceEndReplayAction();
        }

        private void ExecuteActionCombatEnd(ActionCombatEnd action)
        {
            if (gManager.onlineGameInterface.WaitingForExpectedState()) { gManager.onlineGameInterface.ForceEndReplayAction(); return; }

            if (gManager.onlineGameInterface.GetCurrentOnlineState() == "movingCharacter") // it must be a kill by fireballwand
            {
                if (!fireballTargetsDealtWith) // direct kill notification -> execute the fireball
                {
                    CharacterBehavior wizard = GetTokenByNameAndPlayerId("Wizard", action.winnerId).GetComponent<CharacterBehavior>();

                    Debug.Assert(wizard.tokenTranporte != null);
                    Item_BatonDeBouleDeFeu_IHM fireballwand = wizard.tokenTranporte.GetComponent<Item_BatonDeBouleDeFeu_IHM>();
                    Debug.Assert(fireballwand != null);

                    Debug.Assert(action.killedIds.Count == 1 && action.woundedIds.Count == 0);
                    CharacterBehavior target = GetTokenById(action.killedIds[0]).GetComponent<CharacterBehavior>();

                    wizard.GetComponent<CharacterBehaviorIHM>().characterSelection();
                    fireballwand.readyFireballGUI();
                    fireballwand.fireball(target);
                    fireballTargetsDealtWith = true;
                }
                else // secondary kill notification
                {
                    Multi.Logger.Instance.Log("LOG", "Action Skipped: some wounded token dropped has died");
                    gManager.onlineGameInterface.ForceEndReplayAction();
                }
            }
            else // combat result
            {
                Multi.Logger.Instance.Log("LOG", "Action Skipped: combat result");
                gManager.onlineGameInterface.ForceEndReplayAction();
            }
        }

        private void ExecuteActionDoor(ActionDoor action)
        {
            if (gManager.onlineGameInterface.WaitingForExpectedState()) { gManager.onlineGameInterface.ForceEndReplayAction(); return; }

            CaseBehavior actionCharacterCell = gManager.getCase(action.y1, action.x1).GetComponent<CaseBehavior>();

            CharacterBehavior c = actionCharacterCell.getNonWoundedCharacter();
            Debug.Assert(c != null);
            CharacterBehaviorIHM character = c.GetComponent<CharacterBehaviorIHM>();

            character.characterSelection();
            if (action.action == ActionType.DESTROYDOOR)
            {
                Debug.Assert(character is CB_GuerrierIHM);
                character.GetComponent<CB_GuerrierIHM>().briserHerse();
            }
            else
            {
                Debug.Assert(character is CB_VoleuseIHM);
                character.GetComponent<CB_VoleuseIHM>().changerEtatHerse();
            }
            gManager.onlineGameInterface.ForceEndReplayAction();
        }

        private void ExecuteActionHeal(ActionHeal action)
        {
            if (gManager.onlineGameInterface.WaitingForExpectedState()) { gManager.onlineGameInterface.ForceEndReplayAction(); return; }

            CharacterBehaviorIHM character = GetTokenByNameAndPlayerId(action.tokenName, action.playerId).GetComponent<CharacterBehaviorIHM>();
            CharacterBehaviorIHM target = GetTokenById(action.targetId).GetComponent<CharacterBehaviorIHM>();
            if (action.tokenName == "Troll")
            {
                Debug.Assert(character == target);
                Debug.Assert(character is CB_TrollIHM);
                character.characterSelection();
                character.GetComponent<CB_TrollIHM>().regenerate();
                gManager.onlineGameInterface.ForceEndReplayAction();
            }
            else if (action.tokenName == "Cleric")
            {
                Debug.Assert(character == target);
                Debug.Assert(character is CB_ClercIHM);
                character.characterSelection();
                character.GetComponent<CB_ClercIHM>().healCharacter();
                // TODO: the target seems not to be actually applied
                // EndReplay is done by the clerc
            }
            else
            {
                Debug.Assert(false, "Unexpected character type");
                gManager.onlineGameInterface.ForceEndReplayAction();
            }
        }
        
        private void ExecuteActionRemove(ActionRemove action)
        {
            if (gManager.onlineGameInterface.WaitingForExpectedState()) { gManager.onlineGameInterface.ForceEndReplayAction(); return; }

            if (action.score > 0) // get out
            {
                Debug.Assert(false, "Not implemented yet");
                //GameObject character = ...
                //moveCharacter(character.GetComponent<CharacterBehavior>());
                //endCharacterMovement(character.GetComponent<TokenIHM>());
                gManager.onlineGameInterface.ForceEndReplayAction();
            }
            else // use potion, fireballwand
            {
                Debug.Assert(action.removeTokenIds.Count == 1); // 1 item is consummed
                Token token = GetTokenById(action.removeTokenIds[0]);
                if (token is Item_PotionDeVitesse)
                {
                    CharacterBehaviorIHM character = GetTokenByNameAndPlayerId(action.tokenName, action.playerId).GetComponent<CharacterBehaviorIHM>();
                    character.characterSelection();
                    token.GetComponent<Item_PotionDeVitesse_IHM>().speedPotion();
                    gManager.onlineGameInterface.ForceEndReplayAction();
                }
                else if (token is Item_BatonDeBouleDeFeu)
                {
                    Multi.Logger.Instance.Log("LOG", "Skip removal of fireball");
                    gManager.onlineGameInterface.ForceEndReplayAction();
                }
                else
                {
                    Debug.Assert(false, "Unexpected token");
                }
            }
        }

        private void ExecuteActionSimpleNote(ActionSimpleNote action)
        {
            Multi.Logger.Instance.Log("LOG", "Simple Note: " + action.message);
            gManager.onlineGameInterface.ForceEndReplayAction();
        }

        private void ExecuteActionMultiPlayerState(ActionMultiPlayerState action)
        {
            gManager.onlineGameInterface.UpdateActivePlayers(action.activePlayers);
            gManager.onlineGameInterface.ForceEndReplayAction();
        }

        private void ExecuteActionUpdateTimer(ActionUpdateTimer action)
        {
            Multi.Logger.Instance.Log("LOG", "Update Timer: Not implemented");
            gManager.onlineGameInterface.ForceEndReplayAction();
        }

        private void ExecuteActionRoomDiscovered(ActionRoomDiscovered action)
        {
            /*CharacterBehavior chara = GetTokenById(action.tokenId).GetComponent<CharacterBehavior>();
            CharacterBehaviorIHM charaIHM = chara.GetComponent<CharacterBehaviorIHM>();
            if (!chara.selected)
            {
                charaIHM.characterSelection();
            }*/
            HiddenTileBehavior tileBack = GetTileBack(action.tileIndex);
            tileBack.updateAssociatedTile(action.tileName, action.orientation);
            tileToDiscover = tileBack.GetComponent<HiddenTileBehaviorIHM>();
            gManager.onlineGameInterface.ForceEndReplayAction();
        }
        
        private void ExecuteActionRoomRotated(ActionRoomRotated action)
        {
            if (gManager.onlineGameInterface.WaitingForExpectedState()) { gManager.onlineGameInterface.ForceEndReplayAction(); return; }

            TileBehaviorIHM tile = GetTileOfIndex(action.tileIndex).GetComponent<TileBehaviorIHM>();
            CharacterBehaviorIHM chara = GetTokenByNameAndPlayerId(action.tokenName, action.playerId).GetComponent<CharacterBehaviorIHM>();
            chara.characterSelection();
            tile.inverseRotationDirection = (tile.associatedTile.clockwiseRotation != action.clockwise);
            tile.enableTileRotation(); // enable rotation, apply the rotation and than disable rotation
            // EndReplayAction is called at the end of the rotation.
        }

        private void ExecuteActionMove(ActionMove action)
        {
            if (gManager.onlineGameInterface.WaitingForExpectedState()) { gManager.onlineGameInterface.ForceEndReplayAction(); return; }

            CharacterBehavior chara = GetTokenByNameAndPlayerId(action.tokenName, action.playerId).GetComponent<CharacterBehavior>();
            CharacterBehaviorIHM charaIHM = chara.GetComponent<CharacterBehaviorIHM>();
            if (!chara.selected)
            {
                charaIHM.characterSelection();
            }

            // synchronize carried token
            if (gManager.onlineGameInterface.IsCarrier(action.tokenId))
            {
                int carriedId = gManager.onlineGameInterface.GetCarriedToken(action.tokenId);
                Token carriedToken = GetTokenById(carriedId);
                if (chara.tokenTranporte != null && chara.tokenTranporte.GetComponent<Token>() == carriedToken)
                    charaIHM.deposerToken();

                if (chara.tokenTranporte == null)
                    charaIHM.ramasserToken(carriedToken.gameObject);
            }
            else
            {
                if (chara.tokenTranporte != null)
                    charaIHM.deposerToken();
            }

            CaseBehavior target = gManager.getCase(action.y, action.x).GetComponent<CaseBehavior>();
            target.cibleAssociated.GetComponent<CibleDeplacementIHM>().moveSelectedTokenToTarget();

            // jump will automatically call end movement
            // for simple walk, end movement is called when leaving movingCharacterContinue state.
        }

        private void ExecuteActionCard(ActionCard action)
        {
            if (gManager.onlineGameInterface.WaitingForExpectedState()) { gManager.onlineGameInterface.ForceEndReplayAction(); return; }

            switch (action.action)
            {
                case ActionType.ACTION_CARD:
                    {
                        Transform actionCards = GameObject.Find("GlobalUILayout").transform.Find("Action Cards").transform;
                        actionCards.gameObject.SetActive(true);
                        for (int i = 0; i < actionCards.childCount; i++)
                        {
                            if (actionCards.GetChild(i).GetComponent<ActionCards>().actionPointsValue == action.value)
                            {
                                actionCards.GetChild(i).GetComponent<ActionCards>().launchCardInAnimation();
                                break;
                            }
                        }
                    }
                    break;
                case ActionType.COMBAT_CARD:
                    if (action.value == ActionCard.UNKNOWN) // card played notification
                    {
                        Multi.Logger.Instance.Log("LOG", "ActionPlayer: Skip Play Hidden Combat Card");
                        gManager.onlineGameInterface.ForceEndReplayAction();
                    }
                    else if (action.total == ActionCard.UNKNOWN) // my card played notification
                    {
                        Multi.Logger.Instance.Log("LOG", "ActionPlayer: Skip Play Visible Combat Card");
                        gManager.onlineGameInterface.ForceEndReplayAction();
                    }
                    else // reveal card notification
                    {
                        string log = "ActionPlayer: Check if playerId(" + action.playerId + ")";// of " + GetPlayerById(action.playerId).GetComponent<PlayerBehavior>().name;
                        log += " is different from viewerId(" + gManager.onlineGameInterface.viewerId + ") of " + GetPlayerById(gManager.onlineGameInterface.viewerId).GetComponent<PlayerBehavior>().name;
                        Multi.Logger.Instance.Log("LOG", "ActionPlayer: Skip Reveal Combat Card For Viewer");
                        if (gManager.onlineGameInterface.viewerId != action.playerId)
                        {
                            Multi.Logger.Instance.Log("LOG", "ActionPlayer: Reveal Combat Card " + action.value + " of " + action.playerId);
                            gManager.combatManager.InstanciateAndPlayCombatCard(action.value);
                        }
                        else
                        {
                            Multi.Logger.Instance.Log("LOG", "ActionPlayer: Skip Reveal Combat Card For Viewer");
                            gManager.onlineGameInterface.ForceEndReplayAction();
                        }
                    }
                    break;
                case ActionType.JUMP:
                    {
                        PlayerBehavior player = GetPlayerById(action.playerId).GetComponent<PlayerBehavior>();
                        player.nbSauts--;
                    }
                    break;
                default:
                    Debug.Assert(false, "ActionCard: Invalid Acion Type");
                    break;
            }
        }

        private void ExecuteActionStateChange(ActionStateChange action)
        {
            if (action.leaveState)
            {
                fireballTargetsDealtWith = false;
                gManager.onlineGameInterface.LeaveState(action.statename, action.activePlayer);
                gManager.onlineGameInterface.ForceEndReplayAction();
            }
            else
            {
                // HACK? no more leaveGameState message : auto leave state
                fireballTargetsDealtWith = false;
                gManager.onlineGameInterface.LeaveCurrentState();

                if (!gManager.onlineGameInterface.WaitingForExpectedState() &&
                    gManager.onlineGameInterface.GetCurrentOnlineState() == "movingCharacterContinue" &&
                    action.statename == "endAction")
                {
                    gManager.onlineGameInterface.EnterState(action.statename, action.activePlayer);
                    Debug.Assert(gManager.actionCharacter != null);
                    if (!gManager.deplacementEnCours) Debug.LogWarning("Pas de deplacement en cours");
                    gManager.actionCharacter.GetComponent<CharacterBehaviorIHM>().waitBeforeEndingMove();
                }
                else
                {
                    gManager.onlineGameInterface.EnterState(action.statename, action.activePlayer);
                    gManager.onlineGameInterface.ForceEndReplayAction();
                }
            }
        }

        private void ExecuteActionTokensUpdate(ActionTokensUpdate action)
        {

            foreach (var token in action.tokens)
                if (!token.isHidden)
                    gManager.onlineGameInterface.RegisterToken(token.playerId, token.token, token.id); // all ids update must be done first !!!

            if (gManager.onlineGameInterface.WaitingForExpectedState())
            {
                if (gManager.onlineGameInterface.GetCurrentOnlineState() == "discoverRoomPlaceToken")
                {
                    // if this assert is trigged we will need to add a
                    // hack for very special case, depending of weither bga send grouped updates or not for placed tokens.
                    Debug.Assert(action.tokens.Count <= 1, "discoverRoomPlaceToken's TokensUpdate has been grouped by bga !");
                }
                gManager.onlineGameInterface.ForceEndReplayAction();
                return;
            }

            bool discoveredRoomPlacementComplete = true;
            bool cancelCurrentMove = false;
            int placementIndex = 0;
            foreach (var token in action.tokens)
            {
                Multi.Logger.Instance.Log("DETAIL", token.ToString());
                switch (action.destinationType)
                {
                    case ActionTokensUpdate.Destination.STARTING_LINE: // PLACEMENT1
                        {
                            Debug.Assert(!token.onRoom);
                            if (isOnlinePlayer(token.playerId))
                            {
                                TokenIHM tokenIHM = GetRandomFreeCharacter(token.playerId).GetComponent<TokenIHM>();
                                tokenIHM.displayToken(); // Fix display bug
                                tokenIHM.placeTokenOnCell(gManager.getCase(token.y, token.x).GetComponent<CaseBehavior>());
                            }
                            else
                            {
                                Multi.Logger.Instance.Log("DETAIL", "Placement of hideen token on cell " + token.y + "," + token.x + " confirmed by bga");
                            }
                            break;
                        }
                    case ActionTokensUpdate.Destination.ROOM: // PLACEMENT2
                        {
                            Debug.Assert(token.onRoom);
                            HiddenTileBehavior tileBack = GetTileBack(token.room);
                            TokenIHM tokenIHM = GetRandomFreeToken(token.playerId).GetComponent<TokenIHM>();
                            if (!tileBack.placeTokenOnAvailableSpot(tokenIHM))
                                Multi.Logger.Instance.Log("ERROR", "ActionPlayer: No spot found on tile index " + tileBack.tileAssociated.GetComponent<TileBehavior>().index);
                            break;
                        }
                    case ActionTokensUpdate.Destination.REVEAL: // REVEAL STARTING CHARACTERS
                        {
                            Debug.Assert(!token.onRoom && !token.isHidden);
                            Token tokenToPlace = GetTokenByNameAndPlayerId(token.token, token.playerId);
                            Debug.Assert(tokenToPlace != null);
                            Token tokenFound = GetTokenAtPosition(token.x, token.y);
                            Debug.Assert(tokenFound != null);
                            SwitchTokenPositions(tokenToPlace, tokenFound);
                            break;
                        }
                    case ActionTokensUpdate.Destination.TO_PLACE: // ROOM DISCOVERED
                        {
                            Debug.Assert(tileToDiscover != null);
                            PlacementTokens targetPlacement = tileToDiscover.getPlacementSpot(placementIndex);
                            ++placementIndex;
                            Debug.Assert(targetPlacement != null);
                            Token tokenFound = targetPlacement.tokenAssociated.GetComponent<Token>();
                            Token tokenToPlace = GetTokenByNameAndPlayerId(token.token, token.playerId);
                            SwitchTokenPositions(tokenToPlace, tokenFound);
                            break;
                        }
                    case ActionTokensUpdate.Destination.BOARD: // MISC
                        {
                            if (gManager.onlineGameInterface.GetCurrentOnlineState() == "discoverRoomPlaceToken")
                            {
                                Debug.Assert(!token.isCarried);
                                Debug.Assert(!token.isHidden);
                                if (token.onRoom)
                                {
                                    discoveredRoomPlacementComplete = false;
                                }
                                else
                                {
                                    PlacementTokens target = FindRevealedTileTarget(gManager.getCase(token.y, token.x).GetComponent<CaseBehavior>());
                                    Debug.Assert(target != null && target.tokenAssociated == null && !target.locked);
                                    TokenIHM tokenIHM = GetTokenByNameAndPlayerId(token.token, token.playerId).GetComponent<TokenIHM>();
                                    tokenIHM.placeToken(target.gameObject);
                                }
                            }
                            else if (gManager.onlineGameInterface.GetCurrentOnlineState() == "movingCharacterContinue")
                            {
                                if (GetTokenByNameAndPlayerId(token.token, token.playerId).gameObject == gManager.actionCharacter)
                                {
                                    // update on the selected character means a move cancel, therefore: cancel move :)
                                    cancelCurrentMove = true;
                                }
                            }
                            else
                            {
                                Multi.Logger.Instance.Log("LOG", "TokensUpdate: simple update has no effect");
                            }
                            break;
                        }
                }
            }

            if (!discoveredRoomPlacementComplete) Multi.Logger.Instance.Log("LOG", "TokensUpdate: discoveredRoom placement incomplete");

            foreach (var token in action.tokens)
                if (token.isCarried)
                    gManager.onlineGameInterface.SetCarriedToken(token.holdingCharacterId, token.id);
                else
                    gManager.onlineGameInterface.SetDropped(token.id);

            if (action.destinationType == ActionTokensUpdate.Destination.TO_PLACE) // ROOM DISCOVERED
            {
                if (gManager.actionCharacter == null)
                    gManager.actionCharacter = SelectACharacterToOpenRoom(tileToDiscover).gameObject;
                if (!gManager.actionCharacter.GetComponent<CharacterBehavior>().selected)
                    gManager.actionCharacter.GetComponent<CharacterBehavior>().selectionPersonnage();
                tileToDiscover.openRoom();
                // TODO: wait for end of animation ?
                gManager.progression.BlockInteractionsUntilBgaAnswer(false); // allow player to interact again once room discovered
            }

            if (cancelCurrentMove)
                gManager.actionCharacter.GetComponent<CharacterBehaviorIHM>().cancelMovement();

            gManager.onlineGameInterface.ForceEndReplayAction();
        }



        /////////// implementation details ///////////
        private CharacterBehavior SelectACharacterToOpenRoom(HiddenTileBehaviorIHM tileToDiscover)
        {
            // TODO: choose a character that is neighboor to the room
            foreach(var token in GameObject.FindGameObjectsWithTag("Token"))
            {
                CharacterBehavior character = token.GetComponent<CharacterBehavior>();
                if (character != null && gManager.isActivePlayer(character.affiliationJoueur) && character.isTokenOnBoard())
                {
                    return character;
                }
            }
            Debug.Assert(false, "No valid character found");
            return null;
        }

        private PlacementTokens FindRevealedTileTarget(CaseBehavior cell)
        {
            foreach (GameObject cible in GameObject.FindGameObjectsWithTag("TileRevealedTarget"))
            {
                PlacementTokens target = cible.GetComponent<PlacementTokens>();
                if (target.caseActuelle == cell)
                {
                    return target;
                }
            }
            return null;
        }
        private List<Token> GetAllTokens()
        {
            List<Token> tokens = new List<Token>();
            foreach (var token in GameObject.FindGameObjectsWithTag("Token"))
            {
                tokens.Add(token.GetComponent<Token>());
            }
            return tokens;
        }

        private Token GetRandomFreeCharacter(string playerId) { return GetRandomFreeToken(playerId, true); }
        private Token GetRandomFreeToken(string playerId, bool onlyCharacter = false)
        {
            List<Token> freeTokens = new List<Token>();
            foreach (Token token in GetAllTokens())
            {
                if (token.cibleToken == null && !token.tokenPlace) // token not yet placed
                {
                    if (!onlyCharacter || token.isCharacter())
                    {
                        if (token.getOwnerId() == playerId)
                        {
                            freeTokens.Add(token);
                        }
                    }
                }
            }

            if (freeTokens.Count == 0)
            {
                return null;
            }
            else
            {
                int randomIndex = UnityEngine.Random.Range(0, freeTokens.Count);
                return freeTokens[randomIndex];
            }
        }

        private bool IsTokenAtPosition(Token token, int x, int y)
        {
            return token.cibleToken == null && token.tokenPlace && // placed on board
                token.caseActuelle.GetComponent<CaseBehavior>().row == y &&
                token.caseActuelle.GetComponent<CaseBehavior>().column == x;
        }
        private Token GetTokenAtPosition(int x, int y)
        {
            foreach (Token token in GetAllTokens())
            {
                if (IsTokenAtPosition(token, x, y)) return token;
            }
            return null;
        }

        private GameObject GetPlayerById(string playerId)
        {
            int index = gManager.onlineGameInterface.playerIndex(playerId);
            Debug.Assert(index != -1, "GetPlayerById: Supplied player id invalid (" + playerId + ")");
            return gManager.players[index];
        }
        private Token GetTokenByNameAndPlayerId(string tokenName, string playerId)
        {
            GameObject player = GetPlayerById(playerId);
            foreach (Token token in GetAllTokens())
            {
                if (token.getTokenName() == tokenName && token.affiliationJoueur == player) return token;
            }
            return null;
        }
        private Token GetTokenById(int tokenId)
        {
            int ownerIndex;
            string tokenName;
            gManager.onlineGameInterface.GetTokenFromId(tokenId, out ownerIndex, out tokenName);
            GameObject player = gManager.players[ownerIndex];
            foreach (Token token in GetAllTokens())
            {
                if (token.getTokenName() == tokenName && token.affiliationJoueur == player) return token;
            }
            return null;
        }

        private void SwitchTokenPositions(Token token1, Token token2)
        {
            if (token1 != token2)
            {
                if (token1.tokenPlace && token2.tokenPlace)
                {
                    _SwitchCellToCell(token1, token2);
                }
                else if (token2.tokenPlace)
                {
                    _SwitchTargetToCell(token1, token2);
                }
                else if (token1.tokenPlace)
                {
                    _SwitchTargetToCell(token2, token1);
                }
                else
                {
                    _SwitchTargetToTarget(token2, token1);
                }
                token1.GetComponent<TokenIHM>().refreshPosition();
                token2.GetComponent<TokenIHM>().refreshPosition();
            }
        }
        private void _SwitchTargetToCell(Token token1, Token token2)
        {
            Debug.Assert(token1.cibleToken != null);
            Debug.Assert(token2.caseActuelle != null);
            PlacementTokens cible1 = token1.cibleToken;
            CaseBehavior cell2 = token2.caseActuelle.GetComponent<CaseBehavior>();

            token2.tokenPlace = false;
            token2.caseActuelle = null;

            token1.placeTokenOnCell(cell2);
            token2.placeToken(cible1);
        }
        private void _SwitchCellToCell(Token token1, Token token2)
        {
            Debug.Assert(token1.caseActuelle != null);
            Debug.Assert(token2.caseActuelle != null);
            CaseBehavior cell1 = token1.caseActuelle.GetComponent<CaseBehavior>();
            CaseBehavior cell2 = token2.caseActuelle.GetComponent<CaseBehavior>();

            token1.caseActuelle = null;
            token2.caseActuelle = null;

            token1.placeTokenOnCell(cell2);
            token2.placeTokenOnCell(cell1);
        }
        private void _SwitchTargetToTarget(Token token1, Token token2)
        {
            Debug.Assert(token1.cibleToken != null);
            Debug.Assert(token2.cibleToken != null);
            PlacementTokens cible1 = token1.cibleToken;
            PlacementTokens cible2 = token2.cibleToken;

            token1.placeToken(cible2);
            token2.placeToken(cible1);
        }

        private HiddenTileBehavior GetTileBack(int index)
        {
            return gManager.tilesBacks[index].GetComponent<HiddenTileBehavior>();
        }

        private GameObject GetTileOfIndex(int index)
        {
            GameObject[] tiles = GameObject.FindGameObjectsWithTag("Tile");
            if (index < tiles.Length)
            {
                foreach (GameObject tile in tiles)
                {
                    if (tile.GetComponent<TileBehavior>().index == index) return tile;
                }
            }
            Debug.LogError("Game Manager, getTileOfIndex: Index given incorrect (" + index + ")");
            return null;
        }

        private bool isOnlinePlayer(string playerId)
        {
            return gManager.onlineGameInterface.isOnlineOpponent(gManager.onlineGameInterface.playerIndex(playerId));
        }
    }
}