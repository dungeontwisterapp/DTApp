using UnityEngine;
using System.Collections;

public class SpecialHighlight : MonoBehaviour {

    Token associatedToken;
    SpriteRenderer sprite;
    GameManager gManager;

    bool displaySign;

	// Use this for initialization
	void Awake () {
        sprite = GetComponent<SpriteRenderer>();
        associatedToken = transform.parent.parent.GetComponent<Token>();
        gManager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameManager>();
	}
	
	// Update is called once per frame
    void Update()
    {
        if (gManager.gameMacroState != GameProgression.GameOver)
        {
            if (associatedToken.isTokenOnBoard() && !associatedToken.GetComponent<TokenIHM>().tokenHidden)
            {
                displaySign = !associatedToken.selected;
                if (associatedToken.deplacementRestant == 0) displaySign = true;
            }
            else displaySign = false;
        }
        else displaySign = false;
        sprite.enabled = displaySign;
	}
}
