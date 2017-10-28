using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PathFinding {

    public static int MAX_PRIORITY = 1000;
    

    // implementation
    private struct ActionDetail {
        public int priority;
        public CaseBehavior previousCell;
        public ActionDetail(int prio, CaseBehavior prev)
        {
            priority = prio;
            previousCell = prev;
        }
    }

    private Dictionary<ActionType, Dictionary<CaseBehavior, ActionDetail>> possibleActions;

    private bool RegisterAction(ActionType type, CaseBehavior cell, int priority, CaseBehavior previous = null) {
        if (priority < GetActionPriority(type, cell))
        {
            if (!possibleActions.ContainsKey(type))
            {
                possibleActions[type] = new Dictionary<CaseBehavior, ActionDetail>();
            }
            possibleActions[type][cell] = new ActionDetail(priority, previous);
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool HasAction(ActionType type, CaseBehavior cell)
    {
        return GetActionPriority(type, cell) < MAX_PRIORITY;
    }

    public int GetActionPriority(ActionType type, CaseBehavior cell)
    {
        int priority = MAX_PRIORITY;
        if (possibleActions.ContainsKey(type))
        {
            var actions = possibleActions[type];
            if (actions.ContainsKey(cell))
            {
                priority = actions[cell].priority;
                Debug.Assert(priority < MAX_PRIORITY);
            }
        }
        return priority;
    }

    public List<CaseBehavior> GetActionPath(ActionType type, CaseBehavior cell, bool includeStart = false)
    {
        // WARNING: no check for infinite loop here. We assume that the pathfinding was correct in the first place.
        List<CaseBehavior> path = new List<CaseBehavior>();
        if (possibleActions.ContainsKey(type))
        {
            var actions = possibleActions[type];
            while (cell != null && actions.ContainsKey(cell))
            {
                if (!includeStart && cell == startCell)
                {
                    cell = null;
                }
                else
                {
                    path.Add(cell);
                    cell = actions[cell].previousCell;
                }
            }
            path.Reverse();
        }
        return path;
    }

    public List<CaseBehavior> GetActivatedCells(ActionType type)
    {
        if (possibleActions.ContainsKey(type))
            return new List<CaseBehavior>(possibleActions[type].Keys);
        else
            return new List<CaseBehavior>();
    }

    private CharacterBehavior currentCharacter;
    private CaseBehavior startCell;
    private GameManager gManager;

    // settings
    public PathFinding(GameManager manager, int row, int column, CharacterBehavior character)
    {
        gManager = manager;
        startCell = gManager.getCase(row, column).GetComponent<CaseBehavior>();
        currentCharacter = character;

        possibleActions = new Dictionary<ActionType, Dictionary<CaseBehavior, ActionDetail>>();
    }

    public void SearchPossibleActions() {
        if (!currentCharacter.freshlyHealed)
        {
            if (!currentCharacter.wounded)
            {
                // special actions and jump are possible only if move has not been started already.
                if (!gManager.deplacementEnCours)
                {
                    SearchLocalActions(startCell);
                    SearchAdjacentActions(startCell);
                }
                SearchPath(startCell, currentCharacter.deplacementRestant, !gManager.deplacementEnCours);
            }
            else SearchLocalActions(startCell);
        }

        Debug.Log(printDictionaryContent());
    }

    // action search
    void SearchLocalActions(CaseBehavior currentCell)
    {
        if (!currentCharacter.wounded)
        {
            // Si la case permet la rotation de la salle, afficher cette possibilité
            if (currentCell.type == CaseBehavior.typeCase.caseRotation)
            {
                RegisterAction(ActionType.ROTATE, currentCell, 0);
            }

            if (currentCharacter.tokenTranporte != null && currentCharacter.tokenTranporte.GetComponent<Item>() != null)
            {
                Item carriedItem = currentCharacter.tokenTranporte.GetComponent<Item>();
                if (carriedItem is Item_BatonDeBouleDeFeu)
                {
                    RegisterAction(ActionType.FIREBALL, currentCell, 0);
                }
                if (carriedItem is Item_PotionDeVitesse)
                {
                    RegisterAction(ActionType.SPEEDPOTION, currentCell, 0);
                }
            }
        }
        else
        {
            if (currentCharacter is CB_Troll)
            {
                RegisterAction(ActionType.REGENERATE, currentCell, 0);
            }
            /*
             * TODO : Mettre potion de vie quand ajouté au jeu
             * 
            if (currentCharacter.tokenTranporte != null && currentCharacter.tokenTranporte.GetComponent<Item>() != null)
            {
                Item carriedItem = currentCharacter.tokenTranporte.GetComponent<Item>();
                if (carriedItem is Item_BatonDeBouleDeFeu)
                {
                    RegisterAction(ActionType.FIREBALL, currentCell, 0);
                }
            }
            */
        }
    }

    void SearchAdjacentActions(CaseBehavior currentCell)
    {
        // Propagate in all directions
        for (int dir = 0; dir < 4; ++dir)
        {
            checkAdjacentCell(currentCell, dir);
        }
    }


    // Vérifie l'ensemble des actions spéciales qui peuvent etre exécutées depuis la case actuelle vers la direction ciblée
    public void checkAdjacentCell(CaseBehavior currentCell, int dir)
    {
        if (!currentCell.debordement(dir))
        {
            CaseBehavior nextCell = currentCell.getCaseVers(dir);

            if (!nextCell.isCaseRevealed())
            {
                // Si une salle non ouverte est adjacente et accessible, en permettre l'ouverture
                if (currentCell.cheminDegage(currentCell.versDirection(dir)))
                {
                    RegisterAction(ActionType.REVEAL, nextCell, 0);
                }
            }
            else
            {
                // Accès à la case pour vérifier que l'on peut s'y déplacer
                if (currentCell.cheminDegage(currentCell.versDirection(dir)) && nextCell.cheminDegage(nextCell.versDirection(CaseBehavior.opposee(dir))))
                {
                    // Regarder si un combat est possible
                    if (nextCell.enemyFighterPresent(currentCharacter.gameObject))
                    {
                        RegisterAction(ActionType.ATTACK, nextCell, 0);
                    }

                    if (currentCharacter is CB_Clerc)
                    {
                        // Regarder si un allie peut etre soigne
                        if (nextCell.woundedCharacterReadyForHealing())
                        {
                            RegisterAction(ActionType.HEAL, nextCell, 0);
                        }
                    }
                }

                // Si une herse non brisee relie cette case
                if (nextCell.herse != null && currentCell.herse == nextCell.herse && !nextCell.herse.GetComponent<HerseBehavior>().herseBrisee)
                {
                    if (currentCharacter is CB_Guerrier)
                    {
                        RegisterAction(ActionType.DESTROYDOOR, nextCell, 0);
                    }
                    else if (currentCharacter is CB_Voleuse)
                    {
                        if (currentCell.herse.GetComponent<HerseBehavior>().herseOuverte) RegisterAction(ActionType.CLOSEDOOR, nextCell, 0);
                        else RegisterAction(ActionType.OPENDOOR, nextCell, 0);
                    }
                }
            }
        }

        if (currentCharacter is CB_PasseMuraille)
        {
            if (!currentCell.debordement(dir))
            {
                // Peut franchir les murs adjacents
                CaseBehavior nextCell = currentCell.getCaseVers(dir);
                if (nextCell.isCaseRevealed())
                {
                    if (currentCell.versDirection(dir) == CaseBehavior.cheminCaseAdjacente.mur ||
                        nextCell.versDirection(CaseBehavior.opposee(dir)) == CaseBehavior.cheminCaseAdjacente.mur
                        )
                    {
                        if (currentCharacter.canStayOnCell(nextCell))
                        {
                            RegisterAction(ActionType.WALLWALK, currentCell, 0);
                            RegisterAction(ActionType.WALLWALK, nextCell, 1, currentCell);
                        }
                    }
                }
            }
        }
    }


    // Recherche A* iterative de deplacement et saut
    void SearchPath(CaseBehavior startCell, int depth, bool jump)
    {
        Queue<CaseBehavior> queue = new Queue<CaseBehavior>();

        RegisterAction(ActionType.WALK, startCell, 0);
        if (jump) RegisterAction(ActionType.JUMP, startCell, 0);
        if (depth > 0) queue.Enqueue(startCell);

        // Proof that this iterative BFS (Broad-First-Search) has no redundancy:
        // If we can only jump through a cell, there will be only one register of this cell.
        // If we can jump and move through the same cell (therefore as first move), then the code will discard the jump.
        // Since the priority increase with depth, no cell will be registered twice for move (due to register priority check)
        // As a conclusion: we guarantee no redundant check.
        while (queue.Count > 0)
        {
            CaseBehavior currentCell = queue.Dequeue();
            int priority = GetActionPriority(ActionType.WALK, currentCell); // 0: init, depth: stop, MAX: JumpStart

            Debug.Assert(!currentCharacter.surLaRegletteAdverse(currentCell));

            // Propagate in all directions
            for (int dir = 0; dir < 4; ++dir)
            {
                if (!currentCell.debordement(dir) && currentCell.cheminDegage(currentCell.versDirection(dir)))
                {
                    CaseBehavior nextCell = currentCell.getCaseVers(dir);
                    if (nextCell.isCaseRevealed() && nextCell.cheminDegage(nextCell.versDirection(CaseBehavior.opposee(dir))) && (currentCharacter is CB_Magicien || !nextCell.isNonWoundedEnemyPresent(currentCharacter.gameObject)))
                    {
                        // Si la case suivante est traversable et le mouvement precedent n'est pas un saut, enregistrer un mouvement.
                        if (currentCharacter.canCrossCell(nextCell) && priority != MAX_PRIORITY)
                        {
                            if (priority + 1 == depth)
                            {
                                // Si ce n'est pas la case finale, on doit pouvoir terminer le mouvement sur cette case.
                                // WARNING: le cas d'un couloir de plusieurs cases contenant plusieur cases non finales n'est pas pris en compte.
                                //          (possibilite d'effectuer un mouvement qui devra etre annule par la suite)
                                if (currentCharacter.canStayOnCell(nextCell))
                                    RegisterAction(ActionType.WALK, nextCell, priority + 1, currentCell);
                            }
                            else
                            {
                                // propage le mouvement si besoin.
                                if (RegisterAction(ActionType.WALK, nextCell, priority + 1, currentCell) && !currentCharacter.surLaRegletteAdverse(nextCell))
                                {
                                    queue.Enqueue(nextCell);
                                }
                            }
                        }
                        // si le saut est permis et que c'est le premier mouvement, initier un saut
                        else if (priority == 0 && jump)
                        {
                            if (RegisterAction(ActionType.JUMP, nextCell, 1, currentCell))
                            {
                                queue.Enqueue(nextCell);
                            }
                        }
                        // sinon si le mouvement precedent etait un saut, enregistrer la fin du saut si possible.
                        else if (priority == MAX_PRIORITY && currentCharacter.canStayOnCell(nextCell))
                        {
                            RegisterAction(ActionType.JUMP, nextCell, 2, currentCell);
                        }
                    }
                }
            }
        }
    }

    // The first fighter will always be the action character, and the second, the target character.
    public List<CharacterBehavior> SearchFighters(CharacterBehavior attacker, CharacterBehavior opponent) {
        List<CharacterBehavior> fighters = new List<CharacterBehavior>();
        Queue<CharacterBehavior> queue = new Queue<CharacterBehavior>();

        System.Action<CharacterBehavior> SelectFighter = (fighter) =>
        {
            if (!fighters.Contains(fighter))
            {
                fighters.Add(fighter);
                queue.Enqueue(fighter);
            }
        };

        SelectFighter(attacker);
        SelectFighter(opponent);

        while (queue.Count > 0)
        {
            CharacterBehavior currentFighter = queue.Dequeue();
            CaseBehavior currentCell = currentFighter.caseActuelle.GetComponent<CaseBehavior>();

            // Propagate in all directions
            for (int dir = 0; dir < 4; ++dir)
            {
                if (!currentCell.debordement(dir) && currentCell.cheminDegage(currentCell.versDirection(dir)))
                {
                    CaseBehavior nextCell = currentCell.getCaseVers(dir);

                    if (nextCell.isCaseRevealed() && nextCell.cheminDegage(nextCell.versDirection(CaseBehavior.opposee(dir))) && nextCell.enemyFighterPresent(currentFighter.gameObject))
                    {
                        if (nextCell.isNonWoundedEnemyPresent(currentFighter.gameObject))
                        {
                            foreach (CharacterBehavior character in nextCell.characters)
                            {
                                SelectFighter(character);
                            }
                        }
                    }
                }
            }
        }

        return fighters;
    }

    // Recherche A* iterative de ligne de vue
    List<CaseBehavior> getLineOfSight(CaseBehavior fromCell)
    {
        List<CaseBehavior> charactersOnSight = new List<CaseBehavior>();
        // Propagate in all directions
        for (int dir = 0; dir < 4; ++dir)
        {
            CaseBehavior currentCell = fromCell;
            bool gotToSightLimit = false;
            while (!gotToSightLimit)
            {
                if (!currentCell.debordement(dir) && currentCell.cheminDegage(currentCell.versDirection(dir)))
                {
                    CaseBehavior nextCell = currentCell.getCaseVers(dir);
                    if (nextCell.isCaseRevealed() && nextCell.cheminDegage(nextCell.versDirection(CaseBehavior.opposee(dir))))
                    {
                        if (nextCell.characters.Count > 0)
                        {
                            charactersOnSight.Add(nextCell);
                            gotToSightLimit = true;
                        }
                        else currentCell = nextCell;
                    }
                    else gotToSightLimit = true;
                }
                else gotToSightLimit = true;
            }
        }
        return charactersOnSight;
    }

    public List<CaseBehavior> enemyCharactersOnSight(CaseBehavior fromCell)
    {
        List<CaseBehavior> enemiesOnSight = new List<CaseBehavior>();
        List<CaseBehavior> charactersOnSight = getLineOfSight(fromCell);
        foreach (CaseBehavior cell in charactersOnSight)
        {
            CharacterBehavior character = cell.getMainCharacter();
            Debug.Assert(character != null);
            if (!gManager.isActivePlayer(character.affiliationJoueur)) enemiesOnSight.Add(cell);
        }
        return enemiesOnSight;
    }

    public string printDictionaryContent()
    {
        string dico = "Actions possibles :\n";
        foreach (KeyValuePair<ActionType, Dictionary<CaseBehavior, ActionDetail>> action in possibleActions)
        {
            if ((action.Key == ActionType.WALK || action.Key == ActionType.JUMP || action.Key == ActionType.WALLWALK) &&
                action.Value.Count == 1)
            {
                break; // only starting celle : no action possible
            }
            dico += "\nAction " + action.Key + "\n";
            foreach (KeyValuePair<CaseBehavior, ActionDetail> detail in action.Value)
            {
                if (action.Key == ActionType.WALK || action.Key == ActionType.JUMP || action.Key == ActionType.WALLWALK)
                {
                    if (detail.Value.previousCell != null)
                    {
                        CaseBehavior previousCell = detail.Value.previousCell;
                        CaseBehavior cell = detail.Key;
                        dico += action.Key + " from (" + previousCell.row + ", " + previousCell.column + ") to (" + cell.row + ", " + cell.column + ") with priority " + detail.Value.priority + "\n";
                    }
                }
                else if (action.Key == ActionType.REVEAL)
                {
                    dico += action.Key + " tile " + detail.Key.transform.parent.name + "\n";
                }
                else if (action.Key == ActionType.ATTACK)
                {
                    dico += action.Key + " character " + detail.Key.getMainCharacter().name + "\n";
                }
                else if (action.Key == ActionType.HEAL)
                {
                    dico += action.Key + " character " + detail.Key.getMainCharacter().name + "\n";
                }
                else if (action.Key == ActionType.FIREBALL)
                {
                    dico += action.Key + " on target character " + detail.Key.getMainCharacter().name + "\n";
                }
                else // default
                {
                    dico += action.Key + " toward cell " + detail.Key.name;
                }
            }
        }
        return dico;
    }
}
