using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CB_Clerc : CharacterBehavior {
	
	public List<GameObject> personnagesSoignables = new List<GameObject>();
	
	public bool targetAcquired = false;
	public GameObject iconHolder;

    void Update()
    {
        if (targetAcquired && gManager.targetCharacter != null && personnagesSoignables.Contains(gManager.targetCharacter))
        {
            heal(gManager.targetCharacter);
        }
    }

    public void heal(GameObject target) {
        Debug.Assert(personnagesSoignables.Contains(target), "invalid target");
        target.GetComponent<CharacterBehaviorIHM>().characterHealedIHM();
        gManager.onlineGameInterface.RecordAction(ActionType.HEAL, this, target.GetComponent<CharacterBehavior>());
        GetComponent<CharacterBehaviorIHM>().endDeplacementIHM();
        gManager.onlineGameInterface.EndReplayAction();
    }

    // Remettre à zéro les variables indiquant les actions possibles
    public override void clearUnresolvedActions () {
		base.clearUnresolvedActions(); 
		if (iconHolder != null) Destroy(iconHolder);
		gManager.usingSpecialAbility = false;
		targetAcquired = false;
		personnagesSoignables.Clear();
	}

}
