using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Item_BatonDeBouleDeFeu_IHM : ItemIHM {

	public Sprite abilityPicto;
	Item_BatonDeBouleDeFeu associatedBaton;

    public GameObject iconeCible;
    public GameObject fireballFX;
    bool fireballFired = false;

	// Use this for initialization
	void Awake () {
		initialization();
		associatedBaton = GetComponent<Item_BatonDeBouleDeFeu>();
		if (associatedBaton == null) {
			Debug.LogError("Item_BatonDeBouleDeFeu_IHM, Awake: Le script Item_BatonDeBouleDeFeu n'a pas été trouvé sur le meme Game Object");
			this.enabled = false;
		}
	}
	
	// Update is called once per frame
	void Update () {
		// Gérer l'apparence des tokens et la mise en place du plateau
        placementDesTokens();
        // Si une cible a été sélectionnée, la détruire puis défausser l'objet
        if (associatedBaton.targetAcquired && gManager.targetCharacter != null && !fireballFired) fireball(gManager.targetCharacter.GetComponent<CharacterBehavior>());
	}
	
	void OnGUI () {
		// Si l'objet est tenu par un personnage et qu'aucune cible n'a encore été choisie
		if (!associatedBaton.targetAcquired && associatedBaton.tokenHolder != null) {
			// Si on peut afficher la GUI de l'objet, et que l'objet est tenu par un Magicien
			if (associatedBaton.tokenHolder.GetComponent<CB_Magicien>() != null) {
				if (canDisplayItemUseGUI()) {
                    // On vérifie si des cibles sont disponibles
                    if (associatedBaton.ciblesBouleDeFeu.Count == 0) associatedBaton.fireballGetTargets();
					// S'il existe des cibles, on affiche le bouton permettant d'utiliser la boule de feu
					if (associatedBaton.ciblesBouleDeFeu.Count > 0) {
						gManager.actionWheel.activateOneButtonIfNeeded(ActionType.FIREBALL, abilityPicto, () => {readyFireballGUI();} );
					}
				}
			}
		}
	}

	// Afficher les icones sur les cibles
	public void readyFireballGUI() {
		associatedBaton.clearDeplacementHUD();
		GameObject temp;
		associatedBaton.iconHolder = new GameObject("(Dynamic) Cibles baton de boule de feu");
		foreach(GameObject go in associatedBaton.ciblesBouleDeFeu) {
			temp = (GameObject) Instantiate(iconeCible, go.transform.position, iconeCible.transform.rotation);
            if (go.GetComponent<Token>().tokenHolder != null) temp.transform.position = go.GetComponent<Token>().tokenHolder.transform.position;
			temp.transform.parent = associatedBaton.iconHolder.transform;
		}
        associatedBaton.associatedFireball = (GameObject)Instantiate(fireballFX, associatedBaton.tokenHolder.transform.position, fireballFX.transform.rotation);
		// Préparer le lancement de la boule de feu
		associatedBaton.readyFireball();

        gManager.actionWheel.resetButtonsActions();
    }

    CharacterBehavior fireballTarget;

    public void fireball(CharacterBehavior cible)
    {
        gManager.onlineGameInterface.RecordAction(ActionType.FIREBALL, associatedBaton.tokenHolder, cible);

        fireballFired = true;
        gManager.playSound(itemUseSound);
        associatedBaton.associatedFireball.GetComponent<FireballAnimation>().target = cible.transform;
        associatedBaton.associatedFireball.GetComponent<FireballAnimation>().fire();
        fireballTarget = cible;
        Invoke("fireballConsequences", 1.2f);
        associatedBaton.tokenHolder.GetComponent<CharacterBehaviorIHM>().endDeplacementIHMOnly(); // the corresponding endDeplacement will be called in fireballConsequences / endfireballaction
    }

    void fireballConsequences()
    {
        Debug.Assert(gManager.actionCharacter == associatedItem.tokenHolder.gameObject, "Action Character is not set");
        fireballTarget.GetComponent<CharacterBehaviorIHM>().killCharacterIHM();
        fireballFired = false;
        ((Item_BatonDeBouleDeFeu)associatedItem).endfireballaction();
        destroyItem();
        gManager.onlineGameInterface.EndReplayAction();
    }

}
