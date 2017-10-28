using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CB_Voleuse : CharacterBehavior {

    public override bool canCrossCell(CaseBehavior currentCase)
    {
        return !currentCase.isNonWoundedEnemyPresent(gameObject);
    }

    public override bool canStopOnCell(CaseBehavior currentCase)
    {
        return !currentCase.isNonWoundedEnemyPresent(gameObject);
    }

    public override bool canStayOnCell(CaseBehavior currentCase)
    {
        if (surLaRegletteAdverse(currentCase))
        {
            return !currentCase.isOtherNonWoundedCharacterPresent(gameObject);
        }
        else
        {
            return (!currentCase.isOtherNonWoundedCharacterPresent(gameObject) && !currentCase.isOpponentPresent(gameObject) && !tooManyTokensToStayOnCell(currentCase));
        }
    }

    public override bool endCellIsSafe (CaseBehavior currentCase) {
        return true;
    }

}
