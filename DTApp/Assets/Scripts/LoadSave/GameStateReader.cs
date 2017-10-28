using UnityEngine;
using System.Collections.Generic;

/// This class has the following purposes:
/// - reading current game data state from bga
/// - converting those data to the app format "gamestate"
/// - set the initial boarddata state according to those data.
/// - give access to other necessary data (initMoveId, initPacketId) necessary to initialize an online game
public class GameStateReader {

    public const int INVALID_INT = -10;

    private List<GameStateReaderPlayer> players;
    private List<string> tileTypes;
    private List<List<GameStateReaderTokenType>> tokenTypes;
    private List<GameStateReaderTile> tiles;
    private List<GameStateReaderToken> tokens;
    private List<GameStateReaderCard> cards;
    private List<List<GameStateReaderCell>> map;
    private GameStateReaderState gamestate;
    private bool isValid = false;
    private Multi.BGA.BoardData board;
    private GameDataManager.State gameprogression;
    private Dictionary<int, int> carriedTokenMap;


    public GameStateReader(JSONObject json, Multi.BGA.BoardData board)
    {
        this.board = board;
        readGameState(json);
    }

    public bool IsValid { get { return isValid; } }
    public int initMoveId { get { return gamestate.move_nbr; } }
    public int initPacketId { get { return gamestate.last_packet_id; } }

    public void readGameState(JSONObject json)
    {
        try
        {
            genTiles();
            extractPlayerScores(json);
            extractTokenTypes(json);
            extractTiles(json);
            extractTokens(json);
            extractCards(json);
            extractMap(json);
            // "mechanism":[] ??
            gamestate = new GameStateReaderState(json);
            isValid = true;
        }
        catch(System.Exception e)
        {
            Multi.Logger.Instance.Log("ERROR", e.ToString());
        }
    }

    public GameState convertToGameState()
    {
        buildGameProgression();
        buildBoard();

        GameDataManager managerData = buildGameDataManager();
        List<GameDataCharacter> charactersData = buildCharactersData();
        List<GameDataItem> itemsData = buildItemsData();
        List<GameDataTile> tilesData = buildTilesData();
        List<GameDataPlayers> playersData = buildPlayersData();

        return new GameState(managerData, charactersData, itemsData, tilesData, playersData);
    }


    ////// implementation details //////
    private string getFirstPlayer() { foreach (var player in players) { if (player.order == 1) return player.id; } return null; }
    private int getPlayerIndex(string id) { return gamestate.playerorder.IndexOf(id); }
    private int getAssignedTokenCount(int playerIndex) { int count = 0; foreach (var token in tokenTypes[playerIndex]) { if (token.assigned) ++count; } return count; }
    private string pickAndAssignToken(int playerIndex, string name)
    {
        foreach (var token in tokenTypes[playerIndex])
            if (!token.assigned && token.name == name)
            {
                token.assigned = true;
                return token.name;
            }
        return null;
    }
    private string randomPickAndAssignCharacter(int playerIndex)
    {
        foreach (var token in tokenTypes[playerIndex])
            if (!token.assigned && token.isCharacter)
            {
                token.assigned = true;
                return token.name;
            }
        return null;
    }
    private string randomPickAndAssignItem(int playerIndex)
    {
        foreach (var token in tokenTypes[playerIndex])
            if (!token.assigned && !token.isCharacter)
            {
                token.assigned = true;
                return token.name;
            }
        return null;
    }
    private string pickAndAssignTile(string name)
    {
        if (tileTypes.Contains(name))
        {
            tileTypes.Remove(name);
            return name;
        }
        return null;
    }
    private string randomPickAndAssignTile()
    {
        if (tileTypes.Count > 0)
        {
            string name = tileTypes[0];
            tileTypes.Remove(name);
            return name;
        }
        return null;
    }

    private void genTiles()
    {
        tileTypes = new List<string>();
        for (int type = 1; type <= 8; ++type)
        {
            tileTypes.Add(board.ConvertToGame(type.ToString()));
            Multi.Logger.Instance.Log("LOG", "READER: register tile " + board.ConvertToGame(type.ToString()) + " (" + type + ")");
        }
    }

    // return number of character not yet placed on board or on tile
    private int getCharacterPlacedCount()
    {
        int characterPlacedCount = 0;
        foreach (var token in tokens)
            if (token.isPlaced)
                characterPlacedCount++;
        return characterPlacedCount;
    }

    private int getViewerCharacterPlacedCount()
    {
        int characterPlacedCount = 0;
        foreach (var token in tokens)
            if (token.isPlaced && token.player_id == board.viewerId)
                characterPlacedCount++;
        return characterPlacedCount;
    }

    private int getDiscoveredTileCount()
    {
        int discoveredTileCount = 0;
        foreach (var tile in tiles)
            if (tile.discovered)
                discoveredTileCount++;
        return discoveredTileCount;
    }

