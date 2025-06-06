using HRMS.Application.Data;
using HRMS.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class DepartmentWorkingHoursController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartmentWorkingHoursController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var workingHours = await _context.DepartmentWorkingHours
                .Include(d => d.Department)
                .ToListAsync();
            return View(workingHours);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Departments = await _context.Departments.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DepartmentWorkingHours workingHours)
        {
            if (ModelState.IsValid)
            {
                _context.Add(workingHours);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Departments = await _context.Departments.ToListAsync();
            return View(workingHours);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var workingHours = await _context.DepartmentWorkingHours.FindAsync(id);
            if (workingHours == null) return NotFound();

            ViewBag.Departments = await _context.Departments.ToListAsync();
            return View(workingHours);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DepartmentWorkingHours workingHours)
        {
            if (id != workingHours.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(workingHours);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WorkingHoursExists(workingHours.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Departments = await _context.Departments.ToListAsync();
            return View(workingHours);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var workingHours = await _context.DepartmentWorkingHours
                .Include(d => d.Department)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (workingHours == null) return NotFound();

            return View(workingHours);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var workingHours = await _context.DepartmentWorkingHours.FindAsync(id);
            if (workingHours != null)
            {
                _context.DepartmentWorkingHours.Remove(workingHours);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool WorkingHoursExists(int id)
        {
            return _context.DepartmentWorkingHours.Any(e => e.Id == id);
        }
    }
}
