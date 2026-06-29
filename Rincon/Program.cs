using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rincon.DataAccess.Data;
using Rincon.DataAccess.Data.Repository;
using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;
using Rincon.Utilities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("ConexionPostgres");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'ConexionPostgres' is not configured. Set it with user-secrets locally or with the environment variable 'ConnectionStrings__ConexionPostgres' in production.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IWorkContainer, WorkContainer>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys");
var dataProtectionBuilder = Microsoft.AspNetCore.DataProtection.DataProtectionBuilderExtensions
    .PersistKeysToFileSystem(builder.Services.AddDataProtection(), new DirectoryInfo(dataProtectionKeysPath));
Microsoft.AspNetCore.DataProtection.DataProtectionBuilderExtensions
    .SetApplicationName(dataProtectionBuilder, "Rincon");

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

var app = builder.Build();

// Crear roles automáticamente si no existen
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles =
    {
        SD.Role_Admin,
        SD.Role_Employee
    };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area=Employee}/{controller=Cart}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
