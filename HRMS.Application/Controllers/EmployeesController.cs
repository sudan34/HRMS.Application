using HRMS.Application.Data;
using HRMS.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Controllers
{
    [Authorize]
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Employees
        [Authorize(Policy = "RequireAdminOrHR")]
        public async Task<IActionResult> Index()
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(e => e.IsActive)
                .ToListAsync();

            return View(employees);
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (employee == null)
            {
                return NotFound();
            }

            // Employees can only view their own details unless HR/Admin
            if (!User.IsInRole("HR") && !User.IsInRole("SuperAdmin") &&
                employee.Email != User.Identity.Name)
            {
                return Forbid();
            }

            return View(employee);
        }

        // GET: Employees/Create
        [Authorize(Policy = "RequireHR")]
        public IActionResult Create()
        {
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name");
            ViewData["Roles"] = new SelectList(_context.Roles, "Id", "Name");
            return View();
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireHR")]
        public async Task<IActionResult> Create(
            [Bind("Id,EmployeeId,FirstName,LastName,Email,Phone,JoinDate,DepartmentId,IsActive")] Employee employee,
            List<int> SelectedRoles)
        {
            if (ModelState.IsValid)
            {
                _context.Add(employee);
                await _context.SaveChangesAsync();

                // Add selected roles
                foreach (var roleId in SelectedRoles)
                {
                    _context.UserRoles.Add(new ApplicationUserRole
                    {
                        UserId = employee.Id,
                        RoleId = roleId
                    });
                }
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", employee.DepartmentId);
            ViewData["Roles"] = new SelectList(_context.Roles, "Id", "Name");
            return View(employee);
        }

        // GET: Employees/Edit/5
        [Authorize(Policy = "RequireAdminOrHR")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.UserRoles)
                .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);

            if (employee == null)
            {
                return NotFound();
            }

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", employee.DepartmentId);
            ViewData["Roles"] = new MultiSelectList(_context.Roles, "Id", "Name",
                employee.UserRoles.Select(ur => ur.RoleId));
            return View(employee);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdminOrHR")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,EmployeeId,FirstName,LastName,Email,Phone,JoinDate,DepartmentId,IsActive")] Employee employee,
            List<int> SelectedRoles)
        {
            if (id != employee.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update employee
                    _context.Update(employee);

                    // Update roles
                    var existingRoles = await _context.UserRoles
                        .Where(ur => ur.UserId == employee.Id)
                        .ToListAsync();

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
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.Id))
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

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", employee.DepartmentId);
            ViewData["Roles"] = new MultiSelectList(_context.Roles, "Id", "Name", SelectedRoles);
            return View(employee);
        }

        // POST: Employees/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireSuperAdmin")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            employee.IsActive = false;
            _context.Update(employee);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.Id == id);
        }
    }
}