    private void buildGameProgression()
    {
        if (gamestate.last_packet_id == 0)
        {
            gameprogression = GameDataManager.State.SETUP;
        }
        else
        {
            switch (gamestate.name)
            {
                case "characterChoice":
                    Debug.Assert(getDiscoveredTileCount() == 0);
                    Debug.Assert(getCharacterPlacedCount() == 0 || getCharacterPlacedCount() == 4);
                    if (getViewerCharacterPlacedCount() == 0)
                        gameprogression = GameDataManager.State.PLACEMENT1_FIRST;
                    else
                        gameprogression = GameDataManager.State.PLACEMENT1_SECOND;
                    break;
                case "placeToken":
                    Debug.Assert(getDiscoveredTileCount() == 0);
                    Debug.Assert(getCharacterPlacedCount() >= 8 && getCharacterPlacedCount() < 24);
                    gameprogression = GameDataManager.State.PLACEMENT2;
                    break;
                case "playerChooseCard":
                    gameprogression = GameDataManager.State.CHOOSE_ACTION_CARD;
                    break;
                case "playerChooseAction":
                    gameprogression = GameDataManager.State.PLAY_ACTION;
                    break;
                case "discoverRoomPlaceToken":
                    if (gamestate.current_player == gamestate.active_players[0])
                        gameprogression = GameDataManager.State.PLACE_TOKEN_FIRST;
                    else
                        gameprogression = GameDataManager.State.PLACE_TOKEN_SECOND;
                    break;
                case "combatChooseCard":
                    if (gamestate.active_players.Count == 2)
                        gameprogression = GameDataManager.State.CHOOSE_COMBAT_CARD_FIRST;
                    else
                        gameprogression = GameDataManager.State.CHOOSE_COMBAT_CARD_SECOND;
                    break;
                case "movingCharacter":
                    gameprogression = GameDataManager.State.SELECTION;
                    break;
                case "movingCharacterContinue":
                    gameprogression = GameDataManager.State.MOVING;
                    break;
                case "gameEnd":
                default:
                    Debug.Assert(false);
                    gameprogression = GameDataManager.State.GAMEOVER;
                    break;
            }
        }
    }

    private void buildBoard()
    {
        int selection = gamestate.selectedCharacterId == INVALID_INT ? 0 : gamestate.selectedCharacterId;
        board.Reset(getFirstPlayer(), gamestate.name, gamestate.active_players, selection);

        // register tokens id
        foreach (var token in tokens)
            if (!token.isHidden)
                board.RegisterToken(token.player_id, token.type, token.id);

        carriedTokenMap = new Dictionary<int, int>();
        foreach (var token in tokens)
        {
            if (token.isCarried)
            {
                // register tokens carried on bga side and 
                board.SetCarriedToken(token.holdingCharacterId, token.id);

                // register tokens carried for loading
                carriedTokenMap[token.holdingCharacterId] = token.id;

                // fix carried tokens position (x,y given by bga are wrong when token is carried)
                foreach (var t in tokens)
                {
                    if (t.id == token.holdingCharacterId)
                    {
                        token.x = t.x;
                        token.y = t.y;
                    }
                }
            }
        }

        // mark all tokens as carried whenever possible
        foreach (var token in tokens)
        {
            if ((token.isItem || token.wounded) && token.isOnBoard && !token.isCarried)
            {
                foreach (var t in tokens)
                {
                    if (t.isCharacter && !t.wounded && t.isOnBoard && t.x == token.x && t.y == token.y &&
                        !board.IsCarrier(token.id) && !carriedTokenMap.ContainsKey(token.id))
                    {
                        carriedTokenMap[t.id] = token.id;
                        token.location = "carried";
                        token.location_arg = t.id.ToString();
                    }
                }
            }
        }

        // create carried token backup if viewer is selected
        if (gamestate.active_players[0] == board.viewerId && (gamestate.name == "movingCharacter" || gamestate.name == "movingCharacterContinue"))
        {
            board.CreateCarriedTokenBackup();
            foreach (var elt in gamestate.modifiedTokens)
            {
                var token = elt.Value;
                if (token.isCarried) board.SetCarriedToken(token.holdingCharacterId, token.id);
                else board.SetCarriedToken(Multi.BGA.BoardData.NO_TOKEN, token.id);
            }
            board.SwitchCarriedTokenBackup();
        }
    }

    private GameDataManager buildGameDataManager()
    {
        int indexJoueurActif = gamestate.active_players.Count > 1 ? 0 : getPlayerIndex(gamestate.active_players[0]);
        int actionPoints = gamestate.action_nbr;
        int actionCardMaxValue = (gamestate.max_cards_action_played < 2) ? 2 : (gamestate.max_cards_action_played +1);

        int discoveredRoom = (gamestate.discoveredRoom == INVALID_INT) ? GameDataItem.NO_REFERENCE : board.ConvertTile(gamestate.discoveredRoom);
        string selectedCharacterName = null;
        int selectedCharacterOwner = indexJoueurActif;
        if (gamestate.selectedCharacterId != INVALID_INT)
        {
            board.GetTokenFromId(gamestate.selectedCharacterId, out selectedCharacterOwner, out selectedCharacterName);
            selectedCharacterName = board.ConvertToGame(selectedCharacterName);
        }
        string combatTargetName = null;
        if (gamestate.targetId != INVALID_INT)
        {
            int combatTargetOwner;
            board.GetTokenFromId(gamestate.targetId, out combatTargetOwner, out combatTargetName);
            combatTargetName = board.ConvertToGame(combatTargetName);
        }
        int pointsRemaining = (gamestate.pointsRemaining == INVALID_INT) ? -1 : gamestate.pointsRemaining;

        return new GameDataManager(gameprogression, indexJoueurActif, actionPoints, actionCardMaxValue, discoveredRoom, selectedCharacterName, selectedCharacterOwner, pointsRemaining, combatTargetName);
    }

    private string CreateCoolTokenLogMessage(string name, GameStateReaderToken token, string type)
    {
        string message = string.Format( "READER: {0}pick {1} {2} {3} ({4})", token.isHidden ? "random ": "", type, name, getPlayerIndex(token.player_id), token.type);
        if (!token.isPlaced) message += "not placed";
        else if (token.isOnBoard) message += string.Format("at ({0},{1}) -> ({2},{3})", token.x, token.y, board.ConvertY(token.y), board.ConvertX(token.x));
        else if (token.isOnTile) message += string.Format("on tile  {0} -> {1}", token.location_arg, board.ConvertTile(int.Parse(token.location_arg)));
        else message += "of UNKNOWN STATUS";
        return message;
    }

