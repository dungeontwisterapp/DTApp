using UnityEngine;
using System.Collections;

public class InfoCharacterPanel : MonoBehaviour {

    public GameObject btnOpenPanel;
    public Transform previewPositionRef;
    Vector3 openPosition;
    Vector3 previewPosition;
    float screenHeight;

    public bool open = false;
    public bool moving = false;

    void Start()
    {
        openPosition = transform.position;
        previewPosition = previewPositionRef.position;
        transform.position = previewPosition;
        screenHeight = Screen.height;
    }

    public void panelToOpenPosition()
    {
        if (!moving)
        {
            moving = true;
            StartCoroutine(changePanelPositionCoroutine(openPosition, 2.5f));
        }
    }

    public void panelToPreviewPosition()
    {
        if (!moving)
        {
            moving = true;
            btnOpenPanel.SetActive(true);
            StartCoroutine(changePanelPositionCoroutine(previewPosition, 3.5f));
        }
    }

    IEnumerator changePanelPositionCoroutine(Vector3 finalPosition, float speed)
    {
        yield return new WaitForSeconds(0.001f);
        transform.position = Vector3.MoveTowards(transform.position, finalPosition, speed * screenHeight * Time.deltaTime);
        if (Vector3.Distance(transform.position, finalPosition) > 1) StartCoroutine(changePanelPositionCoroutine(finalPosition, speed));
        else
        {
            transform.position = finalPosition;
            if (finalPosition == openPosition)
            {
                btnOpenPanel.SetActive(false);
                open = true;
            }
            else open = false;
            moving = false;
        }
    }
}
