using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ButtonCancel : MonoBehaviour {

    GameManager gManager;
    Image image;
    Button button;
    public AudioClip cancelSound;

    // Use this for initialization
    void Start()
    {
        gManager = GameManager.gManager;
        image = GetComponent<Image>();
        button = GetComponent<Button>();
    }

    void Update()
    {
        image.enabled = gManager.displayCancelButton;
        button.enabled = gManager.displayCancelButton;
    }

    public void cancel()
    {
        if (gManager.deplacementEnCours) cancelMovement();
        else if (gManager.rotationEnCours) cancelRotation();
    }

    void cancelMovement()
    {
        Debug.Assert(gManager.actionCharacter != null, "ButtonCancel, cancelMovement: Aucun personnage sélectionné");
        gManager.playSound(cancelSound);
        gManager.actionCharacter.SendMessage("cancelMovement");
    }

    void cancelRotation()
    {
        GameObject[] tiles = GameObject.FindGameObjectsWithTag("Tile");
        int len = tiles.Length;
        for (int i = 0; i < len; ++i)
        {
            if (tiles[i].GetComponent<TileBehaviorIHM>().selectedForRotation())
            {
                gManager.actionPointCost = 0;
                tiles[i].GetComponent<TileBehaviorIHM>().cancelRotation();
                break;
            }
        }
    }

}