    private List<GameDataCharacter> buildCharactersData()
    {
        List<GameDataCharacter> charactersData = new List<GameDataCharacter>();
        // build revealed characters
        foreach (var token in tokens)
        {
            if (token.isCharacter)
            {
                int playerIndex = getPlayerIndex(token.player_id);
                string name = pickAndAssignToken(playerIndex, token.type);
                Multi.Logger.Instance.Log("LOG", CreateCoolTokenLogMessage(name, token, "character"));
                Debug.Assert(name != null);
                string tokenname = board.ConvertToGame(name);
                int row = board.ConvertY(token.y);
                int col = board.ConvertX(token.x);
                var characterData = new GameDataCharacter(tokenname, playerIndex, row, col, token.isOnBoard || token.isRemovedFromGame, token.isRemovedFromGame, token.wounded, token.wounded_this_turn, token.blocked_this_turn, token.isKilled, token.additional_actions);
                if (token.isOnTile)
                    characterData.indexRoomAssociated = board.ConvertTile(token.associatedTileId);
                if (token.toBePlaced && gamestate.discoveredRoom != INVALID_INT)
                    characterData.indexRoomAssociated = board.ConvertTile(gamestate.discoveredRoom);
                if (token.isCarried)
                {
                    board.GetTokenFromId(token.holdingCharacterId, out characterData.associatedCharacterOwnerIndex, out characterData.associatedCharacterName);
                    characterData.associatedCharacterName = board.ConvertToGame(characterData.associatedCharacterName);
                }
                if (carriedTokenMap.ContainsKey(token.id))
                {
                    board.GetTokenFromId(carriedTokenMap[token.id], out characterData.tokenHeldOwnerIndex, out characterData.tokenHeldName);
                    characterData.tokenHeldName = board.ConvertToGame(characterData.tokenHeldName);
                }
                if (gamestate.modifiedTokens.ContainsKey(token.id))
                {
                    var previousState = gamestate.modifiedTokens[token.id];
                    if (previousState.isCarried)
                    {
                        board.GetTokenFromId(previousState.holdingCharacterId, out characterData.previousAssociatedCharacterOwnerIndex, out characterData.previousAssociatedCharacterName);
                        characterData.previousAssociatedCharacterName = board.ConvertToGame(characterData.previousAssociatedCharacterName);
                    }
                    else
                    {
                        characterData.previousRow = board.ConvertY(previousState.y);
                        characterData.previousColumn = board.ConvertX(previousState.x);
                    }
                }
                charactersData.Add(characterData);
            }
        }
        // build hidden characters
        foreach (var token in tokens)
        {
            if (token.isHidden)
            {
                int playerIndex = getPlayerIndex(token.player_id);
                string name = randomPickAndAssignCharacter(playerIndex);
                Multi.Logger.Instance.Log("LOG", CreateCoolTokenLogMessage(name, token, "character"));
                if (name != null)
                {
                    string tokenname = board.ConvertToGame(name);
                    int row = board.ConvertY(token.y);
                    int col = board.ConvertX(token.x);
                    var characterData = new GameDataCharacter(tokenname, playerIndex, row, col, token.isOnBoard || token.isRemovedFromGame, token.isRemovedFromGame, token.wounded, token.wounded_this_turn, token.blocked_this_turn, token.isKilled, token.additional_actions);
                    if (token.isOnTile)
                        characterData.indexRoomAssociated = board.ConvertTile(token.associatedTileId);
                    charactersData.Add(characterData);
                }
            }
        }
        return charactersData;
    }

    private List<GameDataItem> buildItemsData()
    {
        List<GameDataItem> itemsData = new List<GameDataItem>();
        // build revealed items
        foreach (var token in tokens)
        {
            if (token.isItem)
            {
                int playerIndex = getPlayerIndex(token.player_id);
                string name = pickAndAssignToken(playerIndex, token.type);
                Multi.Logger.Instance.Log("LOG", CreateCoolTokenLogMessage(name, token, "item"));
                Debug.Assert(name != null);
                string tokenname = board.ConvertToGame(name);
                int row = board.ConvertY(token.y);
                int col = board.ConvertX(token.x);
                var itemData = new GameDataItem(tokenname, playerIndex, row, col, token.isOnBoard || token.isRemovedFromGame, token.isRemovedFromGame);
                if (token.isOnTile)
                    itemData.indexRoomAssociated = board.ConvertTile(token.associatedTileId);
                if (token.toBePlaced && gamestate.discoveredRoom != INVALID_INT)
                    itemData.indexRoomAssociated = board.ConvertTile(gamestate.discoveredRoom);
                if (token.isCarried)
                {
                    board.GetTokenFromId(token.holdingCharacterId, out itemData.associatedCharacterOwnerIndex, out itemData.associatedCharacterName);
                    itemData.associatedCharacterName = board.ConvertToGame(itemData.associatedCharacterName);
                }
                if (gamestate.modifiedTokens.ContainsKey(token.id))
                {
                    var previousState = gamestate.modifiedTokens[token.id];
                    if (previousState.isCarried)
                    {
                        board.GetTokenFromId(previousState.holdingCharacterId, out itemData.previousAssociatedCharacterOwnerIndex, out itemData.previousAssociatedCharacterName);
                        itemData.previousAssociatedCharacterName = board.ConvertToGame(itemData.previousAssociatedCharacterName);
                    }
                    else
                    {
                        itemData.previousRow = board.ConvertY(previousState.y);
                        itemData.previousColumn = board.ConvertX(previousState.x);
                    }
                }
                itemsData.Add(itemData);
            }
        }
        // build hidden items (in reverse order to take the tokens left by characters loop)
        for (int i = tokens.Count - 1; i >= 0; --i)
        {
            var token = tokens[i];
            if (token.isHidden)
            {
                int playerIndex = getPlayerIndex(token.player_id);
                string name = randomPickAndAssignItem(playerIndex);
                Multi.Logger.Instance.Log("LOG", CreateCoolTokenLogMessage(name, token, "item"));
                if (name != null)
                {
                    string tokenname = board.ConvertToGame(name);
                    int row = board.ConvertY(token.y);
                    int col = board.ConvertX(token.x);
                    var itemData = new GameDataItem(tokenname, playerIndex, row, col, token.isOnBoard, token.isRemovedFromGame);
                    if (token.isOnTile)
                        itemData.indexRoomAssociated = board.ConvertTile(token.associatedTileId);
                    itemsData.Add(itemData);
                }
            }
        }
        return itemsData;
    }

