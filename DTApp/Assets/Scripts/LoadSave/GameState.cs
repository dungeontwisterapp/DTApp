using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class GameState {
	
	bool saveDone;

    GameDataManager managerData;
	List<GameDataCharacter> charactersData;
	List<GameDataItem> itemsData;
    List<GameDataTile> tilesData;
    List<GameDataPlayers> playersData;

    public GameState()
    {
        managerData = null;
        charactersData = new List<GameDataCharacter>();
        itemsData = new List<GameDataItem>();
        tilesData = new List<GameDataTile>();
        playersData = new List<GameDataPlayers>();
        saveDone = false;
    }

    // ctor used by GameStateReader
    public GameState(GameDataManager manager, List<GameDataCharacter> characters, List<GameDataItem> items, List<GameDataTile> tiles, List<GameDataPlayers> players)
    {
        managerData = manager;
        charactersData = characters;
        itemsData = items;
        tilesData = tiles;
        playersData = players;
        saveDone = true;
    }

    public void Clear()
    {
        if (saveDone)
        {
            managerData = null;
            charactersData.Clear();
            itemsData.Clear();
            tilesData.Clear();
            playersData.Clear();
            saveDone = false;
        }
    }

    public bool isGameSaved()
    {
        return saveDone;
    }

    #region save
    public void saveGameState () {
        Clear();
        saveTokens();
        saveTiles();
        savePlayers();
        saveGameData();
        
        saveDone = true;
        Debug.Log("Save done");
	}

    private void saveTokens()
    {
        List<CharacterBehavior> characters = new List<CharacterBehavior>();
        List<Item> items = new List<Item>();
		foreach(GameObject g in GameObject.FindGameObjectsWithTag("Token")) {
			if (g.GetComponent<CharacterBehavior>() != null) characters.Add(g.GetComponent<CharacterBehavior>());
			else if (g.GetComponent<Item>() != null) items.Add(g.GetComponent<Item>());
			else Debug.LogError("GameState, saveGameState: Le Token n'est ni un personnage ni un objet");
		}
		
		foreach(CharacterBehavior chara in characters) {
			int row = -1, column = -1;
			if (chara.caseActuelle != null) {
				CaseBehavior cell = chara.caseActuelle.GetComponent<CaseBehavior>();
				row = cell.row;
				column = cell.column;
			}
            GameDataCharacter newCharacterEntry = new GameDataCharacter(chara.getTokenName(), chara.getOwnerIndex(), row, column, chara.tokenPlace, chara.horsJeu, chara.wounded, chara.freshlyWounded, chara.freshlyHealed, chara.killed, chara.actionPoints);
            if (chara.cibleToken != null)
            {
                if (chara.cibleToken.transform.parent != null) newCharacterEntry.indexRoomAssociated = chara.cibleToken.transform.parent.GetComponent<HiddenTileBehavior>().tileAssociated.GetComponent<TileBehavior>().index;
                else
                {
                    newCharacterEntry.row = chara.cibleToken.caseActuelle.row;
                    newCharacterEntry.column = chara.cibleToken.caseActuelle.column;
                }
            }
            if (chara.tokenHolder != null)
            {
                newCharacterEntry.associatedCharacterName = chara.tokenHolder.getTokenName();
                newCharacterEntry.associatedCharacterOwnerIndex = chara.tokenHolder.getOwnerIndex();
            }
            if (chara.tokenTranporte != null)
            {
                newCharacterEntry.tokenHeldName = chara.tokenTranporte.GetComponent<Token>().getTokenName();
                newCharacterEntry.tokenHeldOwnerIndex = chara.tokenTranporte.GetComponent<Token>().getOwnerIndex();
            }
			charactersData.Add(newCharacterEntry);
		}
		foreach(Item item in items) {
			int row = -1, column = -1;
			if (item.caseActuelle != null)
            {
                CaseBehavior cell = item.caseActuelle.GetComponent<CaseBehavior>();
                row = cell.row;
                column = cell.column;
			}
            GameDataItem newItemEntry = new GameDataItem(item.getTokenName(), item.getOwnerIndex(), row, column, item.tokenPlace, item.horsJeu);
            if (item.cibleToken != null)
            {
                if (item.cibleToken.transform.parent != null) newItemEntry.indexRoomAssociated = item.cibleToken.transform.parent.GetComponent<HiddenTileBehavior>().tileAssociated.GetComponent<TileBehavior>().index;
                else
                {
                    newItemEntry.row = item.cibleToken.caseActuelle.row;
                    newItemEntry.column = item.cibleToken.caseActuelle.column;
                }
            }
            if (item.tokenHolder != null)
            {
                newItemEntry.associatedCharacterName = item.tokenHolder.getTokenName();
                newItemEntry.associatedCharacterOwnerIndex = item.tokenHolder.getOwnerIndex();
            }
            itemsData.Add(newItemEntry);
		}
    }
    private void saveTiles()
    {
        GameObject[] tiles = GameObject.FindGameObjectsWithTag("Tile");
        foreach (GameObject tile in tiles)
        {
            TileBehavior t = tile.GetComponent<TileBehavior>();
            List<GameObject> herses = new List<GameObject>();
            List<HerseData> hersesData = new List<HerseData>();
            for (int i = 0; i < tile.transform.childCount; i++)
            {
                if (tile.transform.GetChild(i).GetComponent<HerseBehavior>() != null) herses.Add(tile.transform.GetChild(i).gameObject);
            }
            foreach (GameObject herse in herses)
            {
                HerseData hData = new HerseData("", "", herse.GetComponent<HerseBehavior>().herseBrisee, herse.GetComponent<HerseBehavior>().herseOuverte);
                for (int i = 0; i < tile.transform.childCount; i++)
                {
                    if (tile.transform.GetChild(i).GetComponent<CaseBehavior>() != null)
                    {
                        if (tile.transform.GetChild(i).GetComponent<CaseBehavior>().herse == herse)
                        {
                            if (hData.cellOneName == "") hData.cellOneName = tile.transform.GetChild(i).name;
                            else if (hData.cellTwoName == "") hData.cellTwoName = tile.transform.GetChild(i).name;
                        }
                    }
                }
                hersesData.Add(hData);
            }
            tilesData.Add(new GameDataTile(t.getTileName(), t.index, t.hidden, t.tileRotation, hersesData));
        }
    }
    private void savePlayers()
    {
        GameManager gManager = GameManager.gManager;
        for (int i = 0; i < gManager.players.GetLength(0); i++)
        {
            PlayerBehavior player = gManager.players[i].GetComponent<PlayerBehavior>();
            playersData.Add(new GameDataPlayers(player.victoryPoints, player.nbSauts, player.usedActionCards, player.combatCardsAvailable));
        }
    }
    private void saveGameData()
    {
        GameManager gManager = GameManager.gManager;
        GameDataManager.State gamestate = GameDataManager.buildGameState(gManager);
        int roomBeingOpenedIndex = (!GameManager.gManager.placerTokens) ? -1 : GameObject.FindGameObjectWithTag("TileRevealedTarget").GetComponent<PlacementTokens>().caseActuelle.transform.parent.GetComponent<TileBehavior>().index;
        managerData = new GameDataManager(gamestate, gManager.activePlayer.index, gManager.actionPoints, gManager.valeurMaxCarteAction, roomBeingOpenedIndex);
    }
    #endregion

    #region load
    public void loadGameState () {
        if (saveDone)
        {
            GameManager gManager = GameManager.gManager;

            Debug.Assert(gManager.progression.IsGameInInitialPhase(), "you must restore the game to its initial state before loading a new one");

            loadGameData();
            loadPlayers();
            loadTiles();
            loadTokens();

            Debug.Log("Load done");
        }
        else Debug.Log("Load failed");
	}

    private HiddenTileBehaviorIHM getHiddenTile(int indexTile)
    {
        foreach (GameObject tileBack in GameObject.FindGameObjectsWithTag("HiddenTile"))
        {
            if (tileBack.GetComponent<HiddenTileBehavior>().tileAssociated.GetComponent<TileBehavior>().index == indexTile)
            {
                return tileBack.GetComponent<HiddenTileBehaviorIHM>();
            }
        }
        return null;
    }

    private PlacementTokens findFreePlacementTargetOnHiddenTile(int indexTile)
    {
        Transform associatedTile = getHiddenTile(indexTile).transform;
        for (int j = 0; j < associatedTile.childCount; j++)
        {
            if (associatedTile.GetChild(j).name != "Highlight")
            {
                PlacementTokens cible = associatedTile.GetChild(j).GetComponent<PlacementTokens>();
                if (cible.tokenAssociated == null)
                {
                    return cible;
                }
            }
        }
        return null;
    }

    private void SetTokenPosition(Token token, int row, int column, string carrierName, int carrierOwnerIndex, int indexRoomAssociated)
    {
        if (token.isRemovedFromGame())
        {
            token.GetComponent<TokenIHM>().hideToken();
            // since it is hidden, no need to set a position
            //token.caseActuelle = GameManager.gManager.getCase(row, column);
            //newPosition = token.caseActuelle.transform.position;
        }
        else if (carrierName != null) // carried token
        {
            // do nothing: carried tokens will be placed later
        }
        else if (token.tokenPlace) // token placed on board
        {
            token.caseActuelle = GameManager.gManager.getCase(row, column);
            token.setPosition(token.caseActuelle.transform.position);
        }
        else if (indexRoomAssociated != GameDataItem.NO_REFERENCE) // placed on room
        {
            PlacementTokens cible = findFreePlacementTargetOnHiddenTile(indexRoomAssociated);
            Debug.Assert(cible != null, "No Free Placement Target found on associated tile " + indexRoomAssociated);
            token.cibleToken = cible;
            cible.tokenAssociated = token.gameObject;
            cible.locked = true;
            token.setPosition(token.cibleToken.transform.position);

            // Si un joueur vient d'ouvrir une salle et que le pion est en attente d'y etre place
            if (indexRoomAssociated == managerData.roomBeingOpenedIndex)
            {
                token.ciblesTokens.Clear();
                token.ciblesTokens.AddRange(GameObject.FindGameObjectsWithTag("TileRevealedTarget"));
                HiddenTileBehaviorIHM hiddenTile = getHiddenTile(managerData.roomBeingOpenedIndex);
                token.setPosition(hiddenTile.getWaitingToBePlacedTokenPosition(token.cibleToken.transform.position));
            }
        }
        else // not placed
        {
            Debug.Assert(!token.tokenPlace, "there should be nothing to do, since game is in initial state");
        }
    }

    private void SetCarriedTokenPosition(GameDataCharacter holderData)
    {
        Token holder = GameManager.gManager.GetTokenByNameAndOwnerIndex(holderData.name, holderData.playerIndex);
        Token carried = GameManager.gManager.GetTokenByNameAndOwnerIndex(holderData.tokenHeldName, holderData.tokenHeldOwnerIndex);

        carried.caseActuelle = holder.caseActuelle;
        carried.setPosition(carried.caseActuelle.transform.position);
        carried.tokenHolder = holder.GetComponent<CharacterBehavior>();
        carried.transform.SetParent(holder.transform);
        holder.GetComponent<CharacterBehaviorIHM>().ramasserToken(carried.gameObject, false);
    }

    private void removeTargetOfTokensToBePlacedOnDiscoveredRoom()
    {
        HiddenTileBehaviorIHM hiddenTile = getHiddenTile(managerData.roomBeingOpenedIndex);
        if (hiddenTile != null)
        {
            Transform associatedTile = hiddenTile.transform;
            for (int j = 0; j < associatedTile.childCount; j++)
            {
                if (associatedTile.GetChild(j).name != "Highlight")
                {
                    PlacementTokens cible = associatedTile.GetChild(j).GetComponent<PlacementTokens>();
                    if (cible.tokenAssociated != null)
                    {
                        cible.tokenAssociated.GetComponent<Token>().cibleToken = null;
                        cible.tokenAssociated = null;
                        cible.locked = false;
                    }
                }
            }
        }
    }

    private void SetPreviousState(Token token, int row, int column, string carrierName, int carrierOwnerIndex)
    {
        if (carrierName != null)
        {
            Debug.Assert(carrierOwnerIndex != GameDataItem.NO_REFERENCE);
            token.GetComponent<TokenIHM>().previousPersonnageAssocie = GameManager.gManager.GetTokenByNameAndOwnerIndex(carrierName, carrierOwnerIndex).gameObject;
        }
        else if (row != GameDataItem.NO_REFERENCE)
        {
            Debug.Assert(column != GameDataItem.NO_REFERENCE);
            token.GetComponent<TokenIHM>().previousCase = GameManager.gManager.getCase(row, column);
            token.GetComponent<TokenIHM>().previousPersonnageAssocie = null;
        }
    }

    private void UpdatePreviousPosition(Token token)
    {
        if (token.GetComponent<TokenIHM>().previousPersonnageAssocie != null)
        {
            TokenIHM holder = token.GetComponent<TokenIHM>().previousPersonnageAssocie.GetComponent<TokenIHM>();
            Debug.Assert(holder != null);
            if (holder.previousCase != null) token.GetComponent<TokenIHM>().previousCase = holder.previousCase;
            else token.GetComponent<TokenIHM>().previousCase = holder.GetComponent<Token>().caseActuelle;
        }
    }

    private void loadTokens()
    {
        // set characters state
        foreach (var characterData in charactersData)
        {
            Token token = GameManager.gManager.GetTokenByNameAndOwnerIndex(characterData.name, characterData.playerIndex);
            CharacterBehavior chara = token.GetComponent<CharacterBehavior>();
            Debug.Assert(chara != null);

            // Remplacement des valeurs
            chara.horsJeu = characterData.tokenHorsJeu;
            chara.tokenPlace = characterData.tokenPlace;
            chara.wounded = characterData.wounded;
            chara.freshlyWounded = characterData.freshlyWounded;
            chara.freshlyHealed = characterData.freshlyHealed;
            chara.killed = characterData.killed;
            chara.actionPoints = characterData.actionPoints;

            SetPreviousState(chara, characterData.previousRow, characterData.previousColumn, characterData.previousAssociatedCharacterName, characterData.previousAssociatedCharacterOwnerIndex);

            Debug.Assert(chara.transform.parent == GameObject.Find("Personnages").transform, "A character should be child of characterContainer by default");

            SetTokenPosition(chara, characterData.row, characterData.column, characterData.associatedCharacterName, characterData.associatedCharacterOwnerIndex, characterData.indexRoomAssociated);

            if (!chara.isRemovedFromGame() && chara.freshlyHealed) chara.GetComponent<TokenIHM>().getTokenIcon().GetComponent<SpriteRenderer>().color = Color.green;
        }

        // set items state
        foreach (var itemData in itemsData)
        {
            Token token = GameManager.gManager.GetTokenByNameAndOwnerIndex(itemData.name, itemData.playerIndex);
            Item item = token.GetComponent<Item>();
            Debug.Assert(item != null);

            // Remplacement des valeurs
            item.tokenPlace = itemData.tokenPlace;
            item.horsJeu = itemData.tokenHorsJeu;

            SetPreviousState(item, itemData.previousRow, itemData.previousColumn, itemData.previousAssociatedCharacterName, itemData.previousAssociatedCharacterOwnerIndex);

            Debug.Assert(item.transform.parent == GameObject.Find("Items").transform, "An item should be child of itemContainer by default");
            
            SetTokenPosition(item, itemData.row, itemData.column, itemData.associatedCharacterName, itemData.associatedCharacterOwnerIndex, itemData.indexRoomAssociated);
        }

        // second pass to place token carried (cannot be done before the holders are placed).
        foreach (var characterData in charactersData)
            if (characterData.tokenHeldName != null)
                SetCarriedTokenPosition(characterData);

        // third pass to update previously held tokens' previous position
        foreach (GameObject g in GameObject.FindGameObjectsWithTag("Token"))
            UpdatePreviousPosition(g.GetComponent<Token>());

        removeTargetOfTokensToBePlacedOnDiscoveredRoom();
    }
    
    private GameDataTile getTileData(int tileIndex)
    {
        foreach (var tileData in tilesData)
            if (tileIndex == tileData.tileIndex)
                return tileData;
        Debug.Assert(false, "No tile data found");
        return new GameDataTile();
    }

    private void loadTiles()
    {
        foreach (GameObject tileBackObject in GameObject.FindGameObjectsWithTag("HiddenTile"))
        {
            HiddenTileBehavior tileBack = tileBackObject.GetComponent<HiddenTileBehavior>();
            GameDataTile tileData = getTileData(tileBack.tileAssociated.GetComponent<TileBehavior>().index);
            if (!tileData.hidden) // nothing to do for hidden tiles
            {
                tileBack.updateAssociatedTile(tileData.name, tileData.tileRotation);

                TileBehavior tile = tileBack.tileAssociated.GetComponent<TileBehavior>();

                for (int j = 0; j < tileData.hersesState.Count; j++)
                {
                    // TODO: use cells on each side of the portcullis
                    HerseBehavior herseToLoad = null;
                    for (int k = 0; k < tile.transform.childCount; k++)
                    {
                        if (tile.transform.GetChild(k).name == tileData.hersesState[j].cellOneName)
                        {
                            herseToLoad = tile.transform.GetChild(k).GetComponent<CaseBehavior>().herse.GetComponent<HerseBehavior>();
                            break;
                        }
                    }
                    herseToLoad.herseBrisee = tileData.hersesState[j].broken;
                    herseToLoad.herseOuverte = tileData.hersesState[j].open;
                    if (herseToLoad.herseBrisee) herseToLoad.GetComponent<HerseBehaviorIHM>().manipulate(ActionType.DESTROYDOOR);
                    else if (herseToLoad.herseOuverte) herseToLoad.GetComponent<HerseBehaviorIHM>().manipulate(ActionType.OPENDOOR);
                }

                tile.hidden = false;
                tileBack.GetComponent<HiddenTileBehaviorIHM>().disableDisplay();
                if (tile.index == managerData.roomBeingOpenedIndex) tileBack.GetComponent<HiddenTileBehaviorIHM>().instanciateTileTargets(tile.getAvailableCells());
            }
        }
    }

    private void loadPlayers()
    {
        GameManager gManager = GameManager.gManager;
        for (int j = 0; j < gManager.players.Length; j++)
        {
            PlayerBehavior player = gManager.players[j].GetComponent<PlayerBehavior>();
            player.victoryPoints = playersData[j].victoryPoints;
            player.nbSauts = playersData[j].nbSauts;
            for (int k = 0; k < player.usedActionCards.Length; k++)
            {
                player.usedActionCards[k] = playersData[j].usedActionCards[k];
            }
            for (int k = 0; k < player.combatCardsAvailable.Length; k++)
            {
                player.combatCardsAvailable[k] = playersData[j].combatCardsAvailable[k];
            }
        }
    }
    private void loadGameData()
    {
        GameManager gManager = GameManager.gManager;

        Debug.Assert(managerData.indexJoueurActif == 0 || managerData.indexJoueurActif == 1, "GameSate, loadGameData: Index de référence incorrect (" + managerData.indexJoueurActif + ")");
        gManager.setActivePlayer( gManager.players[managerData.indexJoueurActif] );
        gManager.updateGameBackground(gManager.activePlayer);

        gManager.actionPoints = managerData.actionPoints;
        gManager.valeurMaxCarteAction = managerData.actionCardMaxValue;

        GameDataManager.applyGameState(gManager, managerData.gamestate);

        if (gManager.gameMacroState != GameProgression.GameStart)
        {
            gManager.resetCiblesToken();
            gManager.initiateHiddenTilesPlacementTokens();
        }

        // Si l'on est en train de placer les 4 premiers personnages sur le plateau, on remet à jour toutes les cibles
        switch (gManager.gameMacroState)
        {
            case GameProgression.GameStart:
                if (managerData.gamestate == GameDataManager.State.PLACEMENT1_SECOND)
                {
                    if (!gManager.onlineGame)
                    {
                        // not necessary, done in newPlayerTurn()
                        /* gManager.resetCiblesToken();
                        gManager.placementPersonnagesDepart();*/
                    }
                    else
                    {
                        gManager.progression.StartProcess(); // NW: in order to block online notifications beyond character placement until game is ready
                    }
                }
                break;
            case GameProgression.TokenPlacement:
                gManager.cameraMovementLocked = true;
                break;
            case GameProgression.Playing:
                gManager.activePlayer.myTurn = true;
                //if (gManager.turnStarted && gManager.actionPoints > 0) gManager.validationButton.SendMessage("hideButton");
                if (managerData.selectedCharacterName != null)
                {
                    gManager.selectCharacterOnLoad = true; // too early for selection, this is a hack for selecting after load.

                    Token token = gManager.GetTokenByNameAndOwnerIndex(managerData.selectedCharacterName, managerData.selectedCharacterOwnerIndex);
                    Debug.Assert(token != null && token is CharacterBehavior);
                    gManager.actionCharacter = token.gameObject;

                    if (managerData.combatTargetName != null) // preload fight
                    {
                        gManager.setActivePlayer(gManager.players[managerData.selectedCharacterOwnerIndex]); // for combat set player who initiated the fight
                        int defenderIndex = 1 - managerData.selectedCharacterOwnerIndex;
                        Token target = gManager.GetTokenByNameAndOwnerIndex(managerData.combatTargetName, defenderIndex);
                        Debug.Assert(target != null && target is CharacterBehavior);
                        gManager.combatManager.preloadFight(target.GetComponent<CharacterBehavior>());

                        Debug.Assert(gManager.combatManager.cardPlayedCount == 0);
                        for(int playerIndex=0; playerIndex < playersData.Count; ++playerIndex)
                            if (playersData[playerIndex].combatCardPlayed && !gManager.onlineGameInterface.isOnlineOpponent(playerIndex))
                            {
                                gManager.combatManager.setHiddenCard(playerIndex);
                                if (playersData[playerIndex].combatCardValue != GameDataPlayers.NO_REFERENCE)
                                    gManager.combatManager.revealCard(playerIndex, playersData[playerIndex].combatCardValue);
                            }
                    }
                    else // moving continue
                    {
                        token.GetComponent<CharacterBehavior>().deplacementRestant = managerData.pointsRemaining;
                    }
                }
                break;
            case GameProgression.GameOver:
                gManager.cameraMovementLocked = true;
                break;
        }
    }
    #endregion

    // TODO: allow to reinitialise gameManager in order to reload during an ongoing game (only needed for debug)
    #region reset
    private void clearTokenData()
    {
        GameObject[] tokens = GameObject.FindGameObjectsWithTag("Token");
        foreach (GameObject token in tokens)
        {
            Token t = token.GetComponent<Token>();
            t.caseActuelle = null;
            t.tokenPlace = false;
            t.horsJeu = false;
            if (t.cibleToken != null)
            {
                t.cibleToken.tokenAssociated = null;
                t.cibleToken.locked = false;
            }
            t.cibleToken = null;
            t.ciblesTokens.Clear();
            t.selected = false;
            t.deplacementRestant = 0;
            if (token.GetComponent<CharacterBehavior>() != null)
            {
                CharacterBehavior chara = token.GetComponent<CharacterBehavior>();
                chara.deplacementRestant = chara.MOVE_VALUE;
                chara.tokenTranporte = null;
                chara.actionPoints = 0;
                chara.wounded = false;
                chara.freshlyHealed = false;
                chara.freshlyWounded = false;
                chara.killed = false;
                chara.GetComponent<TokenIHM>().getTokenIcon().GetComponent<SpriteRenderer>().color = Color.white;
            }
            token.GetComponent<TokenIHM>().displayToken();
            token.GetComponent<Collider>().enabled = true;
        }
    }

    private void clearTileData()
    {
        foreach (GameObject tileBackObject in GameObject.FindGameObjectsWithTag("HiddenTile"))
        {
            HiddenTileBehavior tileBack = tileBackObject.GetComponent<HiddenTileBehavior>();
            TileBehavior tile = tileBack.tileAssociated.GetComponent<TileBehavior>();
            tile.hidden = true;
            tileBackObject.GetComponent<SpriteRenderer>().enabled = true;
            tileBackObject.GetComponent<HiddenTileBehaviorIHM>().resetAppearance();
        }
    }

    private void clearGameData()
    {
        GameManager.gManager.progression.Reset();
    }
    #endregion
}
