using UnityEngine;
using System.Collections;

namespace Actions
{
    public class ActionSimpleNote : Action
    {
        public const int UNKNOWN = -1;
        public string message;
        public ActionSimpleNote(string message)
        {
            type = Type.SIMPLENOTE;
            this.message = message;
        }
    }
}
