using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace IA
{
    public class ScriptedPlayerBehavior : PlayerBehavior
    {

        public override bool isScriptedPlayer()
        {
            return true;
        }

        // Au tour du joueur
        public override void myTurnToPlay()
        {
            base.myTurnToPlay();
            PremadeBoardSetupParameters board = GameObject.Find("Board").GetComponent<PremadeBoardSetupParameters>();
            CharacterBehavior chara = board.mainOpposingCharacter;
            gManager.selectionEnCours = false;
            gManager.actionPoints = board.opponentObjectives.Count;
            gManager.actionCardsButton.SetActive(false);
            gManager.turnStarted = true;
            if (board.opponentObjectives.Count > 0)
            {
                if (board.opponentObjectives[0].GetComponent<CharacterBehavior>() != null) StartCoroutine(pursueCharacter(chara, board.opponentObjectives, 2));
                else StartCoroutine(playScriptedMoves(chara, board.opponentObjectives));
            }
            else
            {
                Debug.LogError("ScriptedPlayerBehavior, myTurnToPlay: Aucun comportement prévu pour " + chara.name + " pour cette mission");
                gManager.startTurn = false; // Prevents from displaying Draw action cards button
                gManager.selectionEnCours = true; // Prevents automatic player change
                StartCoroutine(changePlayer());
            }
        }

        IEnumerator changePlayer()
        {
            yield return new WaitForSeconds(1.5f);
            gManager.changementTourJoueur(true);
        }

        IEnumerator pursueCharacter(CharacterBehavior chara, List<GameObject> opponentObjectives, int movementsThisTurn)
        {
            yield return new WaitForSeconds(1.5f);
            for (int i = 0; i < movementsThisTurn; i++)
            {
                chara.GetComponent<CharacterBehaviorIHM>().characterSelection();
                yield return new WaitForSeconds(0.75f);
                if (gManager.adversaireAdjacent)
                {
                    yield return new WaitForSeconds(1.5f);
                    gManager.combatManager.combat(opponentObjectives[i]);
                    yield return new WaitForSeconds(1.5f);
                    break;
                }
                else if (opponentObjectives.Count == 1)
                {
                    Debug.Assert(false, "ScriptedPlayerBehavior, pursueCharacter: Créer le chemin vers la cible");
                    gManager.actionPoints = movementsThisTurn + 1;
                    //opponentObjectives.Insert(0, null);
                    StartCoroutine(pursueCharacter(chara, opponentObjectives, movementsThisTurn +1));
                    break;
                }
                // Déplacement
                if (opponentObjectives[i].GetComponent<CaseBehavior>() != null)
                {
                    opponentObjectives[i].GetComponent<CaseBehavior>().cibleAssociated.GetComponent<CibleDeplacementIHM>().moveSelectedTokenToTarget();
                    yield return new WaitForSeconds(1.5f);
                }
            }
            yield return new WaitForSeconds(3);
        }

        IEnumerator playScriptedMoves(CharacterBehavior chara, List<GameObject> opponentObjectives)
        {
            yield return new WaitForSeconds(1.5f);
            int steps = opponentObjectives.Count;
            for (int i = 0; i < steps; i++)
            {
                chara.GetComponent<CharacterBehaviorIHM>().characterSelection();
                yield return new WaitForSeconds(0.75f);
                // Déplacement
                if (opponentObjectives[i].GetComponent<CaseBehavior>() != null)
                {
                    opponentObjectives[i].GetComponent<CaseBehavior>().cibleAssociated.GetComponent<CibleDeplacementIHM>().moveSelectedTokenToTarget();
                    yield return new WaitForSeconds(0.75f);
                }
                // Combat
                if (opponentObjectives[i].GetComponent<CharacterBehavior>() != null)
                {
                    yield return new WaitForSeconds(1.5f);
                    gManager.combatManager.combat(opponentObjectives[i]);
                }
                yield return new WaitForSeconds(1.5f);
            }
            yield return new WaitForSeconds(3);
        }

    }
}