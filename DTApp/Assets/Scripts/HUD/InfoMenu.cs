using UnityEngine;
using System.Collections;

public class InfoMenu : MonoBehaviour {

    InfoCharacterPanel characterPanel;
    Vector3 displayPosition;
    Vector3 hiddenPosition;
    bool moving = false;

	// Use this for initialization
    void Start()
    {
        characterPanel = transform.Find("Informations").GetComponent<InfoCharacterPanel>();
        displayPosition = transform.position;
        hiddenPosition = displayPosition + Vector3.down * (float)Screen.height / 10.0f;
        
        transform.position = hiddenPosition;
	}

    public void panelToDisplayPosition()
    {
        if (!moving)
        {
            moving = true;
            StartCoroutine(changePanelPositionCoroutine(displayPosition, 600));
        }
    }

    public void panelToHiddenPosition()
    {
        if (!moving)
        {
            if (characterPanel.open)
            {
                characterPanel.panelToPreviewPosition();
                return;
            }
            moving = true;
            StartCoroutine(changePanelPositionCoroutine(hiddenPosition, 800));
        }
    }

    IEnumerator changePanelPositionCoroutine(Vector3 finalPosition, float speed)
    {
        yield return new WaitForSeconds(0.001f);
        transform.position = Vector3.MoveTowards(transform.position, finalPosition, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, finalPosition) > 1) StartCoroutine(changePanelPositionCoroutine(finalPosition, speed));
        else
        {
            transform.position = finalPosition;
            moving = false;
        }
    }
}
