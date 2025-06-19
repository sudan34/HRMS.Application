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
        public DbSet<UnknownAttendanceRecord> UnknownAttendanceRecords { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Leave> Leaves { get; set; }
        public DbSet<Holiday> Holidays { get; set; }
        public DbSet<AttendanceSummary> AttendanceSummaries { get; set; }
        //  public DbSet<CalendarDay> CalendarDays { get; set; }
        public DbSet<DepartmentWeekend> DepartmentWeekends { get; set; }
        public DbSet<DepartmentWorkingHours> DepartmentWorkingHours { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Employee
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasIndex(e => e.EmployeeId).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.JoinDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // Configure Attendance
            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.HasOne(a => a.Employee)
                    .WithMany(e => e.Attendances)
                    .HasForeignKey(a => a.EmployeeId)
                    .HasPrincipalKey(e => e.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(a => a.Status)
                    .HasDefaultValue(AttendanceStatus.Present);

                entity.Property(a => a.CreatedOn)
                    .HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<AttendanceSummary>()
                .HasOne(a => a.Employee)
                .WithMany()
                .HasPrincipalKey(e => e.EmployeeId) // Specify EmployeeId as the principal key
                .HasForeignKey(a => a.EmployeeId); // Specify EmployeeId as the foreign key

            // Configure DepartmentWeekend
            modelBuilder.Entity<DepartmentWeekend>(entity =>
            {
                entity.HasOne(dw => dw.Department)
                    .WithOne(d => d.DepartmentWeekend)
                    .HasForeignKey<DepartmentWeekend>(dw => dw.DepartmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(dw => dw.WeekendType)
                    .HasMaxLength(50)
                    .IsRequired();
            });

            // Configure ApplicationUser
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.HasOne(u => u.Employee)
                    .WithOne()
                    .HasForeignKey<ApplicationUser>(u => u.EmployeeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
