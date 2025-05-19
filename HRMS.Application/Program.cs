using HRMS.Application.Data;
using HRMS.Application.Models;
using HRMS.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Add Identity services
builder.Services.AddIdentity<Employee, ApplicationRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LogoutPath = "/Account/Logout";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireHR", policy => policy.RequireRole("HR"));
    options.AddPolicy("RequireSuperAdmin", policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("RequireAdminOrHR", policy => policy.RequireRole("SuperAdmin", "HR"));
});

builder.Services.AddScoped<IZkDeviceService, ZkDeviceService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();


// Seed super admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await SeedSuperAdmin(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the super admin user.");
    }
}

async Task SeedSuperAdmin(IServiceProvider services)
{
    var userManager = services.GetRequiredService<UserManager<Employee>>();
    var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

    // Ensure roles are created
    string[] roleNames = { "SuperAdmin", "HR", "Employee" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
        }
    }

    // Create super admin user
    var superAdminEmail = "superadmin@company.com";
    var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);
    if (superAdmin == null)
    {
        superAdmin = new Employee
        {
            UserName = superAdminEmail,
            Email = superAdminEmail,
            FirstName = "Super",
            LastName = "Admin",
            EmployeeId = "SA001",
            EmailConfirmed = true,
            IsActive = true,
            PhoneNumber = "+1234567890"
        };

        var createResult = await userManager.CreateAsync(superAdmin, "SuperAdmin@123");
        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");

            // Add to HR role as well if needed
            await userManager.AddToRoleAsync(superAdmin, "HR");
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
