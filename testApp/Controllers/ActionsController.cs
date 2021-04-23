using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ARPG.Models;
using ARPG.Models.Data;
using Microsoft.AspNetCore.Routing;

namespace ARPG.Controllers
{
    public class ActionsController : Controller
    {
        private readonly ARPGContext _context;

        public ActionsController(ARPGContext context)
        {
            _context = context;
        }

        // GET: Actions
        public async Task<IActionResult> Index()
        {
            return View(await _context.Action.ToListAsync());
        }


        // GET: Actions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var action = await _context.Action
                .FirstOrDefaultAsync(m => m.ActionNumber == id);
            if (action == null)
            {
                return NotFound();
            }

            return View(action);
        }

        // GET: Actions/Create
        public IActionResult Create(int id)
        {
            ViewBag.BookID = Request.Query["bookID"];
            return View();
        }

        // POST: Actions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Models.Action actionCreated,int bookID)
        {
            if (ModelState.IsValid)
            {
                _context.Add(actionCreated);
                var book = await _context.Book.FirstAsync(b => b.Id == bookID);
                actionCreated.book = book;
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
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var action = await _context.Action.FindAsync(id);
            if (action == null)
            {
                return NotFound();
            }
            return View(action);
        }

        // POST: Actions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Models.Action actionEdit)
        {
            if (id != actionEdit.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(actionEdit);
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
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var action = await _context.Action
                .FirstOrDefaultAsync(m => m.Id == id);
            if (action == null)
            {
                return NotFound();
            }

            return View(action);
        }

        // POST: Actions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var action = await _context.Action.FindAsync(id);
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
