using UnityEngine;
using System.Collections;

public class CB_TrollIHM : CharacterBehaviorIHM {

    public Sprite abilityPicto;
    CB_Troll associatedTroll;

    void Awake()
    {
        initialization();
        associatedTroll = GetComponent<CB_Troll>();
        if (associatedTroll == null)
        {
            Debug.LogError("CB_TrollIHM, Awake: Le script CB_Troll n'a pas été trouvé sur le même Game Object");
            this.enabled = false;
        }
    }

    void OnGUI()
    {
        if (canDisplayCharacterGUI())
        {
            // Si le Troll a été blessé, mais pas pendant son tour
            if (associatedTroll.wounded && !associatedTroll.freshlyWounded)
            {
                gManager.actionWheel.activateOneButtonIfNeeded(ActionType.REGENERATE, abilityPicto, () => { regenerate(); });
            }
        }
    }

    public void regenerate()
    {
        gManager.playSound(abilitySound);
        characterHealedIHM();
        gManager.onlineGameInterface.RecordAction(ActionType.REGENERATE, associatedTroll);
        endDeplacementIHM();
    }

}