    private List<GameStateReaderHerse> buildHerses()
    {
        List<GameStateReaderHerse> herses = new List<GameStateReaderHerse>();
        foreach(var cells in map)
        {
            foreach (var cell in cells)
            {
                int x = board.ConvertX(cell.x);
                int y = board.ConvertY(cell.y);
                for (int bgaDir = 0; bgaDir < 4; ++bgaDir)
                {
                    int dir = board.ConverHerseDirection(bgaDir);
                    GameStateReaderHerse herse = null;
                    switch (cell.walls[bgaDir])
                    {
                        case "portcullis_closed":
                            herse = new GameStateReaderHerse(x, y, dir, false, false);
                            break;
                        case "portcullis_open":
                            herse = new GameStateReaderHerse(x, y, dir, false, true);
                            break;
                        case "portcullis_broken":
                            herse = new GameStateReaderHerse(x, y, dir, true, false);
                            break;
                    }
                    if (herse != null)
                    {
                        bool alreadyRegistered = false;
                        foreach(var h in herses)
                        {
                            if (h.IsOpposite(herse))
                            {
                                Debug.Assert(h.SameState(herse));
                                alreadyRegistered = true;
                                break;
                            }
                        }
                        if (!alreadyRegistered)
                        {
                            string message = string.Format("READER: create herse ({0},{1})-{2} -> ({3},{4})/({5},{6}) in state {7}",
                                x, y, dir, herse.x1, herse.y1, herse.x2, herse.y2, herse.broken ? "broken" : (herse.open ? "open" : "close"));
                            Multi.Logger.Instance.Log("LOG", message);
                            herses.Add(herse);
                        }
                    }
                }
            }
        }
        return herses;
    }

    private string convertToCellName(int x, int y, int orientation)
    {
        // convert to local non oriented coordinates
        int localx = 1 + ((x - 1) % 5);
        int localy = 1 + (y % 5);
        // reflect to adapt to app tiles index convention
        int reflectx = localy;
        int reflecty = localx;
        // orient depending on tile orientation
        switch (orientation % 4)
        {
            case 0:
                localx = reflectx;
                localy = reflecty;
                break;
            case 1:
                localx = 6 - reflecty;
                localy = reflectx;
                break;
            case 2:
                localx = 6 - reflectx;
                localy = 6 - reflecty;
                break;
            case 3:
                localx = reflecty;
                localy = 6 - reflectx;
                break;
        }
        string message = string.Format("READER: convert cell ({0},{1}) oriented toward {2} to cell {3}",
            x, y, orientation, "Case " + localx.ToString() + localy.ToString());
        Multi.Logger.Instance.Log("LOG", message);
        return "Case " + localx.ToString() + localy.ToString();
    }

    private void addHersesToTiles(List<GameStateReaderHerse> herses, List<GameDataTile> tilesData)
    {
        Dictionary<int, List<GameStateReaderHerse>> map = new Dictionary<int, List<GameStateReaderHerse>>();
        for (int index = 0; index < 8; ++index)
            map[index] = new List<GameStateReaderHerse>();

        foreach (var herse in herses)
        {
            //int id = herse.getTileId();
            //int index = board.ConvertTile(id);
            int index = herse.getTileId();
            map[index].Add(herse);
        }

        for (int i = 0; i < 8; ++i)
        {
            var tileData = tilesData[i];
            Multi.Logger.Instance.Log("LOG", "add herses to tile " + tileData.name + " (" + tileData.tileIndex + ")");
            foreach(var herse in map[tileData.tileIndex])
            {
                string firstCell = convertToCellName(herse.x1, herse.y1, tileData.tileRotation);
                string secondCell = convertToCellName(herse.x2, herse.y2, tileData.tileRotation);
                tileData.hersesState.Add(new HerseData(firstCell, secondCell, herse.broken, herse.open));
            }
        }
    }

