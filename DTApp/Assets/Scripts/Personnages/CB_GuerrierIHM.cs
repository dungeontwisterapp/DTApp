using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class CB_GuerrierIHM : CharacterBehaviorIHM {

    public Sprite abilityPicto;

	void Awake () {
		initialization();
	}

	void OnGUI () {
        if (canDisplayCharacterGUI())
        {
            if (associatedCharacter.pathfinder != null)
            {
                List<CaseBehavior> hersesAdjacentes = associatedCharacter.pathfinder.GetActivatedCells(ActionType.DESTROYDOOR);
                // Si une herse est à proximité
                if (hersesAdjacentes.Count > 0)
                {
                    gManager.actionWheel.activateOneButtonIfNeeded(ActionType.DESTROYDOOR, abilityPicto, () => { briserHerse(); });
                }
            }
		}
	}

    public void briserHerse()
    {
        List<CaseBehavior> hersesAdjacentes = associatedCharacter.pathfinder.GetActivatedCells(ActionType.DESTROYDOOR);
        CaseBehavior target = hersesAdjacentes[0];

        ActionType type = ActionType.DESTROYDOOR;

        gManager.playSound(abilitySound);
        target.herse.GetComponent<HerseBehaviorIHM>().manipulate(type);
        gManager.onlineGameInterface.RecordAction(type, associatedCharacter);
        endDeplacementIHM();
    }

}
