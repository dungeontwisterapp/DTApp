using UnityEngine;
using System.Collections.Generic;

namespace Actions
{
    public class ActionDoor : Action
    {
        public int x1;
        public int y1;
        public int x2;
        public int y2;
        public ActionDoor(ActionType action, int x1, int y1, int x2, int y2)
        {
            type = Type.DOOR;
            this.action = action;
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
        }
    }
}