    private List<GameDataTile> buildTilesData()
    {
        List<GameDataTile> tilesData = new List<GameDataTile>();
        // build revealed tiles
        foreach (var tile in tiles)
        {
            if (tile.discovered)
            {
                Debug.Assert(tile.type != -1);
                string tilename = board.ConvertToGame(tile.type.ToString());
                string name = pickAndAssignTile(tilename);
                int tileIndex = board.ConvertTile(tile.id);
                int tileOrientation = board.ConverTileOrientation(tile.orientation, tile.type);
                Multi.Logger.Instance.Log("LOG", "READER: pick tile " + name + " (" + tilename + ") oriented toward " + tile.orientation + "->" + tileOrientation);
                Debug.Assert(name != null);
                tilesData.Add(new GameDataTile(name, tileIndex, !tile.discovered, tileOrientation, new List<HerseData>()));
            }
        }
        // build hidden tiles
        foreach (var tile in tiles)
        {
            if (!tile.discovered)
            {
                Debug.Assert(tile.type == -1);
                string name = randomPickAndAssignTile();
                int tileIndex = board.ConvertTile(tile.id);
                int tileOrientation = board.ConverTileOrientation(tile.orientation, tile.type);
                Multi.Logger.Instance.Log("LOG", "READER: random pick tile " + name + " oriented toward " + tile.orientation + "->" + tileOrientation);
                Debug.Assert(name != null);
                tilesData.Add(new GameDataTile(name, tileIndex, !tile.discovered, tileOrientation, new List<HerseData>()));
            }
        }

        List<GameStateReaderHerse> herses = buildHerses();
        addHersesToTiles(herses, tilesData);

        return tilesData;
    }
    
    private List<GameDataPlayers> buildPlayersData()
    {
        List<GameDataPlayers> playersData = new List<GameDataPlayers>();
        foreach(var player in players)
        {
            int victoryPoints = player.score;
            string playerId = player.id;
            int nbSauts = 0;
            bool[] usedActionCards = { true, true, true, true };
            Dictionary<int, int> combatCardsIndex = new Dictionary<int, int>();
            combatCardsIndex[0] = 0;
            combatCardsIndex[1] = 1;
            combatCardsIndex[2] = 2 + 1;
            combatCardsIndex[3] = 4 + 1;
            combatCardsIndex[4] = 6;
            combatCardsIndex[5] = 7;
            combatCardsIndex[6] = 8;
            bool[] combatCardsAvailable = { false, false, false, false, false, false, false, false, false };
            bool combatCardPlayed = (gamestate.name == "combatChooseCard" && !gamestate.active_players.Contains(player.id));
            int combatCardValue = GameDataPlayers.NO_REFERENCE;

            bool noActionCardFound = true;
            foreach (var card in cards)
            {
                if (card.location == "hand" && card.location_arg == playerId)
                {
                    switch (card.type)
                    {
                        case "jump":
                            nbSauts++;
                            break;
                        case "action":
                            usedActionCards[card.type_arg - 2] = false;
                            noActionCardFound = false;
                            break;
                        case "combat":
                            int value = card.type_arg;
                            combatCardsAvailable[combatCardsIndex[value]] = true;
                            combatCardsIndex[value]--;
                            break;
                        default:
                            Debug.Assert(false);
                            break;
                    }
                }
                if (card.location == "combat" && card.location_arg == playerId)
                {
                    Debug.Assert(combatCardPlayed);
                    combatCardValue = card.type_arg;
                    // Hack: Do not mark the combat card as played if not viewer Id (In a perfect world bga should not tell us that the card has been played)
                    if (playerId != board.viewerId)
                    {
                        int value = card.type_arg;
                        combatCardsAvailable[combatCardsIndex[value]] = true;
                        combatCardsIndex[value]--;
                    }
                }
            }

            if (noActionCardFound) // need to draw a new set of action cards
                for (int i=0; i<usedActionCards.Length; ++i)
                    usedActionCards[i] = false;

            playersData.Add( new GameDataPlayers(victoryPoints, nbSauts, usedActionCards, combatCardsAvailable, combatCardPlayed, combatCardValue) );
        }
        Debug.Assert(playersData.Count == 2);
        if (board.flipBoardDisplay)
            playersData.Reverse();
        return playersData;
    }

    /// Extraction methods
    private void extractPlayerScores(JSONObject json)
    {
        players = new List<GameStateReaderPlayer>();
        Debug.Assert(JSONTools.HasFieldOfTypeObject(json, "players"));
        JSONObject jsonplayers = json.GetField("players");
        foreach (string key in jsonplayers.keys)
        {
            Debug.Assert(JSONTools.HasFieldOfTypeObject(jsonplayers, key));
            players.Add(new GameStateReaderPlayer(jsonplayers.GetField(key)));
        }
        Debug.Assert(players.Count == 2);
    }

    private void extractTokenTypes(JSONObject json)
    {
        tokenTypes = new List<List<GameStateReaderTokenType>>();
        tokenTypes.Add(new List<GameStateReaderTokenType>()); // player 1
        tokenTypes.Add(new List<GameStateReaderTokenType>()); // player 2
        Debug.Assert(JSONTools.HasFieldOfTypeObject(json, "token_types"));
        JSONObject jsontokens = json.GetField("token_types");
        foreach (string key in jsontokens.keys)
        {
            Debug.Assert(JSONTools.HasFieldOfTypeObject(jsontokens, key));
            JSONObject jsontoken = jsontokens.GetField(key);
            string category = JSONTools.GetStrValue(jsontoken, "category");
            Debug.Assert(category != null);

            Multi.Logger.Instance.Log("LOG", "READER: register " + key + " " + category);
            tokenTypes[0].Add(new GameStateReaderTokenType(key, category));
            tokenTypes[1].Add(new GameStateReaderTokenType(key, category));
        }
        Debug.Assert(tokenTypes[0].Count == 14 && tokenTypes[1].Count == 14);
    }

    private void extractTiles(JSONObject json)
    {
        tiles = new List<GameStateReaderTile>();
        Debug.Assert(JSONTools.HasFieldOfTypeArray(json, "map_tiles"));
        foreach(JSONObject tile in json.GetField("map_tiles").list)
        {
            tiles.Add(new GameStateReaderTile(tile));
        }
        Debug.Assert(tiles.Count == 8);
    }

