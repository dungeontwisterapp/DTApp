using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CB_VoleuseIHM : CharacterBehaviorIHM {

    public Sprite abilityPicto;
    public AudioClip sonFermerHerse;
    public AudioClip sonOuvrirHerse;
	
	void OnGUI () {
        if (canDisplayCharacterGUI())
        {
            List<CaseBehavior> hersesAdjacentes = new List<CaseBehavior>();
            if (associatedCharacter.pathfinder != null)
            {
                hersesAdjacentes.AddRange(associatedCharacter.pathfinder.GetActivatedCells(ActionType.CLOSEDOOR));
                hersesAdjacentes.AddRange(associatedCharacter.pathfinder.GetActivatedCells(ActionType.OPENDOOR));
            }

			// Si une herse est à proximité
            if (hersesAdjacentes.Count > 0)
            {
                gManager.actionWheel.activateOneButtonIfNeeded(ActionType.OPENDOOR, abilityPicto, () => {changerEtatHerse();} );
			}
		}
	}
	
	public void changerEtatHerse () {
        List<CaseBehavior> hersesAdjacentes = new List<CaseBehavior>();
        hersesAdjacentes.AddRange(associatedCharacter.pathfinder.GetActivatedCells(ActionType.CLOSEDOOR));
        hersesAdjacentes.AddRange(associatedCharacter.pathfinder.GetActivatedCells(ActionType.OPENDOOR));
        CaseBehavior target = hersesAdjacentes[0];

        AudioClip sound = target.herse.GetComponent<HerseBehavior>().herseOuverte ? sonFermerHerse : sonOuvrirHerse;
        ActionType type = target.herse.GetComponent<HerseBehavior>().herseOuverte ? ActionType.CLOSEDOOR : ActionType.OPENDOOR;

        gManager.playSound(sound);
        target.herse.GetComponent<HerseBehaviorIHM>().manipulate(type);
        gManager.onlineGameInterface.RecordAction(type, associatedCharacter);
        endDeplacementIHM();
    }

}
