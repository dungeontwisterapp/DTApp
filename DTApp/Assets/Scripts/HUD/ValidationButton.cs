using UnityEngine;
using System.Collections;

public class ValidationButton : MonoBehaviour {

    GameManager gManager;
    public bool validated = false;
    public AudioClip[] validationSoundFeedback;

    void Start()
    {
        gManager = GameManager.gManager;
    }

	public void validate () {
        if (validationSoundFeedback.GetLength(0) > 0) gManager.playSound(validationSoundFeedback[UnityEngine.Random.Range(0, validationSoundFeedback.Length)]);
        else Debug.LogError("ValidationButton, validate: Aucun son n'a été prévu pour la validation");
		validated = true;
	}

	public void hideButton () {
		validated = false;
		gameObject.SetActive(false);
	}
	

}
