using UnityEngine;
using System.Collections;

public class CombatBackground : MonoBehaviour {

    ScreenShake screen;
    GameObject vs;
    Transform leftPanel, rightPanel, leftTotal, rightTotal;
    Vector3 leftOpenPosition, rightOpenPosition;
    Vector3 leftClosedPosition, rightClosedPosition;
    Vector3 leftTotalSpace, rightTotalSpace;

	// Use this for initialization
	void Start () {
        leftPanel = transform.GetChild(0);
        rightPanel = transform.GetChild(1);

        leftOpenPosition = leftPanel.position;
        rightOpenPosition = rightPanel.position;
        leftClosedPosition = leftPanel.position + new Vector3(-Screen.width * 0.6f, 0, 0);
        rightClosedPosition = rightPanel.position + new Vector3(Screen.width * 0.6f, 0, 0);

        leftPanel.position = leftClosedPosition;
        rightPanel.position = rightClosedPosition;

        vs = transform.GetChild(2).gameObject;
        leftTotal = vs.transform.GetChild(0).GetChild(0);
        leftTotalSpace = leftTotal.position;
        rightTotal = vs.transform.GetChild(1).GetChild(0);
        rightTotalSpace = rightTotal.position;

        leftTotal.position = rightTotalSpace;
        rightTotal.position = leftTotalSpace;

        vs.SetActive(false);

        screen = transform.parent.GetComponent<ScreenShake>();
	}

    public void closePanelsForCombat()
    {
        StartCoroutine(closePanelsForCombatCoroutine(Time.time, 0.8f));
    }

    IEnumerator closePanelsForCombatCoroutine(float startTime, float duration)
    {
        float valueProgression = (Time.time - startTime) / duration;
        leftPanel.position = Vector3.Lerp(leftClosedPosition, leftOpenPosition, valueProgression);
        rightPanel.position = Vector3.Lerp(rightClosedPosition, rightOpenPosition, valueProgression);
        yield return new WaitForSeconds(0.01f);
        if (Time.time - startTime < duration) StartCoroutine(closePanelsForCombatCoroutine(startTime, duration));
        else
        {
            leftPanel.position = leftOpenPosition;
            rightPanel.position = rightOpenPosition;
            Invoke("launchShake", 0.1f);
            Invoke("revealTotalSpaces", 0.5f);
        }
    }

    void launchShake()
    {
        screen.launchShake(1.0f);
    }

    void revealTotalSpaces()
    {
        vs.SetActive(true);
        StartCoroutine(switchTotalSpacesCoroutine(Time.time, 0.3f, leftTotalSpace, rightTotalSpace));
    }

    IEnumerator hideTotalSpaces()
    {
        StartCoroutine(switchTotalSpacesCoroutine(Time.time, 0.3f, rightTotalSpace, leftTotalSpace));
        yield return new WaitForSeconds(0.31f);
        vs.SetActive(false);
    }

    IEnumerator switchTotalSpacesCoroutine(float startTime, float duration, Vector3 left, Vector3 right)
    {
        float valueProgression = (Time.time - startTime) / duration;
        leftTotal.position = Vector3.Lerp(right, left, valueProgression);
        rightTotal.position = Vector3.Lerp(left, right, valueProgression);
        yield return new WaitForSeconds(0.01f);
        if (Time.time - startTime < duration) StartCoroutine(switchTotalSpacesCoroutine(startTime, duration, left, right));
        else
        {
            leftTotal.position = left;
            rightTotal.position = right;
        }
    }

    public void closeCombatBackground()
    {
        float animationDuration = 0.2f;
        StartCoroutine(switchTotalSpacesCoroutine(Time.time, animationDuration, rightTotalSpace, leftTotalSpace));
        Invoke("reOpenPanels", animationDuration + 0.1f);
    }

    void reOpenPanels()
    {
        vs.SetActive(false);
        StartCoroutine(reOpenPanelsCoroutine(Time.time, 0.8f));
    }

    IEnumerator reOpenPanelsCoroutine(float startTime, float duration)
    {
        float valueProgression = (Time.time - startTime) / duration;
        leftPanel.position = Vector3.Lerp(leftOpenPosition, leftClosedPosition, valueProgression);
        rightPanel.position = Vector3.Lerp(rightOpenPosition, rightClosedPosition, valueProgression);
        yield return new WaitForSeconds(0.01f);
        if (Time.time - startTime < duration) StartCoroutine(reOpenPanelsCoroutine(startTime, duration));
        else
        {
            leftPanel.position = leftClosedPosition;
            rightPanel.position = rightClosedPosition;
            transform.parent.gameObject.SetActive(false);
        }
    }
}
