using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MenuListButton : MonoBehaviour {

    Image image;

	// Use this for initialization
	void Start () {
        image = GetComponent<Image>();
	}

    public void resetSelection()
    {
        transform.parent.BroadcastMessage("hideSelectionFeedback");
    }

    void hideSelectionFeedback()
    {
        image.fillAmount = 0;
    }
}
