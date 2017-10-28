using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ExchangePointUI : MonoBehaviour {

	public GameObject occupyingToken;
    Image image;

    void Start()
    {
        image = GetComponent<Image>();
    }

    public void displayExchangePoint(bool isToDisplay)
    {
        image.enabled = isToDisplay;
    }

    public bool isHoldingToken()
    {
        return (occupyingToken != null);
    }

    public bool isCarriedTokenSpot()
    {
        return name.Contains("Carried");
    }


}
