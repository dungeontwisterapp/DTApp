using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Permet de stocker les données d'un objet à un instant t
/// </summary>
[Serializable]
public struct GameDataItem {
	
	public static int NO_REFERENCE = -1;

    public string name;
    public int playerIndex;
	public int row;
	public int column;
    public bool tokenPlace;
    public bool tokenHorsJeu;
    // Si l'objet est porté par un autre personnage
    public string associatedCharacterName;
    public int associatedCharacterOwnerIndex;
    public int indexRoomAssociated;
    // Si deplacement en cours
    public string previousAssociatedCharacterName;
    public int previousAssociatedCharacterOwnerIndex;
    public int previousRow;
    public int previousColumn;

    public GameDataItem(string name, int playerIndex, int row, int column, bool tokenOnTheBoard, bool tokenRemovedFromGame)
    {
        this.name = name;
        this.playerIndex = playerIndex;
        this.row = row;
        this.column = column;
        tokenPlace = tokenOnTheBoard;
        tokenHorsJeu = tokenRemovedFromGame;

        associatedCharacterName = null;
        associatedCharacterOwnerIndex = NO_REFERENCE;
        indexRoomAssociated = NO_REFERENCE;

        previousAssociatedCharacterName = null;
        previousAssociatedCharacterOwnerIndex = NO_REFERENCE;
        previousRow = NO_REFERENCE;
        previousColumn = NO_REFERENCE;
    }

}
