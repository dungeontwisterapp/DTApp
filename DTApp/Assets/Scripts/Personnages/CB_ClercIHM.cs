using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CB_ClercIHM : CharacterBehaviorIHM {

    public Sprite abilityPicto;
	CB_Clerc associatedCleric;

	public GameObject iconeCibleSoin;
	
	void Awake () {
		initialization();
		associatedCleric = GetComponent<CB_Clerc>();
		if (associatedCleric == null) {
			Debug.LogError("CB_ClercIHM, Awake: Le script CB_Clerc n'a pas été trouvé sur le même Game Object");
			this.enabled = false;
		}
	}
	
	void OnGUI () {
		if (canDisplayCharacterGUI()) {
			// Si un personnage blessé est adjacent et qu'une cible pour le soin n'a pas encore été désignée
			if (!associatedCleric.targetAcquired && associatedCleric.personnagesSoignables.Count > 0) {
				gManager.actionWheel.activateOneButtonIfNeeded(ActionType.HEAL, abilityPicto, () => {healCharacter();} );
			}
		}
	}

    public void healCharacter()
    {
        gManager.playSound(abilitySound);
        
        if (associatedCleric.personnagesSoignables.Count == 1)
        {
            // only one target: heal it
            associatedCleric.heal(associatedCleric.personnagesSoignables[0]);
        }
        else
        {
            associatedCleric.clearDeplacementHUD();
            gManager.actionWheel.resetButtonsActions();

            associatedCleric.targetAcquired = true;
            gManager.usingSpecialAbility = true;

            associatedCleric.iconHolder = new GameObject("(Dynamic) Cibles pour Soin");
            foreach (GameObject go in associatedCleric.personnagesSoignables)
            {
                GameObject targetIcon = (GameObject)Instantiate(iconeCibleSoin, go.transform.position, iconeCibleSoin.transform.rotation);
                targetIcon.transform.parent = associatedCleric.iconHolder.transform;
            }
            associatedCleric.iconHolder.transform.Translate(0, 0, -3);

            // heal() will be called during Update once a target is selected
        }
    }

}
