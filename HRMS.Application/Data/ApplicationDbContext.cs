using HRMS.Application.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Data
{
    public class ApplicationDbContext : IdentityDbContext<Employee, ApplicationRole, int,
        IdentityUserClaim<int>, ApplicationUserRole, IdentityUserLogin<int>,
        IdentityRoleClaim<int>, IdentityUserToken<int>>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<ApplicationRole> ApplicationRoles { get; set; }

        // public DbSet<Role> Roles { get; set; }
        public DbSet<ApplicationUserRole> UserRoles { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUserRole>(userRole =>
            {
                userRole.HasKey(ur => new { ur.UserId, ur.RoleId });

                userRole.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();

                userRole.HasOne(ur => ur.User)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();
            });

            // Seed initial roles
            builder.Entity<ApplicationRole>().HasData(
                new ApplicationRole { Id = 1, Name = "Employee", NormalizedName = "EMPLOYEE", Description = "Regular employee" },
                new ApplicationRole { Id = 2, Name = "HR", NormalizedName = "HR", Description = "HR personnel" },
                new ApplicationRole { Id = 3, Name = "SuperAdmin", NormalizedName = "SUPERADMIN", Description = "System administrator" }
            );

            // Configure relationships and constraints
            builder.Entity<Attendance>()
                .HasOne(a => a.Employee)
                .WithMany(e => e.Attendances)
                .HasForeignKey(a => a.EmployeeId);
        }
    }
}
