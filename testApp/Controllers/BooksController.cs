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
using ARPG.Services;

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
            var book = await _context.Book.Include(b => b.Actions).Where(b => b.Id == id).SingleOrDefaultAsync();
            if (book == null)
                return NotFound();

            //Call the book validator to validate the book
            bool isBookValid = BookValidator.Validate(book, out List<string> warnings, out List<String> errors);

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
