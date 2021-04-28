using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ARPG.Models.Data;
using ARPG.Areas.Identity;

namespace ARPG.Controllers
{
    public class ActionsController : Controller
    {
        private readonly ARPGContext _context;
        private readonly int BASE_HEALTHPOINT = 50;
        private readonly string finalMessageLoose = @"Sadly, you have no healthpoint left. 
                You gather what courage you have left and leave without your dignity. Try again ?";

        public ActionsController(ARPGContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        private readonly UserManager<User> _userManager;
        private Task<User> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);


        // GET: Actions/Details/5
        [HttpGet("Books/{bookId}/Actions/{actionNumber}")]
        public async Task<IActionResult> Details([FromRoute]int? bookId, [FromRoute]int? actionNumber)
        {
            if (actionNumber == null || bookId == null)
            {
                return NotFound();
            }

            var action = await _context.Action
                .FirstOrDefaultAsync(m => (m.ActionNumber == actionNumber && m.BookId == bookId));


            if (action == null)
            {
                return NotFound();
            }

            //Check if action is terminal
            if (action.IsWon != null)
            {
                ViewBag.bookId = action.BookId;
                ViewBag.win = action.IsWon;
                ViewBag.message = action.ActionMessage;
                return View("End");
            }

            //Get HP from session, default at base healthpoint
            int healthPoint;
            if (actionNumber == 1)
            {
                //Fist view of a book - the first page always go to max hitpoints
                healthPoint = BASE_HEALTHPOINT;
            } else {
                healthPoint = HttpContext.Session.GetInt32("hp") ?? BASE_HEALTHPOINT;
                if(action.HPGains.HasValue)
                    healthPoint += action.HPGains.Value;//TODO LINK TO ACTION HEALTH
                if (healthPoint > BASE_HEALTHPOINT)
                    healthPoint = BASE_HEALTHPOINT;

                //Check if the user is below 0 hitpoints
                if (healthPoint <= 0)
                {
                    ViewBag.bookId = action.BookId;
                    ViewBag.message = finalMessageLoose;
                    ViewBag.win = false;
                    return View("End");
                }
            }

            HttpContext.Session.SetInt32("hp", healthPoint);

            ViewBag.hp = healthPoint;
            ViewBag.maxHP = BASE_HEALTHPOINT;
            ViewBag.ratio = (healthPoint >= 0 ? 100*((float)healthPoint / BASE_HEALTHPOINT) : 0); //avoid negative value for progressbar
            return View(action);
        }

        // GET: Actions/Create
        [Authorize]
        public async Task<ActionResult> Create(int id)
        {

            ViewBag.BookID = Request.Query["bookID"];
            int bookId = Int32.Parse(ViewBag.BookID+"");
            var book = await _context.Book.FirstAsync(b => b.Id == bookId);
            _context.Entry(book).Reference(b => b.User);
            var user = await GetCurrentUserAsync();

            if (book.User?.Id != user.Id)
            {
                return Unauthorized();
            }
            return View();
        }

        // POST: Actions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Models.Action actionCreated,int bookID)
        {
            if (ModelState.IsValid)
            {
                _context.Add(actionCreated);
                var book = await _context.Book.FirstAsync(b => b.Id == bookID);
                _context.Entry(book).Reference(b => b.User);
                var user = await GetCurrentUserAsync();

                if (book.User?.Id != user.Id)
                {
                    return Unauthorized();
                }


                //Action created : The book is not valid anymore and has to be re-verified
                book.IsValid = false;
                _context.Update(book);
                //Assign action to book

                actionCreated.Book = book;
                //Save both changes
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(BooksController.Details), new RouteValueDictionary(
                     new { 
                         controller = "Books", 
                         action = nameof(BooksController.Details), 
                         Id = bookID }
                     ));
            }
            return View(actionCreated);

        }

        // GET: Actions/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var action = await _context.Action.FindAsync(id);
            var book = await _context.Book.FindAsync(action.BookId);
            _context.Entry(book).Reference(b => b.User);
            var user = await GetCurrentUserAsync();

            if (book.User?.Id != user.Id)
            {
                return Unauthorized();
            }
            if (action == null)
            {
                return NotFound();
            }
            return View(action);
        }

        // POST: Actions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Models.Action actionEdit)
        {
            var action = await _context.Action.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            var book = await _context.Book.FindAsync(action.BookId);
            _context.Entry(book).Reference(b => b.User);
            var user = await GetCurrentUserAsync();
            if (book.User?.Id != user.Id)
            {
                return Unauthorized();
            }

            if (id != actionEdit.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    
                    //replace unchangable parameters
                    actionEdit.BookId = action.BookId;
                    _context.Update(actionEdit);
                    //Action updated : The book is not valid anymore and has to be re-verified
                    book.IsValid = false;
                    _context.Update(book);
                    //Save both changes
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ActionExists(actionEdit.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                //return RedirectToAction(nameof(Index));
                return RedirectToAction(nameof(BooksController.Details), new RouteValueDictionary(
                     new
                     {
                         controller = "Books",
                         action = nameof(BooksController.Details),
                         Id = actionEdit.BookId
                     }
                     ));
            } 
            return View(actionEdit);
        }

        // GET: Actions/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var action = await _context.Action
                .FirstOrDefaultAsync(m => m.Id == id);
            var book = await _context.Book.FindAsync(action.BookId);
            _context.Entry(book).Reference(b => b.User);
            var user = await GetCurrentUserAsync();

            if (book.User?.Id != user.Id)
            {
                return Unauthorized();
            }
            if (action == null)
            {
                return NotFound();
            }

            return View(action);
        }

        // POST: Actions/Delete/5
        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var action = await _context.Action.FindAsync(id);
            var book = await _context.Book.FindAsync(action.BookId);
            _context.Entry(book).Reference(b => b.User);
            //Action updated : The book is not valid anymore and has to be re-verified
            book.IsValid = false;
            _context.Update(book);
            var user = await GetCurrentUserAsync();

            if (book.User.Id != user.Id)
            {
                return Unauthorized();
            }
            _context.Action.Remove(action);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ActionExists(int id)
        {
            return _context.Action.Any(e => e.Id == id);
        }
    }
}