using UnityEngine;
using System.Collections;

public class CardsPanel : MonoBehaviour {

	public GameObject buttonOpen;
	public GameObject buttonClose;
    public float closedPosCoeff = 1.63f;
    public AudioClip[] openSoundFeedback;
    public AudioClip[] closeSoundFeedback;
    public int indexPlayer;
    public GameObject oppositePanel;

    GameManager gManager;
    GameObject associatedPlayer;
	Vector3 openPosition;
    Vector3 closedPosition;
    float screenWidth;
    public Transform refOpenPos;

	/*
	float OPENING_DURATION = 0.5f;
	float CLOSING_DURATION = 0.3f;
	float scaleChangeStartTime;
	*/

	// Use this for initialization
	void Start () {
        /*
		openPosition = transform.position;
		closedPosition = new Vector3(openPosition.x*closedPosCoeff, openPosition.y, openPosition.z);
		transform.position = closedPosition;
         */
        gManager = GameManager.gManager;
        if (indexPlayer >= 0 && indexPlayer <= 1) associatedPlayer = gManager.players[indexPlayer];

        closedPosition = transform.position;
        openPosition = new Vector3(closedPosition.x * closedPosCoeff, closedPosition.y, closedPosition.z);
        screenWidth = Screen.width;

        //if (!gManager.app.standardRatio)
        {
            //transform.localScale = new Vector3(0.75f, 0.75f, 1);
        }
        if (refOpenPos != null)
            openPosition = refOpenPos.position;
	}

	public void openPanel () {
        StartCoroutine(changePanelPositionCoroutine(openPosition, 1.0f));
        if (openSoundFeedback.GetLength(0) > 0) gManager.playSound(openSoundFeedback[UnityEngine.Random.Range(0, openSoundFeedback.Length)]);
        //else Debug.LogError("CardsPanel, openPanel: Aucun son n'a été prévu");
        buttonOpen.SetActive(false);
        buttonClose.SetActive(true);
        oppositePanel.SendMessage("closePanel");
	}

    public void closePanel() {
        if (transform.position != closedPosition)
        {
            StartCoroutine(changePanelPositionCoroutine(closedPosition, 1.5f));
            if (closeSoundFeedback.GetLength(0) > 0) gManager.playSound(closeSoundFeedback[UnityEngine.Random.Range(0, closeSoundFeedback.Length)]);
            //else Debug.LogError("CardsPanel, closePanel: Aucun son n'a été prévu");
            buttonOpen.SetActive(true);
            buttonClose.SetActive(false);
        }
	}

    IEnumerator changePanelPositionCoroutine(Vector3 finalPosition, float speed)
    {
        yield return new WaitForSeconds(0.001f);
        transform.position = Vector3.MoveTowards(transform.position, finalPosition, speed * screenWidth * Time.deltaTime);
        if (Vector3.Distance(transform.position, finalPosition) > 1) StartCoroutine(changePanelPositionCoroutine(finalPosition, speed));
        else transform.position = finalPosition;
    }

    bool canOpenPanel()
    {
        return gManager.onlineGame || gManager.isActivePlayer(associatedPlayer);
    }

}
