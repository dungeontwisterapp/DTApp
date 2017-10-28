using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class GlossarySwipeControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {

    DisplayGlossaryInfo glossaryInfo;
    GlossaryAnimation glossaryState;

    float minimumSwipeWidth = 9999;
    bool touchReceived = false;

	// Use this for initialization
    void Start()
    {
        glossaryInfo = transform.parent.GetComponent<DisplayGlossaryInfo>();
        glossaryState = transform.parent.GetComponent<GlossaryAnimation>();
        minimumSwipeWidth = Screen.width / 50.0f;
        //if (Screen.dpi != 0) minimumSwipeWidth = 0.25f *  (Screen.width / Screen.dpi);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        touchReceived = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        touchReceived = false;
    }
	
	// Update is called once per frame
    void Update()
    {
        if (!glossaryState.hidden && glossaryState.isInPlace() && touchReceived)
        {
            if (Input.touchCount == 1)
            {
                if (Input.GetTouch(0).deltaPosition.x > 0.5f && Input.GetTouch(0).deltaPosition.magnitude > minimumSwipeWidth)
                {
                    if (glossaryInfo.currentIndex > 0) glossaryState.contentBar.GetChild(glossaryInfo.currentIndex - 1).SendMessage("requestAnotherInfo");
                }
                else if (Input.GetTouch(0).deltaPosition.x < 0.5f && Input.GetTouch(0).deltaPosition.magnitude > minimumSwipeWidth)
                {
                    if (glossaryInfo.currentIndex < glossaryState.contentBar.childCount) glossaryState.contentBar.GetChild(glossaryInfo.currentIndex + 1).SendMessage("requestAnotherInfo");
                }
            }
        }
	}
}