    private void extractTokens(JSONObject json)
    {
        tokens = new List<GameStateReaderToken>();
        Debug.Assert(JSONTools.HasFieldOfTypeArray(json, "tokens"));
        foreach (JSONObject token in json.GetField("tokens").list)
        {
            tokens.Add(new GameStateReaderToken(token));
        }
        Debug.Assert(tiles.Count == 8);
    }

    private void extractCards(JSONObject json)
    {
        cards = new List<GameStateReaderCard>();
        Debug.Assert(JSONTools.HasFieldOfTypeObject(json, "hand"));
        JSONObject hand = json.GetField("hand");
        foreach (string key in hand.keys)
        {
            cards.Add(new GameStateReaderCard(hand.GetField(key)));
        }
    }

    private void extractMap(JSONObject json)
    {
        map = new List<List<GameStateReaderCell>>();
        Debug.Assert(JSONTools.HasFieldOfTypeArray(json, "map"));
        foreach (JSONObject column in json.GetField("map").list)
        {
            var cells = new List<GameStateReaderCell>();
            Debug.Assert(JSONTools.HasFieldOfTypeArray(json, "map"));
            foreach (JSONObject cell in column.list)
            {
                cells.Add(new GameStateReaderCell(cell));
            }
            Debug.Assert(cells.Count == 10);
            map.Add(cells);
        }
        Debug.Assert(map.Count == 22);
    }
}

class GameStateReaderPlayer
{
    public string id;
    public int score;
    public string color;
    public int order;

    public GameStateReaderPlayer(JSONObject json)
    {
        id = JSONTools.GetStrValue(json, "id");
        score = JSONTools.GetIntValue(json, "score", GameStateReader.INVALID_INT);
        color = JSONTools.GetStrValue(json, "color");
        order = JSONTools.GetIntValue(json, "player_no", GameStateReader.INVALID_INT);
        Debug.Assert(isValid);
    }

    public bool isValid
    {
        get
        {
            return (id != null) && (score != GameStateReader.INVALID_INT) &&
                    (color != null) && (order != GameStateReader.INVALID_INT);
        }
    }
}

class GameStateReaderTokenType
{
    public string name;
    public string category;
    public bool assigned;

    public bool isCharacter { get { return category == "character"; } }

    public GameStateReaderTokenType(string name, string category)
    {
        this.name = name;
        this.category = category;
        assigned = false;
    }
}

class GameStateReaderTile
{
    public int id;
    public int type;
    public int x;
    public int y;
    public int orientation;
    public bool discovered;

    public GameStateReaderTile(JSONObject json)
    {
        id = JSONTools.GetIntValue(json, "id", GameStateReader.INVALID_INT);
        type = JSONTools.GetIntValue(json, "type", GameStateReader.INVALID_INT);
        x = JSONTools.GetIntValue(json, "x", GameStateReader.INVALID_INT);
        y = JSONTools.GetIntValue(json, "y", GameStateReader.INVALID_INT);
        orientation = JSONTools.GetIntValue(json, "orientation", GameStateReader.INVALID_INT);
        discovered = JSONTools.GetBoolValue(json, "discovered", 1);
        Debug.Assert(isValid);
    }

    public bool isValid {
        get {
            return (id != GameStateReader.INVALID_INT) && (type != GameStateReader.INVALID_INT) &&
                (x != GameStateReader.INVALID_INT) && (y != GameStateReader.INVALID_INT) &&
                (orientation != GameStateReader.INVALID_INT);
        }
    }
}

class GameStateReaderToken
{
    public int id;
    public string player_id;
    public string category;
    public string type;
    public string location; // not_placed, to_place, ontile, ingame, carried, removed, initial
    public string location_arg;
    public int x;
    public int y;
    public bool wounded;
    public bool wounded_this_turn;
    public bool blocked_this_turn;
    public int additional_actions;

    public bool isCharacter { get { return category == "character"; } }
    public bool isItem { get { return category == "object"; } }
    public bool isHidden { get { return category == "hidden"; } }
    public bool isPlaced { get { return location != "to_place" && location != "not_placed"; } }
    public bool toBePlaced { get { return location == "to_place"; } }
    public bool isOnBoard { get { return location == "initial" || location == "ingame" || location == "carried"; } }
    public bool isOnTile { get { return location == "ontile"; } }
    public bool isRemovedFromGame { get { return location == "removed"; } }
    public bool isKilled { get { return false; } } // killed are marked as removed from the game
    public bool isCarried { get { return location == "carried"; } }

    public int holdingCharacterId { get { Debug.Assert(isCarried); return locationArgToInt; } }
    public int associatedTileId { get { Debug.Assert(isOnTile); return locationArgToInt; } }

    private int locationArgToInt { get { int result = 0; if (!int.TryParse(location_arg, out result)) Debug.Assert(false); return result; } }

