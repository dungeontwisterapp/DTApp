using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Permet de stocker les données d'un personnage à un instant t
/// </summary>
[Serializable]
public struct GameDataCharacter {

	public static int NO_REFERENCE = -1;
	
	public string name;
    public int playerIndex;
    public int row;
	public int column;
    public bool tokenPlace;
    public bool tokenHorsJeu;
    // Si le personnage est porté par un autre personnage
	public string associatedCharacterName;
    public int associatedCharacterOwnerIndex;
    public int indexRoomAssociated;
    // Si deplacement en cours
    public string previousAssociatedCharacterName;
    public int previousAssociatedCharacterOwnerIndex;
    public int previousRow;
    public int previousColumn;

    public bool wounded;
	public bool freshlyWounded;
	public bool freshlyHealed;
	public bool killed;
	public int actionPoints;
    // Si le personnage porte un objet ou un autre personnage
    public string tokenHeldName;
    public int tokenHeldOwnerIndex;

    public GameDataCharacter(string name, int playerIndex, int row, int column, bool tokenOnTheBoard, bool tokenRemovedFromGame, bool isWounded, bool isFreshlyWounded, bool isFreshlyHealed, bool isKilled, int currentActionPoints)
    {
		this.name = name;
        this.playerIndex = playerIndex;
        this.row = row;
		this.column = column;
        tokenPlace = tokenOnTheBoard;
        tokenHorsJeu = tokenRemovedFromGame;
		wounded = isWounded;
		freshlyWounded = isFreshlyWounded;
		freshlyHealed = isFreshlyHealed;
		killed = isKilled;
		actionPoints = currentActionPoints;

        indexRoomAssociated = NO_REFERENCE;
        associatedCharacterName = null;
        associatedCharacterOwnerIndex = NO_REFERENCE;
        tokenHeldName = null;
        tokenHeldOwnerIndex = NO_REFERENCE;

        previousAssociatedCharacterName = null;
        previousAssociatedCharacterOwnerIndex = NO_REFERENCE;
        previousRow = NO_REFERENCE;
        previousColumn = NO_REFERENCE;
    }

}
