using UnityEngine;
using System.Collections;

public class HerseBehaviorIHM : MonoBehaviour {

	float alpha = 0;
	
	public Sprite spriteHerseOuverte;
	public Sprite spriteHerseBrise;
	
	// Use this for initialization
	void Start () {
        HerseBehavior hB = GetComponent<HerseBehavior>();
        if (!hB.herseOuverte) GetComponent<Renderer>().material.color = new Color(1, 1, 1, alpha);
	}
    
    public void manipulate(ActionType action)
    {
        HerseBehavior herse = GetComponent<HerseBehavior>();
        switch (action)
        {
            case ActionType.CLOSEDOOR:
                herse.fermerHerse();
                fermerHerse();
                break;
            case ActionType.OPENDOOR:
                herse.ouvrirHerse();
                ouvrirHerse();
                break;
            case ActionType.DESTROYDOOR:
                herse.briserHerse();
                briserHerse();
                break;
            default:
                Debug.Assert(false);
                break;
        }
    }

    // La herse passe à l'état Ouvert et son Sprite apparait
    public void ouvrirHerse () {
		GetComponent<SpriteRenderer>().sprite = spriteHerseOuverte;
		StartCoroutine(fadeIn(0.02f));
	}
	
	// La herse passe à l'état Fermée et son Sprite disparait
	public void fermerHerse () {
		StartCoroutine(fadeOut(0.02f));
	}
	
	// La herse passe à l'état Brisée et son Sprite apparait
	public void briserHerse () {
		GetComponent<SpriteRenderer>().sprite = spriteHerseBrise;
		StartCoroutine(fadeIn(0.02f));
	}
	
	// Fait apparaitre le Sprite progressivement
	IEnumerator fadeIn (float intervals) {
		alpha += 0.1f;
		GetComponent<Renderer>().material.color = new Color(1, 1, 1, alpha);
		yield return new WaitForSeconds(intervals);
		if (alpha < 1) StartCoroutine(fadeIn(intervals));
	}
	
	// Fait disparaitre le Sprite progressivement
	IEnumerator fadeOut (float intervals) {
		alpha -= 0.1f;
		GetComponent<Renderer>().material.color = new Color(1, 1, 1, alpha);
		yield return new WaitForSeconds(intervals);
		if (alpha > 0) StartCoroutine(fadeOut(intervals));
	}
}
