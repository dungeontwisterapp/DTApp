using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CancelMovement : MonoBehaviour {

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

	public void cancelMovement () {
        if (gManager.actionCharacter != null)
        {
            gManager.playSound(cancelSound);
            gManager.actionCharacter.SendMessage("cancelMovement");
        }
        else
        {
            Debug.LogError("CancelMovement, cancelMovement: Aucun personnage sélectionné");
            gManager.displayCancelButton = false;
        }
	}
}
