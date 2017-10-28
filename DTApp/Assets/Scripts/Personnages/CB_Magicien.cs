using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CB_Magicien : CharacterBehavior {

    public override bool canCrossCell(CaseBehavior currentCase)
    {
		return true;
	}

    public override bool canStopOnCell(CaseBehavior currentCase)
    {
        return true;
    }

    public override bool canStayOnCell(CaseBehavior currentCase)
    {
        if (surLaRegletteAdverse(currentCase)) return true;
        else return base.canStayOnCell(currentCase);
    }

}
