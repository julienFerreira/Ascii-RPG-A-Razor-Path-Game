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
        /// Entirely validate a book, structure then paths
        /// </summary>
        /// <param name="book">The book to validate</param>
        /// <param name="warnings">The list of warnings associated with this book</param>
        /// <param name="errors">The list of errors associated with this book</param>
        /// <returns>Wether the book is validated or not</returns>
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
                errors.AddRange(pathErrors); //errors are supposed to be empty if the book is valid, but better concat then replace
            }

            return isBookValid;
        }

        /// <summary>
        /// Validate the structural integrity of a book
        /// Tries to be as tolerant as possible with warnings that are not fatal to user experience
        /// </summary>
        /// <param name="book">The book to analyse</param>
        /// <param name="warnings">The warnings found on the book</param>
        /// <param name="errors">The errors found on the boo</param>
        /// <returns>Wether the book is structurally valid</returns>
        private static bool ValidateBook(Book book, out List<String> warnings, out List<String> errors)
        {
            warnings = new List<string>();
            errors = new List<string>();

            bool isBookValid = true;
            IList<int> visited = new List<int>();
            ISet<int> linkedActions = new HashSet<int>(); //set avoids repetition of linked action numbers
            List<Action> actions = book.Actions.ToList();

            foreach (Action action in actions)
            {
                //verify duplicates
                if (visited.Contains(action.ActionNumber))
                {
                    isBookValid = false;
                    errors.Add($"You have a duplicate ! action number {action.ActionNumber} is used multiple times, fix this");
                }

                //add successors in linked actions list
                if (action.SuccessorCode1.HasValue)
                    linkedActions.Add(action.SuccessorCode1.Value);
                if (action.SuccessorCode2.HasValue)
                    linkedActions.Add(action.SuccessorCode2.Value);

                //add action to visited
                visited.Add(action.ActionNumber);
                //verify the action itself
                if (!IsActionValid(action, actions, out List<String> errorsAction, out List<String> warningsAction))
                    isBookValid = false;

                errors.AddRange(errorsAction);
                warnings.AddRange(warningsAction);
            }

            // Check that initial action is present !
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

        /// <summary>
        /// Verifies if a particular action is valid.
        /// Tries to be as tolerant as possible, throw warning when possible
        /// </summary>
        /// <param name="action"></param>
        /// <param name="actions"></param>
        /// <param name="errorsAction"></param>
        /// <param name="warningsAction"></param>
        /// <returns></returns>
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

            //can't validate if there is not exactly one starting point, shouldn't happen after structural verification
            var initialActionQuery = actions.Where(a => a.ActionNumber == 1);
            if (initialActionQuery == null || initialActionQuery.Count() != 1)
                return false;

            ActionChainer initialChainer = new ActionChainer(initialActionQuery.Single());
            IList<ActionChainer> actionChain = new List<ActionChainer> {
                initialChainer,
            };
            //call the recursive function that create the action chains according to the paths of the book
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

            return isBookValid;
        }


        //Try to validate error in failed path
        #region
        /*
        private static void FindFailedPaths(ActionChainer FailingChainer, out List<List<int>> paths)
        {
            List<int> initialPath = new List<int>();
            paths = new List<List<int>>()
            {
                initialPath
            };
            RecursiveFailedPathsSearch(FailingChainer, paths, initialPath);
        }

        private static void RecursiveFailedPathsSearch(ActionChainer FailingChainer, List<List<int>> paths, List<int> currentPath)
        {
            if (currentPath.Contains(FailingChainer.Action.ActionNumber))
                return;

            int nextPathCount = FailingChainer.Parents.Where(a => a.IsVerified == false).Count();
            List<int> initialPathSnapshot = null;
            if (nextPathCount > 1)
            {
                initialPathSnapshot = new List<int>(currentPath);
            }

            bool doSeparate = false; 
            foreach (ActionChainer parent in FailingChainer.Parents)
            {
                if (!parent.IsVerified)
                {
                    List<int> pathToAction;
                    //First activation - continue current path
                    if (!doSeparate)
                    {
                        doSeparate = true;
                        pathToAction = currentPath;
                    } else //already activated - create a new path
                    {
                        pathToAction = new List<int>(initialPathSnapshot);
                        paths.Add(pathToAction);
                    }

                    pathToAction.Add(FailingChainer.Action.ActionNumber);
                    RecursiveFailedPathsSearch(parent, paths, pathToAction);
                }
            }
        }*/
        #endregion

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
                childrenChainer.AddParent(parent); //declare that this chainer now has one more parent
                if (childrenChainer.IsVerified)
                {
                    //already verified - we are good to go. Otherwise we could be called later when the chainer is verified
                    ValidateChildrens(parent);
                }
            }
            else
            {
                //case : chainers does not exist. Fetch action
                var action = actions.Where(a => a.ActionNumber == next).Single();
                //create chainer, add it to the list
                var actionChainer = new ActionChainer(action, parent);
                chainList.Add(actionChainer);
                //Discover paths on the newly created chainer, keeping exploring the book
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