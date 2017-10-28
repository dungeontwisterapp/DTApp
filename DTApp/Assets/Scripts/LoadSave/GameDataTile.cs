using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Permet de stocker les données d'une salle à un instant t
/// </summary>
[Serializable]
public struct GameDataTile {

    public string name;
	public int tileIndex;
	public bool hidden;
	public int tileRotation;
    public List<HerseData> hersesState;

    public GameDataTile(string room, int currentTileIndex, bool tileHidden, int currentTileRotation, List<HerseData> herses)
    {
        name = room;
        tileIndex = currentTileIndex;
		hidden = tileHidden;
		tileRotation = currentTileRotation;
        hersesState = new List<HerseData>();
        hersesState.AddRange(herses);
	}

}

[Serializable]
public struct HerseData
{
    public string cellOneName;
    public string cellTwoName;
    public bool broken;
    public bool open;

    public HerseData(string firstCell, string secondCell, bool broken, bool open)
    {
        cellOneName = firstCell;
        cellTwoName = secondCell;
        this.broken = broken;
        this.open = open;
    }
}
