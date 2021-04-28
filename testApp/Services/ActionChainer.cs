using System;
using System.Collections.Generic;
using System.Linq;
using Action = ARPG.Models.Action;

namespace ARPG.Services
{
    public class ActionChainer
    {
        public ActionChainer(Action action)
        {
            this.Action = action;
            this.Parents = new List<ActionChainer>();
            IsPivot = IsVerified = false;
        }

        public ActionChainer(Action action, ActionChainer parent) : this(action)
        {
            AddParent(parent);
        }

        public void AddParent(ActionChainer parent)
        {
            Parents.Add(parent);
            if (Parents.Count() > 1)
                IsPivot = true;
        }

        //the action numbers are immutable 
        public Action Action { get; }
        public IList<ActionChainer> Parents { get; }

        //verified : called on the chain when a terminal is met
        public bool IsVerified { get; set; }

        //Pivot : called on the node when an action loop back to it
        public bool IsPivot { get; set; }

        public override string ToString()
        {
            return $"Action number = {Action.ActionNumber} Parent(s) = {Parents.Select(a => $"{a.Action.ActionNumber} , ")}";
        }
    }
}