    public GameStateReaderToken(JSONObject json, bool initialState = false)
    {
        id = JSONTools.GetIntValue(json, "id", GameStateReader.INVALID_INT);
        player_id = JSONTools.GetStrValue(json, "player_id");
        category = JSONTools.GetStrValue(json, "category");
        type = JSONTools.GetStrValue(json, "type");

        if (!initialState)
        {
            location = JSONTools.GetStrValue(json, "location");
            location_arg = JSONTools.GetStrValue(json, "location_arg");
            x = JSONTools.GetIntValue(json, "x", GameStateReader.INVALID_INT);
            y = JSONTools.GetIntValue(json, "y", GameStateReader.INVALID_INT);
            wounded = JSONTools.GetBoolValue(json, "wounded", 1);
            wounded_this_turn = JSONTools.GetBoolValue(json, "wounded_this_turn", 1);
            blocked_this_turn = JSONTools.GetBoolValue(json, "blocked_this_turn", 1);
            additional_actions = JSONTools.GetIntValue(json, "additional_actions", GameStateReader.INVALID_INT);
        }
        else
        {
            location = JSONTools.GetStrValue(json, "initial_location");
            location_arg = JSONTools.GetStrValue(json, "initial_location_arg");
            x = JSONTools.GetIntValue(json, "initial_x", GameStateReader.INVALID_INT);
            y = JSONTools.GetIntValue(json, "initial_y", GameStateReader.INVALID_INT);
            wounded = false;
            wounded_this_turn = false;
            blocked_this_turn = false;
            additional_actions = 0;
        }

        Debug.Assert(isValid);
    }

    public bool isValid
    {
        get
        {
            return (id != GameStateReader.INVALID_INT) && (x != GameStateReader.INVALID_INT) && (y != GameStateReader.INVALID_INT) &&
                (player_id != null) && (category != null) &&
                (type != null) && (location != null) &&
                (location_arg != null) && (additional_actions != GameStateReader.INVALID_INT);
        }
    }
}

class GameStateReaderCard
{
    public int id;
    public string type;
    public int type_arg;
    public string location;
    public string location_arg;

    public GameStateReaderCard(JSONObject json)
    {
        id = JSONTools.GetIntValue(json, "id", GameStateReader.INVALID_INT);
        type = JSONTools.GetStrValue(json, "type");
        type_arg = JSONTools.GetIntValue(json, "type_arg", GameStateReader.INVALID_INT);
        location = JSONTools.GetStrValue(json, "location");
        location_arg = JSONTools.GetStrValue(json, "location_arg");
        Debug.Assert(isValid);
    }

    public bool isValid
    {
        get
        {
            return (id != GameStateReader.INVALID_INT) && (type_arg != GameStateReader.INVALID_INT) &&
                (type != null) && (location != null) &&
                (location_arg != null);
        }
    }
}

class GameStateReaderCell
{
    public int x;
    public int y;
    public string type;
    public string type_arg;
    public List<string> walls; // n, w, s, e  (0=haut, 1=gauche, 2=bas, 3=droite)

    public GameStateReaderCell(JSONObject json)
    {
        x = JSONTools.GetIntValue(json, "x", GameStateReader.INVALID_INT);
        y = JSONTools.GetIntValue(json, "y", GameStateReader.INVALID_INT);
        type = JSONTools.GetStrValue(json, "type");
        type_arg = JSONTools.GetStrValue(json, "type_arg");
        walls = new List<string>();
        walls.Add( JSONTools.GetStrValue(json, "wall_n") );
        walls.Add( JSONTools.GetStrValue(json, "wall_w") );
        walls.Add( JSONTools.GetStrValue(json, "wall_s") );
        walls.Add( JSONTools.GetStrValue(json, "wall_e") );
        Debug.Assert(isValid);
    }

    public bool isValid
    {
        get
        {
            return (x != GameStateReader.INVALID_INT) && (y != GameStateReader.INVALID_INT) &&
                (type != null) && (type_arg != null) && (walls.Count == 4) &&
                (walls[0] != null) &&( walls[1] != null) &&
                (walls[2] != null) && (walls[3] != null);
        }
    }
}

class GameStateReaderHerse
{
    public int x1, y1, y2, x2;
    public bool broken;
    public bool open;

    public GameStateReaderHerse(int x, int y, int dir, bool broken, bool open)
    {
        x1 = x;
        y1 = y;
        x2 = x + incX(dir);
        y2 = y + incY(dir);
        this.broken = broken;
        this.open = open;
    }

    static private int incX(int dir) { return dir == 1 ? -1 : (dir == 3 ? 1 : 0); }
    static private int incY(int dir) { return dir == 0 ? -1 : (dir == 2 ? 1 : 0); }
    static private int opposite(int dir) { return (dir+2) % 4; }

    static private int getTileLocation(int x, int y) { return (x-1)/5 + 4*(y/5); }

    public bool IsOpposite(GameStateReaderHerse herse)
    {
        return (x1 == herse.x2) && (y1 == herse.y2) && (x2 == herse.x1) && (y2 == herse.y1);
    }

    public bool SameState(GameStateReaderHerse herse)
    {
        return (broken == herse.broken) && (open == herse.open);
    }

    public int getTileId()
    {
        int id1 = getTileLocation(x1, y1);
        int id2 = getTileLocation(x2, y2);
        Debug.Assert(id1 == id2);
        return id1;
    }
}


class GameStateReaderState
{
    // game state
    public string name;
    public string type;
    public List<string> active_players;
    // game infos
    public int action_nbr;
    public int max_cards_action_played;
    public int tablespeed;
    public int rtc_mode;
    public string game_result_neutralized;
    public string neutralized_player_id;
    public List<string> playerorder;
    // notifications
    public int last_packet_id;
    public int move_nbr;
    // state specifics
    public string current_player;
    public int discoveredRoom;
    public int selectedCharacterId, targetId;
    public int pointsRemaining;
    public Dictionary<int, GameStateReaderToken> modifiedTokens;

    public GameStateReaderState(JSONObject json)
    {
        extractGameState(json);
        extractGameInfos(json);
        extractNotifications(json);
        Debug.Assert(isValid);
    }

