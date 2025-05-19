using HRMS.Application.Data;
using HRMS.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Controllers
{
    [Authorize(Policy = "RequireSuperAdmin")]
    public class RolesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RolesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Roles/Assign/5
        public async Task<IActionResult> Assign(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            ViewData["Roles"] = new MultiSelectList(_context.Roles, "Id", "Name",
                employee.UserRoles.Select(ur => ur.RoleId));
            return View(employee);
        }

        // POST: Roles/Assign/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int id, List<int> SelectedRoles)
        {
            var employee = await _context.Users
                .Include(e => e.UserRoles)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            // Update roles
            var existingRoles = employee.UserRoles.ToList();

            // Remove unselected roles
            foreach (var existingRole in existingRoles)
            {
                if (!SelectedRoles.Contains(existingRole.RoleId))
                {
                    _context.UserRoles.Remove(existingRole);
                }
            }

            // Add new roles
            foreach (var roleId in SelectedRoles)
            {
                if (!existingRoles.Any(er => er.RoleId == roleId))
                {
                    _context.UserRoles.Add(new ApplicationUserRole
                    {
                        UserId = employee.Id,
                        RoleId = roleId
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), "Employees");
        }
    }
}
