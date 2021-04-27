using ARPG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Action = ARPG.Models.Action;

namespace ARPG.Services
{
    public class BookValidator
    {
        /// <summary>
        /// Entry point of the book validator
        /// Entirely validate a book, structure & paths
        /// </summary>
        /// <param name="book"></param>
        /// <param name="warnings"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public static bool Validate(Book book, out List<string> warnings, out List<String> errors)
        {
            //init result var
            bool isBookValid;
            //Verify the structural integrity of the actions (doubles, links, etc)
            isBookValid = ValidateBook(book, out warnings, out errors);

            //If structural integrity is OK, verify paths of the book
            if (isBookValid)
            {
                isBookValid = ValidatePaths(book, out List<String> pathErrors);
                errors.AddRange(pathErrors);
            }

            return isBookValid;
        }

        private static bool ValidateBook(Book book, out List<String> warnings, out List<String> errors)
        {
            warnings = new List<string>();
            errors = new List<string>();

            bool isBookValid = true;
            List<int> visited = new List<int>();
            ISet<int> linkedActions = new HashSet<int>();
            List<Action> actions = book.Actions.ToList();

            foreach (Action action in actions)
            {
                //verify duplicates
                if (visited.Contains(action.ActionNumber))
                {
                    isBookValid = false;
                    errors.Add($"You have a duplicate ! action number {action.ActionNumber} is used multiple times, fix this");
                }

                if (action.SuccessorCode1.HasValue)
                    linkedActions.Add(action.SuccessorCode1.Value);
                if (action.SuccessorCode2.HasValue)
                    linkedActions.Add(action.SuccessorCode2.Value);

                visited.Add(action.ActionNumber);
                if (!IsActionValid(action, actions, out List<String> errorsAction, out List<String> warningsAction))
                    isBookValid = false;

                errors.AddRange(errorsAction);
                warnings.AddRange(warningsAction);
            }

            // Lastly : Check that initial action is valid !
            if (actions.Find(a => a.ActionNumber == 1) == null)
            {
                errors.Add("Your book does not contain the initial action with actionnumber at 1 !");
                isBookValid = false;
            }

            //Verify that each action is linked (if an action is not linked, it is a warning because the action is unreachable but the book is not broken)
            foreach (int actionNumber in visited)
            {
                if (actionNumber != 1 && !linkedActions.Contains(actionNumber))
                {
                    warnings.Add($"Action {actionNumber} is not reachable because no other action is linked to it");
                }
            }

            return isBookValid;
        }

        private static bool IsActionValid(Action action, List<Action> actions, out List<String> errorsAction, out List<String> warningsAction)
        {
            bool isActionValid = true;
            errorsAction = new List<string>();
            warningsAction = new List<string>();


            if (String.IsNullOrEmpty(action.ActionMessage))
            {
                warningsAction.Add($"action {action.ActionNumber} has no message, is it normal ?");
            }

            if (action.IsWon != null)
            {
                //Action is terminal
                if (action.SuccessorCode1 != null || action.SuccessorCode2 != null || action.SuccessorMessage1 != null || action.SuccessorMessage2 != null)
                {
                    warningsAction.Add($"action {action.ActionNumber} (message : {action.ActionMessage}) is terminal but has successor defined, it will never be used");
                }

                if (action.HPGains != null)
                {
                    warningsAction.Add($"action {action.ActionNumber} is terminal but has an HP gains that will never be used");
                }
            }
            else
            {
                //Action is not terminal
                //verify successor existence and successor existence in book - a non terminal action must have two successors
                if (action.SuccessorCode1 == null)
                {
                    errorsAction.Add($"Your action {action.ActionNumber} (message : {action.ActionMessage}) has successor 1 that is null on a non terminal acton");
                    isActionValid = false;
                }
                else if (!actions.Where(a => a.ActionNumber == action.SuccessorCode1).Any())
                {
                    errorsAction.Add($"Your action {action.ActionNumber} (message : {action.ActionMessage}) has successor 1 that does not exists in the books ! ({action.SuccessorCode1})");
                    isActionValid = false;
                }

                if (action.SuccessorCode2 == null)
                {
                    errorsAction.Add($"Your action {action.ActionNumber} (message : {action.ActionMessage}) has successor 2 that is null on a non terminal action");
                    isActionValid = false;
                }
                else if (!actions.Where(a => a.ActionNumber == action.SuccessorCode2).Any())
                {
                    errorsAction.Add($"Your action {action.ActionNumber} (message : {action.ActionMessage}) has successor 2 that does not exists in the books ! ({action.SuccessorCode2})");
                    isActionValid = false;
                }
            }

            return isActionValid;
        }

        private static bool ValidatePaths(Book book, out List<String> errors)
        {
            errors = new List<string>();

            bool isBookValid = true;
            IList<Action> actions = book.Actions.ToList();

            var initialActionQuery = actions.Where(a => a.ActionNumber == 1);
            if (initialActionQuery == null || initialActionQuery.Count() != 1)
                return false; //can't validate if there is not exactly one starting point

            ActionChainer initialChainer = new ActionChainer(initialActionQuery.Single());
            IList<ActionChainer> actionChain = new List<ActionChainer>();
            actionChain.Add(initialChainer);
            DiscoverPaths(initialChainer, actionChain, actions);

            foreach (ActionChainer chainer in actionChain)
            {
                if (!chainer.IsVerified)
                {
                    isBookValid = false;
                    if (chainer.IsPivot)
                        errors.Add($"Action {chainer.Action.ActionNumber} is a loop point and does not go to any terminal value");
                    else
                        errors.Add($"Action {chainer.Action.ActionNumber}  does not go to any terminal node, please fix this");
                }
            }

            //Go from action 1 and ignore every other action not linked to 1
            //As the client will not see them
            return isBookValid;
        }

        private static void DiscoverPaths(ActionChainer node, IList<ActionChainer> chainList, IList<Action> actions)
        {
            //Action is terminal
            if (node.Action.IsWon != null)
                ValidateChildrens(node);
            else
            {
                //discover childrens - they exists as the verification of the path occurs after the verification
                //of the actions structure
                DiscoverChild(node, node.Action.SuccessorCode1.Value, chainList, actions);
                DiscoverChild(node, node.Action.SuccessorCode2.Value, chainList, actions);
            }
        }

        private static void DiscoverChild(ActionChainer parent, int next, IList<ActionChainer> chainList, IList<Action> actions)
        {
            //verify if the chainer for this action already exists
            var chainerQuery = chainList.Where(actionChainer => actionChainer.Action.ActionNumber == next);
            if (chainerQuery.Count() == 1)
            {
                //case : chainer exists, action already visited (avoid looping)
                ActionChainer childrenChainer = chainerQuery.Single();
                childrenChainer.Parents.Add(parent); //declare that this chainer now has one more parent
                if (childrenChainer.IsVerified)
                {
                    //already verified - we are good to go. Otherwise we could be called when the chainer is verified
                    ValidateChildrens(parent);
                }
            }
            else
            {
                //case : chainers does not exist
                var action = actions.Where(a => a.ActionNumber == next).Single();
                var actionChainer = new ActionChainer(action, parent);
                chainList.Add(actionChainer);
                DiscoverPaths(actionChainer, chainList, actions);
            }
        }

        private static void ValidateChildrens(ActionChainer verifiedNode)
        {
            verifiedNode.IsVerified = true;
            //Verify each parent of this node that is not already verifed
            foreach (ActionChainer chainer in verifiedNode.Parents)
            {
                if (!chainer.IsVerified) //avoid multiple verifications
                    ValidateChildrens(chainer);
            }
        }
    }
}