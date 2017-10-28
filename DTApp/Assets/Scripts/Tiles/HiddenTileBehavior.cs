using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HiddenTileBehavior : MonoBehaviour {
	
	public GameObject cibleToken;
	public GameObject tileAssociated;
    public int tokenSpaces = 0;
	
	GameManager gManager;

    void Start()
    {
        gManager = GameManager.gManager;
	}

	// Crée plusieurs points d'ancrage sur la salle pour que l'on puisse y poser les tokens
	void placementTokensSurSalle() {
		// Si c'est une salle du milieu, on crée trois points
        switch (tokenSpaces)
        {
            case 0:
                Debug.LogWarning("Hidden Tile Behavior, placementTokensSurSalle: Le nombre de tokens à placer sur cette salle est égal à 0");
                break;
            case 1:
                instantiateCibleToken(new Vector3(transform.position.x, transform.position.y, 0.1f));
                break;
            case 2:
                instantiateCibleToken(new Vector3(transform.position.x - 1, transform.position.y - 1, 0.1f));
                instantiateCibleToken(new Vector3(transform.position.x + 1, transform.position.y + 1, 0.1f));
                break;
            case 3:
                instantiateCibleToken(new Vector3(transform.position.x - 1, transform.position.y - 1, 0.1f));
                instantiateCibleToken(new Vector3(transform.position.x, transform.position.y, 0.1f));
                instantiateCibleToken(new Vector3(transform.position.x + 1, transform.position.y + 1, 0.1f));
                break;
            case 4:
                instantiateCibleToken(new Vector3(transform.position.x - 1, transform.position.y - 1, 0.1f));
                instantiateCibleToken(new Vector3(transform.position.x - 1, transform.position.y + 1, 0.1f));
                instantiateCibleToken(new Vector3(transform.position.x + 1, transform.position.y - 1, 0.1f));
                instantiateCibleToken(new Vector3(transform.position.x + 1, transform.position.y + 1, 0.1f));
                break;
            case 5:
                instantiateCibleToken(new Vector3(transform.position.x, transform.position.y, 0.1f));
                instantiateCibleToken(new Vector3(transform.position.x - 1, transform.position.y - 1, 0.1f));
                instantiateCibleToken(new Vector3(transform.position.x - 1, transform.position.y + 1, 0.1f));
                instantiateCibleToken(new Vector3(transform.position.x + 1, transform.position.y - 1, 0.1f));
                instantiateCibleToken(new Vector3(transform.position.x + 1, transform.position.y + 1, 0.1f));
                break;
            default:
                Debug.LogError("Hidden Tile Behavior, placementTokensSurSalle: Le nombre de tokens à placer sur cette salle est inférieur à 1 ou supérieur à 5");
                break;
        }
	}

	public void instantiateCibleToken (Vector3 location) {
		GameObject temp;
		temp = (GameObject) Instantiate(cibleToken, location, cibleToken.transform.rotation);
		temp.transform.parent = transform;
		temp.GetComponent<SpriteRenderer>().enabled = true;
	}

    public List<CaseBehavior> revealTile()
    {
        List<CaseBehavior> casesAvailable = new List<CaseBehavior>();
		tileAssociated.GetComponent<TileBehavior>().hidden = false;
        gManager.actionCharacter.GetComponent<CharacterBehaviorIHM>().endDeplacementIHM();
        if (gManager.onlineGame) { gManager.actionPoints++; gManager.actionPointCost = 1; } // temporary hack to adapt to bga's behavior
        casesAvailable.AddRange(tileAssociated.GetComponent<TileBehavior>().getAvailableCells());
		for (int i=0 ; i < transform.childCount ; i++) {
			if (transform.GetChild(i).name != "Highlight") {
				Token token = transform.GetChild(i).GetComponent<PlacementTokens>().tokenAssociated.GetComponent<Token>();
				token.cibleToken = null;
			}
		}
		gManager.selectionEnCours = true;
		if (gManager.timedGame) gManager.activePlayer.setShortTimer(gManager.QUICK_ACTION_DURATION);
        return casesAvailable;
	}

    public bool placeTokenOnAvailableSpot(TokenIHM token)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).name != "Highlight")
            {
                if (transform.GetChild(i).GetComponent<PlacementTokens>().tokenAssociated == null)
                {
                    token.placeToken(transform.GetChild(i).gameObject);
                    return true;
                }
            }
        }
        return false;
    }

    public PlacementTokens getPlacementSpot(int index)
    {
        int placementIndex = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).name != "Highlight")
            {
                if (placementIndex == index)
                    return transform.GetChild(i).GetComponent<PlacementTokens>();
                else
                    placementIndex++;
            }
        }
        Debug.Assert(false, "Cannot find placement spot " + index + ": only " + placementIndex + " spots available");
        return null;
    }

    static public void SwapAssociatedTiles(HiddenTileBehavior tileBack, HiddenTileBehavior otherTileBack)
    {
        // swap associated tiles
        GameObject tmp = tileBack.tileAssociated;
        tileBack.tileAssociated = otherTileBack.tileAssociated;
        otherTileBack.tileAssociated = tmp;

        TileBehavior tile = tileBack.tileAssociated.GetComponent<TileBehavior>();
        TileBehavior otherTile = otherTileBack.tileAssociated.GetComponent<TileBehavior>();

        // swap tiles positions
        Vector3 tempPos = tile.transform.position;
        tile.transform.position = otherTile.transform.position;
        otherTile.transform.position = tempPos;

        // swap tiles indices
        int tmpIndex = tile.index;
        tile.index = otherTile.index;
        otherTile.index = tmpIndex;
        
        // finally update cells and swap rotation
        int rotation = tile.tileRotation;
        int otherRotation = otherTile.tileRotation;
        tile.GetComponent<TileBehaviorIHM>().goToDesiredOrientation(0);
        otherTile.GetComponent<TileBehaviorIHM>().goToDesiredOrientation(0);
        tile.updateTileCells();
        otherTile.updateTileCells();
        tile.GetComponent<TileBehaviorIHM>().goToDesiredOrientation(otherRotation);
        otherTile.GetComponent<TileBehaviorIHM>().goToDesiredOrientation(rotation);
    }

    static public HiddenTileBehavior FindAssociatedHiddenTileBehavior(string tileName)
    {
        foreach (GameObject tileBack in GameObject.FindGameObjectsWithTag("HiddenTile"))
            if (tileBack.GetComponent<HiddenTileBehavior>().tileAssociated.GetComponent<TileBehavior>().getTileName() == tileName)
                return tileBack.GetComponent<HiddenTileBehavior>();
        Debug.Assert(false, "No tile found");
        return null;
    }

    public void updateAssociatedTile(string tileName, int rotation)
    {
        // if associated tile is incorrect, exchange it with expected associated tile
        if (tileAssociated.GetComponent<TileBehavior>().getTileName() != tileName)
        {
            HiddenTileBehavior otherTileBack = FindAssociatedHiddenTileBehavior(tileName);
            SwapAssociatedTiles(this, otherTileBack);
        }

        tileAssociated.GetComponent<TileBehaviorIHM>().goToDesiredOrientation(rotation);
    }
}
