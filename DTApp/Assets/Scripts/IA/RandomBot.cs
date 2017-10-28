using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace IA
{

    public class RandomBot : PlayerBehavior
    {

        public override bool isScriptedPlayer()
        {
            return true;
        }

        public override void myTurnToPlay()
        {
            // TODO
        }

        IEnumerator playScriptedMoves(CharacterBehavior chara, List<GameObject> opponentObjectives)
        {
            yield return new WaitForSeconds(1.5f);
            // TODO
        }

    }

}