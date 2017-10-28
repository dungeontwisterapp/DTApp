using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class ItemIHM : TokenIHM {

    protected Item associatedItem;
    public AudioClip itemUseSound;

	// Use this for initialization
	void Awake () {
		initialization();
	}
	
	public override void initialization () {
		base.initialization();
		associatedItem = GetComponent<Item>();
		if (associatedItem == null) {
			Debug.LogError("ItemIHM, Awake: Le script Token n'a pas été trouvé sur le meme Game Object");
			this.enabled = false;
		}
	}
	
	// Update is called once per frame
    void Update()
    {
        if (!gManager.freezeDisplay)
        {
            // Gérer l'apparence des tokens et la mise en place du plateau
            placementDesTokens();
            if (associatedItem.tokenPlace) tokenHighlight.SetActive(false);
        }
	}

	// Renvoie TRUE si la situation permet d'utiliser un objet, FALSE sinon
	public bool canDisplayItemUseGUI () {
        if (associatedItem.tokenHolder == null) { Debug.LogError("wrong"); return false; }
		return (gManager.canDisplayTokenGUI() && 
            associatedItem.tokenHolder.gameObject == gManager.actionCharacter &&
            !associatedItem.tokenHolder.wounded && !associatedItem.tokenHolder.freshlyHealed &&
            !gManager.deplacementEnCours);
	}
	
	// Retire l'objet du plateau
	public void destroyItem () {
		associatedItem.destroyItem();
		GetComponent<Collider>().enabled = false;
		fadeOutToken();
	}
}
