using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class OnlinePlayer : PlayerBehavior {

    public override bool isOnlinePlayer()
    {
        return true;
    }
}
