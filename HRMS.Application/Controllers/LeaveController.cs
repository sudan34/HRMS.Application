using HRMS.Application.Data;
using HRMS.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Controllers
{
    [Authorize(Roles = "SuperAdmin,HR")]
    public class LeaveController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LeaveController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var leaves = await _context.Leaves
                .Include(l => l.Employee)
                .OrderByDescending(l => l.StartDate)
                .ToListAsync();
            return View(leaves);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Employees = await _context.Employees.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Leave leave)
        {
            if (ModelState.IsValid)
            {
                leave.CreatedBy = User.Identity.Name;
                leave.CreatedOn = DateTime.Now;
                _context.Add(leave);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Employees = await _context.Employees.ToListAsync();
            return View(leave);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var leave = await _context.Leaves.FindAsync(id);
            if (leave == null) return NotFound();

            ViewBag.Employees = await _context.Employees.ToListAsync();
            return View(leave);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Leave leave)
        {
            if (id != leave.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    leave.UpdatedBy = User.Identity.Name;
                    leave.UpdatedOn = DateTime.Now;
                    _context.Update(leave);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LeaveExists(leave.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Employees = await _context.Employees.ToListAsync();
            return View(leave);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var leave = await _context.Leaves
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (leave == null) return NotFound();

            return View(leave);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var leave = await _context.Leaves.FindAsync(id);
            if (leave != null)
            {
                _context.Leaves.Remove(leave);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool LeaveExists(int id)
        {
            return _context.Leaves.Any(e => e.Id == id);
        }
    }
}
