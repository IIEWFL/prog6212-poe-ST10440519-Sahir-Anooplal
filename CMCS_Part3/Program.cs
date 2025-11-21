using Microsoft.EntityFrameworkCore;
using CMCS_Part3.Data;
using CMCS_Part3.Services;
using Microsoft.AspNetCore.Identity;
using CMCS_Part3.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews(); //[1]

// Configure in-memory database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("CMCS_Database")); //[1]

// Configure identity with in-memory stores
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false; //[2]
    options.Password.RequireLowercase = false; //[2]
    options.Password.RequireNonAlphanumeric = false; //[2]
    options.Password.RequireUppercase = false; //[2]
    options.Password.RequiredLength = 3; //[2]
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Register services
builder.Services.AddScoped<IClaimService, ClaimService>(); //[1]
builder.Services.AddScoped<IReportService, ReportService>(); //[1]
builder.Services.AddScoped<IUserService, UserService>(); //[1]

builder.Services.AddSession();

var app = builder.Build();

// Seed initial data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); //[1]
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); //[2]
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(); //[2]

    await SeedData.Initialize(context, userManager, roleManager); //[1]
}

// Configure pipeline
if (!app.Environment.IsDevelopment()) //[1]
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


/*
[1] Microsoft Docs. "ASP.NET Core Fundamentals." https://learn.microsoft.com/en-us/aspnet/core/
[2] Microsoft Docs. "ASP.NET Core Identity." https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity
*/