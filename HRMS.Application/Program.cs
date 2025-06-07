using HRMS.Application.Data;
using HRMS.Application.Models;
using HRMS.Application.Repository;
using HRMS.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using System.Data;

namespace HRMS.Application
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Configure Serilog early in the pipeline
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    path: "Logs/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    shared: true)
                .CreateLogger();

            try
            {
                Log.Information("Starting web application");

                var builder = WebApplication.CreateBuilder(args);
                // Add Serilog to the DI container
                builder.Host.UseSerilog();

                ConfigureServices(builder.Services, builder.Configuration);
                var app = builder.Build();

                ConfigureMiddleware(app, builder.Environment);
                await InitializeDatabase(app);

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }

        }
        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Database Configuration
            ConfigureDatabase(services, configuration);

            // Identity Configuration
            ConfigureIdentity(services);

            // Authentication Configuration
            ConfigureAuthentication(services);

            // Application Services
            ConfigureApplicationServices(services);

            // MVC Configuration
            services.AddControllersWithViews();
        }
        private static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Add Dapper configuration
            services.AddTransient<IDbConnection>(provider =>
            {
                return new SqlConnection(connectionString);
            });
        }

        private static void ConfigureIdentity(IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                ConfigurePasswordOptions(options);
                ConfigureLockoutOptions(options);
                ConfigureUserOptions(options);
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        }

        private static void ConfigurePasswordOptions(IdentityOptions options)
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;
        }

        private static void ConfigureLockoutOptions(IdentityOptions options)
        {
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        }

        private static void ConfigureUserOptions(IdentityOptions options)
        {
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;
        }

        private static void ConfigureAuthentication(IServiceCollection services)
        {
            services.AddAuthentication()
                .AddCookie(options =>
                {
                    options.Cookie.HttpOnly = true;
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.SlidingExpiration = true;
                });
        }

        private static void ConfigureApplicationServices(IServiceCollection services)
        {
            services.AddScoped<IZkDeviceService, ZkDeviceService>();
            services.AddScoped<IAttendanceStatusService, AttendanceStatusService>();
            services.AddScoped<IHolidayService, HolidayService>();

            // Add Dapper repository
            services.AddScoped<IAttendanceRepository, AttendanceRepository>();

            // Add AutoMapper with assembly scanning
            services.AddAutoMapper(typeof(Program).Assembly);
        }

        private static void ConfigureMiddleware(WebApplication app, IWebHostEnvironment env)
        {
            if (!env.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.MapStaticAssets();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}")
                .WithStaticAssets();
        }

        private static async Task InitializeDatabase(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                // Apply pending migrations first
                var dbContext = services.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.MigrateAsync();

                // Seed all data
                await DatabaseSeeder.SeedInitialDataAsync(services);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while initializing the database.");
            }
        }
    }
}
