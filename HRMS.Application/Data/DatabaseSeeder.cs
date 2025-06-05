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
            await SeedAdminUserAsync(services.GetRequiredService<UserManager<ApplicationUser>>());
            await SeedDepartmentWeekendsAsync(services.GetRequiredService<ApplicationDbContext>());
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
                DepartmentId = 1, // Ensure this department exists or handle creation
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

        private static async Task SeedDepartmentWeekendsAsync(ApplicationDbContext context)
        {
            if (await context.DepartmentWeekends.AnyAsync())
                return;

            // Ensure there are departments first
            if (!await context.Departments.AnyAsync())
            {
                // Optionally seed default departments if none exist
                context.Departments.Add(new Department { Name = "Administration" });
                context.Departments.Add(new Department { Name = "Development Team A" });
                context.Departments.Add(new Department { Name = "Development Team B" });
                await context.SaveChangesAsync();
            }

            var departments = await context.Departments.ToListAsync();
            var weekends = new List<DepartmentWeekend>();

            foreach (var dept in departments)
            {
                var weekend = new DepartmentWeekend
                {
                    DepartmentId = dept.Id,
                    WeekendType = dept.Name.Contains("Development") ? "Custom" : "Single",
                    WeekendDay1 = DayOfWeek.Saturday
                };

                if (dept.Name.Contains("Team A"))
                {
                    weekend.WeekendDay1 = DayOfWeek.Friday;
                    weekend.WeekendDay2 = DayOfWeek.Saturday;
                    weekend.WeekendType = "Friday-Saturday";
                }
                else if (dept.Name.Contains("Team B"))
                {
                    weekend.WeekendDay1 = DayOfWeek.Saturday;
                    weekend.WeekendDay2 = DayOfWeek.Sunday;
                    weekend.WeekendType = "Saturday-Sunday";
                }

                weekends.Add(weekend);
            }

            await context.DepartmentWeekends.AddRangeAsync(weekends);
            await context.SaveChangesAsync();
        }
    }
}