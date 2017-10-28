using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Item_PotionDeVitesse_IHM : ItemIHM {

    public Sprite abilityPicto;

	// Use this for initialization
	void Awake () {
		initialization();
	}
	
	void OnGUI () {
		// Si l'objet est tenu par un personnage
		if (associatedItem.tokenHolder != null) {
			// Si le personnage est sélectionné et n'a pas encore effectué d'action
			if (canDisplayItemUseGUI()) {
				gManager.actionWheel.activateOneButtonIfNeeded(ActionType.SPEEDPOTION, abilityPicto, () => {speedPotion();} );
			}
		}
	}

    public void speedPotion()
    {
        gManager.onlineGameInterface.RecordAction(ActionType.SPEEDPOTION, associatedItem.tokenHolder);

        gManager.playSound(itemUseSound);
        associatedItem.tokenHolder.GetComponent<CharacterBehaviorIHM>().endDeplacementIHM();
        ((Item_PotionDeVitesse)associatedItem).applySpeedPotion();
        destroyItem();
        gManager.onlineGameInterface.EndReplayAction();
    }

}
