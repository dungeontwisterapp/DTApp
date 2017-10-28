using UnityEngine;
using System.Collections;

public class SmallMenuAnimation : MonoBehaviour {

    Vector3 hiddenPosition, visiblePosition;
    public float openAnimDuration = 0.3f, closeAnimDuration = 0.2f;
    public bool comingFromUp = true;
    public bool automaticClose = false;
    Hashtable openMenuParameters = new Hashtable();
    Hashtable closeMenuParameters = new Hashtable();

    bool setupDone = false;

	// Use this for initialization
	void Start () {
        setup();
	}

    protected void setup()
    {
        if (!setupDone)
        {
            visiblePosition = transform.position;
            hiddenPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            if (comingFromUp) hiddenPosition.y += Screen.height;
            else hiddenPosition.y -= Screen.height;
            transform.position = hiddenPosition;

            openMenuParameters.Add("position", visiblePosition);
            openMenuParameters.Add("time", openAnimDuration);
            openMenuParameters.Add("easetype", iTween.EaseType.easeOutCubic);

            closeMenuParameters.Add("position", hiddenPosition);
            closeMenuParameters.Add("time", closeAnimDuration);
            closeMenuParameters.Add("easetype", iTween.EaseType.easeInCubic);
            if (automaticClose) openMenuParameters.Add("oncomplete", "waitBeforeClosing");
            setupDone = true;
        }
    }

    public void openMenu()
    {
        if (!openMenuParameters.Contains("position")) setup();
        openMenuParameters.Remove("time");
        openMenuParameters.Add("time", openAnimDuration);
        iTween.MoveTo(gameObject, openMenuParameters);
    }

    public void closeMenu()
    {
        closeMenuParameters.Remove("time");
        closeMenuParameters.Add("time", closeAnimDuration);
        iTween.MoveTo(gameObject, closeMenuParameters);
    }

    void waitBeforeClosing()
    {
        Invoke("closeMenu", 2.0f);
    }
}
