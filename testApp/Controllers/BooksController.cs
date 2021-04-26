using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ARPG.Models;
using ARPG.Models.Data;
using Microsoft.AspNetCore.Identity;
using ARPG.Areas.Identity;
using Microsoft.AspNetCore.Authorization;
using Action = ARPG.Models.Action;


namespace ARPG.Controllers
{
    public class BooksController : Controller
    {
        private readonly ARPGContext _context;

        public BooksController(ARPGContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private readonly UserManager<User> _userManager;
        private Task<User> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);

        // GET : Library
        public async Task<IActionResult> Library()
        {
            var books = _context.Book.Where(b => b.IsValid);
            return View(books);
        }
        // GET: Books
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();
            await _context.Entry(user).Collection(u => u.Books).LoadAsync();

            return View(user.Books);
        }

        public async Task<IActionResult> Validate(int? id)
        {
            if (id == null)
                return NotFound();


            //fetch book with actions
            var book = await _context.Book.Include(b => b.Actions).FirstOrDefaultAsync(b => b.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            //init result var
            bool isBookValid;
            //Verify the structural integrity of the actions (doubles, links, etc)
            isBookValid = ValidateBook(book, out List<String> warnings, out List<String> errors);

            //If structural integrity is OK, verify paths of the book
            if (isBookValid)
            {
                isBookValid = ValidatePaths(book, out List<String> pathErrors);
                errors.AddRange(pathErrors);
            }

            //save book validity
            book.IsValid = isBookValid;
            _context.Update(book);
            await _context.SaveChangesAsync();

            //pass variables to view
            ViewBag.validated = book.IsValid;
            ViewBag.errors = errors;
            ViewBag.warnings = warnings;

            return View("Details", book);
        }

        private bool ValidateBook(Book book, out List<String> warnings, out List<String> errors)
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

        private bool IsActionValid(Action action, List<Action> actions, out List<String> errorsAction, out List<String> warningsAction)
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
            else {
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
                } else if (!actions.Where(a => a.ActionNumber == action.SuccessorCode2).Any())
                {
                    errorsAction.Add($"Your action {action.ActionNumber} (message : {action.ActionMessage}) has successor 2 that does not exists in the books ! ({action.SuccessorCode2})");
                    isActionValid = false;
                }
            }

            return isActionValid;
        }

        private bool ValidatePaths(Book book, out List<String> errors)
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

            foreach (ActionChainer chainer in actionChain) {
                if (!chainer.IsVerified)
                {
                    isBookValid = false;
                    if(chainer.IsPivot)
                        errors.Add($"Action {chainer.Action.ActionNumber} is a loop point and does not go to any terminal value");
                    else
                        errors.Add($"Action {chainer.Action.ActionNumber}  does not go to any terminal node, please fix this");
                }
            }

            //Go from action 1 and ignore every other action not linked to 1
            //As the client will not see them
            return isBookValid;
        }

        private void DiscoverPaths(ActionChainer node, IList<ActionChainer> chainList, IList<Action> actions)
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

        private void DiscoverChild(ActionChainer parent, int next, IList<ActionChainer> chainList, IList<Action> actions)
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
            } else
            {
                //case : chainers does not exist
                var action = actions.Where(a => a.ActionNumber == next).Single();
                var actionChainer = new ActionChainer(action, parent);
                chainList.Add(actionChainer);
                DiscoverPaths(actionChainer, chainList, actions);
            }
        }

        private void ValidateChildrens(ActionChainer verifiedNode)
        {
            verifiedNode.IsVerified = true;
            //Verify each parent of this node that is not already verifed
            foreach (ActionChainer chainer in verifiedNode.Parents){
                if(!chainer.IsVerified) //avoid multiple verifications
                    ValidateChildrens(chainer);
            }
        }

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

        [Authorize]
        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Book
                .FirstOrDefaultAsync(m => m.Id == id);
            var user = await GetCurrentUserAsync();

            if(book.User?.Id != user.Id)
            {
                return Unauthorized();
            }

            await _context.Entry(book).Collection(b => b.Actions).LoadAsync();
            if (book == null)
            {
                return NotFound();
            }
            
            return View(book);
        }

        // GET: Books/Create
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Books/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,IsValid")] Book book)
        {
            if (ModelState.IsValid)
            {
                var user = await GetCurrentUserAsync();
                book.User = user;
                _context.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        // GET: Books/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Book.FindAsync(id);
            var user = await GetCurrentUserAsync();

            if (book.User?.Id != user.Id)
            {
                return Unauthorized();
            }
            if (book == null)
            {
                return NotFound();
            }
            return View(book);
        }

        // POST: Books/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,IsValid")] Book book)
        {
            if (id != book.Id)
            {
                return NotFound();
            }
            var currentBook = await _context.Book.AsNoTracking().Include(b=>b.User).FirstAsync(b => book.Id==b.Id );
            //_context.Entry(currentBook).Reference(b => b.User);
            var user = await GetCurrentUserAsync();
            if (currentBook.User?.Id != user.Id)
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(book);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        // GET: Books/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Book
                .FirstOrDefaultAsync(m => m.Id == id);
            var user = await GetCurrentUserAsync();

            if (book.User?.Id != user.Id)
            {
                return Unauthorized();
            }

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {

            var book = await _context.Book.FindAsync(id);
            var user = await GetCurrentUserAsync();

            if (book.User?.Id != user.Id)
            {
                return Unauthorized();
            }
            _context.Book.Remove(book);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int id)
        {
            return _context.Book.Any(e => e.Id == id);
        }
    }
}
