using HRMS.Application.Data;
using HRMS.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmployeeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Employee
        [Authorize(Roles = "HR,SuperAdmin")]
        public async Task<IActionResult> Index()
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .ToListAsync();
            return View(employees);
        }

        // GET: Employee/Details/5
        [Authorize(Roles = "Employee,HR,SuperAdmin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employeeIdClaim = User.FindFirst("EmployeeId");
            if (employeeIdClaim == null)
            {
                return Forbid();
            }
            // Get current logged-in user's EmployeeId from Claims
            var currentEmployeeId = int.Parse(employeeIdClaim.Value);

            var isHRorAdmin = User.IsInRole("HR") || User.IsInRole("SuperAdmin");

            if (!isHRorAdmin && id != currentEmployeeId)
            {
                return Forbid();
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // GET: Employee/Create
        [Authorize(Roles = "HR")]
        public IActionResult Create()
        {
            ViewBag.Departments = _context.Departments.ToList();
            return View();
        }

        // POST: Employee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Create(Employee employee)
        {
            if (ModelState.IsValid)
            {
                employee.JoinDate = DateTime.Now;
                _context.Add(employee);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Departments = _context.Departments.ToList();
            return View(employee);
        }

        // GET: Employee/Edit/5
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            ViewBag.Departments = _context.Departments.ToList();
            return View(employee);
        }

        // POST: Employee/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Edit(int id, Employee employee)
        {
            if (id != employee.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingEmployee = await _context.Employees.FindAsync(id);
                    if (existingEmployee == null)
                    {
                        return NotFound();
                    }
                    // Update allowed properties only
                    existingEmployee.EmployeeId = employee.EmployeeId ?? existingEmployee.EmployeeId;
                    existingEmployee.FirstName = employee.FirstName;
                    existingEmployee.LastName = employee.LastName;
                    existingEmployee.Email = employee.Email;
                    existingEmployee.Phone = employee.Phone;
                    existingEmployee.JoinDate = employee.JoinDate;
                    existingEmployee.DepartmentId = employee.DepartmentId;

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
            ViewBag.Departments = _context.Departments.ToList();
            return View(employee);
        }

        // GET: Employee/Delete/5
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employee/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);

            if (employee != null)
            {
                employee.IsActive = false; // Soft delete
                _context.Update(employee);

                // Optional: Deactivate login if Identity is connected
                ApplicationUser user = null;

                if (int.TryParse(employee.EmployeeId, out int empIdInt))
                {
                    user = await _userManager.Users
                        .FirstOrDefaultAsync(u => u.EmployeeId == empIdInt);
                }

                if (user != null)
                {
                    user.LockoutEnabled = true;
                    user.LockoutEnd = DateTimeOffset.MaxValue; // Effectively disables login
                    await _userManager.UpdateAsync(user);
                }

                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.Id == id);
        }
    }
}