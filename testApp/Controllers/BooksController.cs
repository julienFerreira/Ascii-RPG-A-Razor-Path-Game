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
            var book = await _context.Book.FirstOrDefaultAsync(m => m.Id == id);
            await _context.Entry(book).Collection(b => b.Actions).LoadAsync();

            if (book == null)
            {
                return NotFound();
            }


            book.IsValid = ValidateBook(book, out List<String> warnings, out List<String> errors);

            ViewBag.validated = book.IsValid;
            ViewBag.errors = errors;
            ViewBag.warnings = warnings;

            //save book validity
            _context.Update(book);
            await _context.SaveChangesAsync();

            return View("Details", book);
        }

        public bool ValidateBook(Book book, out List<String> warnings, out List<String> errors)
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
            if(actions.Find(a => a.ActionNumber == 1) == null)
            {
                errors.Add("Your book does not contain the initial action with actionnumber at 1 !");
                isBookValid = false;
            }

            //Verify that each action is linked (if an action is not linked, it is a warning because the action is unreachable but the book is not broken)
            foreach(int actionNumber in visited)
            {
                if (!linkedActions.Contains(actionNumber))
                {
                    warnings.Add($"Action {actionNumber} is not reachable because no other action is linked to it");
                }
            }

            return isBookValid;
        }

        public bool IsActionValid(Action action, List<Action> actions, out List<String> errorsAction, out List<String> warningsAction)
        {
            bool isActionValid = true;
            errorsAction = new List<string>();
            warningsAction = new List<string>();
            
            
            if(String.IsNullOrEmpty(action.ActionMessage))
            {
                warningsAction.Add($"action {action.ActionNumber} has no message, is it normal ?");
            }

            if (action.IsWon != null)
            {
                //Action is terminal
                if(action.SuccessorCode1 != null || action.SuccessorCode2 != null || action.SuccessorMessage1 != null || action.SuccessorMessage2 != null)
                {
                    warningsAction.Add($"action {action.ActionNumber} (message : {action.ActionMessage}) is terminal but has successor defined, it will never be used");
                }

                if(action.HPGains != null)
                {
                    warningsAction.Add($"action {action.ActionNumber} is terminal but has an HP gains that will never be used");
                }
            }
            else { 
                //Action is not terminal
                //verify successor existence and successor existence in book - a non terminal action must have two successors
                if(action.SuccessorCode1 == null)
                {
                    errorsAction.Add($"Your action {action.ActionNumber} (message : {action.ActionMessage}) has successor 1 that is null on a non terminal acton");
                    isActionValid = false;
                }
                else if ( !actions.Where(a => a.ActionNumber == action.SuccessorCode1).Any() )
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
