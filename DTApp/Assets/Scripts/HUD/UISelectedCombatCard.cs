using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UISelectedCombatCard : MonoBehaviour {

    private Sprite cardVerso;
    private Sprite cardRecto;
    private bool verso;

	// Use this for initialization
	void Start () {
        verso = true;
        cardVerso = GetComponent<Image>().sprite;
	}

    public void setSelectedCardSprite(Sprite selectedCardSprite)
    {
        cardRecto = selectedCardSprite;
    }

    public void swapRectoVerso() {
        verso = !verso;
        GetComponent<Image>().sprite = (verso) ? cardVerso  : cardRecto;
    }


    public void restoreCardVerso()
    {
        if (!verso) swapRectoVerso();
        GetComponent<Image>().enabled = false;
    }

}