    public bool isValid
    {
        get
        {
            bool isValid = true;
            isValid = isValid && (name != null) && (type != null) && (active_players.Count > 0);
            isValid = isValid && (max_cards_action_played != GameStateReader.INVALID_INT);
            isValid = isValid && (tablespeed != GameStateReader.INVALID_INT) && (rtc_mode != GameStateReader.INVALID_INT);
            isValid = isValid && (playerorder.Count == 2) && (playerorder[0] != null) && (playerorder[1] != null);
            isValid = isValid && (last_packet_id != GameStateReader.INVALID_INT) && (move_nbr != GameStateReader.INVALID_INT) && (action_nbr != GameStateReader.INVALID_INT);
            if (name == "discoverRoomPlaceToken")
                isValid = isValid && (current_player != null && discoveredRoom != GameStateReader.INVALID_INT);
            if (name == "movingCharacter" || name == "movingCharacterContinue")
                isValid = isValid && (selectedCharacterId != GameStateReader.INVALID_INT && pointsRemaining != GameStateReader.INVALID_INT);
            return isValid;
        }
    }

    private void extractGameState(JSONObject json)
    {
        Debug.Assert(JSONTools.HasFieldOfTypeObject(json, "gamestate"));
        JSONObject gamestate = json.GetField("gamestate");
        name = JSONTools.GetStrValue(gamestate, "name");
        type = JSONTools.GetStrValue(gamestate, "type");

        active_players = new List<string>();
        switch (type)
        {
            case "manager":
            case "game":
            case "activeplayer":
                active_players.Add(JSONTools.GetStrValue(gamestate, "active_player"));
                break;
            case "multipleactiveplayer":
                Debug.Assert(JSONTools.HasFieldOfTypeArray(gamestate, "multiactive"));
                foreach (JSONObject id in gamestate.GetField("multiactive").list)
                {
                    Debug.Assert(id.IsString || id.IsNumber);
                    if (id.IsString) active_players.Add(id.str);
                    else active_players.Add(Mathf.RoundToInt(id.n).ToString());
                }
                Debug.Assert(active_players.Count <= 2);
                break;
            default:
                Debug.Assert(false, "unhandled gamestate type");
                break;
        }

        current_player = null;
        discoveredRoom = GameStateReader.INVALID_INT;
        selectedCharacterId = GameStateReader.INVALID_INT;
        targetId = GameStateReader.INVALID_INT;
        pointsRemaining = GameStateReader.INVALID_INT;
        modifiedTokens = new Dictionary<int, GameStateReaderToken>();

        Debug.Assert(gamestate.HasField("args"));
        JSONObject args = gamestate.GetField("args");
        switch (name)
        {
            case "movingCharacter":
            case "movingCharacterContinue":
                {
                    Debug.Assert(args.IsObject);
                    selectedCharacterId = JSONTools.GetIntValue(args, "character_id");
                    pointsRemaining = JSONTools.GetIntValue(args, "points");
                    if (JSONTools.HasFieldOfTypeArray(args, "inital_objects"))
                    {
                        foreach(var token in args.GetField("inital_objects").list)
                        {
                            var tokenData = new GameStateReaderToken(token, true);
                            Debug.Assert(!modifiedTokens.ContainsKey(tokenData.id));
                            modifiedTokens[tokenData.id] = tokenData;
                        }
                    }
                }
                break;
            case "discoverRoomPlaceToken":
                {
                    Debug.Assert(args.IsObject);
                    current_player = JSONTools.GetStrValue(args, "turn");
                    discoveredRoom = JSONTools.GetIntValue(args, "room", GameStateReader.INVALID_INT);
                }
                break;
            case "combatChooseCard":
                {
                    Debug.Assert(args.IsObject);
                    selectedCharacterId = JSONTools.GetIntValue(args, "character_id");
                    targetId = JSONTools.GetIntValue(args, "target_id");
                }
                break;
        }
    }

    private void extractGameInfos(JSONObject json)
    {
        max_cards_action_played = JSONTools.GetIntValue(json, "max_cards_action_played", GameStateReader.INVALID_INT);
        tablespeed = JSONTools.GetIntValue(json, "tablespeed", GameStateReader.INVALID_INT);
        rtc_mode = JSONTools.GetIntValue(json, "rtc_mode", GameStateReader.INVALID_INT);
        game_result_neutralized = JSONTools.GetStrValue(json, "game_result_neutralized");
        neutralized_player_id = JSONTools.GetStrValue(json, "neutralized_player_id");

        Debug.Assert(JSONTools.HasFieldOfTypeObject(json, "actions"));
        JSONObject actions = json.GetField("actions");
        action_nbr = JSONTools.GetIntValue(actions, "action_nbr", GameStateReader.INVALID_INT);

        playerorder = new List<string>();
        Debug.Assert(JSONTools.HasFieldOfTypeArray(json, "playerorder"));
        foreach (JSONObject id in json.GetField("playerorder").list)
        {
            Debug.Assert(id.IsString || id.IsNumber);
            if (id.IsString) playerorder.Add(id.str);
            else playerorder.Add(Mathf.RoundToInt(id.n).ToString());
        }
        Debug.Assert(playerorder.Count == 2);
    }

    private void extractNotifications(JSONObject json)
    {
        Debug.Assert(JSONTools.HasFieldOfTypeObject(json, "notifications"));
        JSONObject notifications = json.GetField("notifications");
        last_packet_id = JSONTools.GetIntValue(notifications, "last_packet_id", GameStateReader.INVALID_INT);
        move_nbr = JSONTools.GetIntValue(notifications, "move_nbr", GameStateReader.INVALID_INT);
    }
}


