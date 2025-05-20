using HRMS.Application.Models;
using Microsoft.AspNetCore.Identity;

namespace HRMS.Application.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                // Seed Roles
                await SeedRolesAsync(roleManager);

                // Seed Admin User
                await SeedAdminUserAsync(userManager);
            }
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roleNames = { "SuperAdmin", "HR", "Employee" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
        {
            var adminUser = await userManager.FindByEmailAsync("admin@hrms.com");
            if (adminUser == null)
            {
                var employee = new Employee
                {
                    EmployeeId = "ADMIN001",
                    FirstName = "System",
                    LastName = "Admin",
                    Email = "admin@hrms.com",
                    JoinDate = DateTime.Now,
                    DepartmentId = 1, // Assuming Administration department exists
                    IsActive = true
                };

                var admin = new ApplicationUser
                {
                    UserName = "admin@hrms.com",
                    Email = "admin@hrms.com",
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
}
