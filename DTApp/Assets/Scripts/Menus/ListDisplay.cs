using UnityEngine;
using System.Collections;

public class ListDisplay : MonoBehaviour {

    float timer;

    bool listCollapsed = false;
    bool listHidden = true;
    Vector3 standardPosition, closedPosition, hiddenPosition;
    //Vector3 parentPositionDistance;

	// Use this for initialization
	void Start () {
        //parentPositionDistance = transform.position - transform.parent.position;
        standardPosition = transform.position;
        closedPosition = new Vector3(standardPosition.x, -(float)Screen.height * 0.07f, standardPosition.z);
        hiddenPosition = new Vector3(standardPosition.x, -(float)Screen.height * 0.11f, standardPosition.z);
        transform.position = hiddenPosition;
	}

    public void hideList()
    {
        listHidden = true;
        timer = Time.time;
        Vector3 fromPosition = standardPosition;
        if (listCollapsed) fromPosition = closedPosition;
        listCollapsed = false;
        StartCoroutine(hideListCoroutine(0.2f, fromPosition));
    }

    public void displayList()
    {
        listHidden = false;
        StartCoroutine(displayList(0.4f));
    }

    IEnumerator displayList(float time)
    {
        if (!listHidden && !listCollapsed)
        {
            yield return new WaitForSeconds(time);
            timer = Time.time;
            StartCoroutine(showList(0.2f, hiddenPosition));
        }
    }

    public void collapseOrDiplayList()
    {
        //standardPosition = transform.parent.position + parentPositionDistance;
        //closedPosition = new Vector3(standardPosition.x, -(float)Screen.height * 0.07f, standardPosition.z);
        if (listCollapsed)
        {
            listCollapsed = false;
            timer = Time.time;
            StartCoroutine(showList(0.2f, closedPosition));
        }
        else
        {
            listCollapsed = true;
            timer = Time.time;
            StartCoroutine(collapseList(0.2f));
        }
    }

    IEnumerator collapseList(float animDuration)
    {
        if (!listHidden && listCollapsed)
        {
            float valueProgression = (Time.time - timer) / animDuration;
            transform.position = Vector3.Lerp(standardPosition, closedPosition, valueProgression);
            yield return new WaitForSeconds(0.01f);
            if (Time.time - timer < animDuration) StartCoroutine(collapseList(animDuration));
        }
    }

    IEnumerator hideListCoroutine(float animDuration, Vector3 fromPosition)
    {
        if (listHidden)
        {
            float valueProgression = (Time.time - timer) / animDuration;
            transform.position = Vector3.Lerp(fromPosition, hiddenPosition, valueProgression);
            yield return new WaitForSeconds(0.01f);
            if (Time.time - timer < animDuration) StartCoroutine(hideListCoroutine(animDuration, fromPosition));
        }
    }

    IEnumerator showList(float animDuration, Vector3 fromPosition)
    {
        if (!listHidden && !listCollapsed)
        {
            float valueProgression = (Time.time - timer) / animDuration;
            transform.position = Vector3.Lerp(fromPosition, standardPosition, valueProgression);
            yield return new WaitForSeconds(0.01f);
            if (Time.time - timer < animDuration) StartCoroutine(showList(animDuration, fromPosition));
        }
    }


}
