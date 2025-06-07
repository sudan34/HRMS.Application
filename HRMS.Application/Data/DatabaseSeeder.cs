using HRMS.Application.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedInitialDataAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            // Seed in proper order
            await SeedRolesAsync(services.GetRequiredService<RoleManager<IdentityRole>>());
            await SeedDepartmentWeekendsAsync(services.GetRequiredService<ApplicationDbContext>());
            await SeedAdminUserAsync(services.GetRequiredService<UserManager<ApplicationUser>>());
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roleNames = { "SuperAdmin", "HR", "Employee" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }
        private static async Task SeedDepartmentWeekendsAsync(ApplicationDbContext context)
        {
            if (await context.DepartmentWeekends.AnyAsync())
                return;

            // Ensure there are departments first
            if (!await context.Departments.AnyAsync())
            {
                // Optionally seed default departments if none exist
                context.Departments.Add(new Department { Name = "Administration", Description = "Administration" });
                context.Departments.Add(new Department { Name = "Development Team A", Description = "Development A" });
                context.Departments.Add(new Department { Name = "Development Team B", Description = "Development B" });
                await context.SaveChangesAsync();
            }

            var departments = await context.Departments.ToListAsync();
            var weekends = new List<DepartmentWeekend>();

            foreach (var dept in departments)
            {
                int MapToSqlWeekday(DayOfWeek day) => ((int)day + 1); // Sunday=0 → 1, Monday=1 → 2, ..., Saturday=6 → 7

                var weekend = new DepartmentWeekend
                {
                    DepartmentId = dept.Id,
                    WeekendType = dept.Name.Contains("Development") ? "Custom" : "Single",
                    WeekendDay1 = (DayOfWeek)MapToSqlWeekday(DayOfWeek.Saturday)  // Default: Saturday
                };

                if (dept.Name.Contains("Team A"))
                {
                    weekend.WeekendDay1 = (DayOfWeek)MapToSqlWeekday(DayOfWeek.Friday);
                    weekend.WeekendDay2 = (DayOfWeek)MapToSqlWeekday(DayOfWeek.Saturday);
                    weekend.WeekendType = "Friday-Saturday";
                }
                else if (dept.Name.Contains("Team B"))
                {
                    weekend.WeekendDay1 = (DayOfWeek)MapToSqlWeekday(DayOfWeek.Saturday);
                    weekend.WeekendDay2 = (DayOfWeek)MapToSqlWeekday(DayOfWeek.Sunday);
                    weekend.WeekendType = "Saturday-Sunday";
                }

                weekends.Add(weekend);
            }

            await context.DepartmentWeekends.AddRangeAsync(weekends);
            await context.SaveChangesAsync();
        }
        private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
        {
            const string adminEmail = "admin@hrms.com";

            if (await userManager.FindByEmailAsync(adminEmail) != null)
                return;

            var employee = new Employee
            {
                EmployeeId = "001",
                FirstName = "System",
                LastName = "Admin",
                Email = adminEmail,
                JoinDate = DateTime.Now,
                DepartmentId = 1, 
                IsActive = true
            };

            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                Employee = employee,
                CustomRoleType = "SuperAdmin"
            };

            var result = await userManager.CreateAsync(admin, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "SuperAdmin");
            }
        }
    }
}