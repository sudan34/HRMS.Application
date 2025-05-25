using HRMS.Application.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Department> Departments { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Employee)
                .WithMany(e => e.Attendances)
                .HasForeignKey(a => a.EmployeeId)
                .HasPrincipalKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            //modelBuilder.Entity<User>()
            //.HasOne(u => u.Employee)
            //.WithMany()
            //.HasForeignKey(u => u.EmployeeId);

            //// Seed initial data
            //modelBuilder.Entity<Department>().HasData(
            //    new Department { Id = 1, Name = "Administration", Description = "Administration Department" },
            //    new Department { Id = 2, Name = "HR", Description = "Human Resources Department" },
            //    new Department { Id = 3, Name = "IT", Description = "Information Technology Department" }
            //);
        }
    }
}
