using ElevateWorkforce.Application;
using ElevateWorkforce.Domain.Entities;
using ElevateWorkforce.Infrastructure;
using ElevateWorkforce.Infrastructure.Data;
using ElevateWorkforce.Web;
using ElevateWorkforce.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//
// ============================================================
// APPLICATION SERVICES
// ============================================================
//

builder.Services.AddApplication();


//
// ============================================================
// INFRASTRUCTURE SERVICES
// ============================================================
//

builder.Services.AddInfrastructure(builder.Configuration);


//
// ============================================================
// JWT CONFIGURATION
// ============================================================
//

var jwtSection = builder.Configuration.GetSection("Jwt");

var jwtOptions = jwtSection.Get<JwtOptions>()
    ?? new JwtOptions();

//
// Render should normally provide Jwt__Key.
// This fallback prevents the application from crashing
// if the environment variable is temporarily missing.
//
if (string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    jwtOptions.Key =
        "ElevateWorkforce-Demo-JWT-Key-2026-Production-64-Characters-Long";
}

//
// Make sure the key is long enough.
//
if (Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must contain at least 32 characters.");
}

//
// Default values if they are missing.
//
if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
{
    jwtOptions.Issuer = "ElevateWorkforce";
}

if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
{
    jwtOptions.Audience = "ElevateWorkforce.Api";
}

if (jwtOptions.ExpirationMinutes <= 0)
{
    jwtOptions.ExpirationMinutes = 60;
}

//
// Register the final JWT configuration.
//
builder.Services.Configure<JwtOptions>(options =>
{
    options.Key = jwtOptions.Key;
    options.Issuer = jwtOptions.Issuer;
    options.Audience = jwtOptions.Audience;
    options.ExpirationMinutes = jwtOptions.ExpirationMinutes;
});


//
// ============================================================
// ASP.NET CORE IDENTITY
// ============================================================
//

builder.Services.AddIdentity<User, ApplicationRole>(options =>
{
    //
    // Password requirements
    //
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;

    //
    // User settings
    //
    options.User.RequireUniqueEmail = true;

    //
    // Account lockout
    //
    options.Lockout.DefaultLockoutTimeSpan =
        TimeSpan.FromMinutes(10);

    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();


//
// ============================================================
// AUTHENTICATION / JWT
// ============================================================
//

builder.Services
    .AddAuthentication()
    .AddJwtBearer(
        JwtBearerDefaults.AuthenticationScheme,
        options =>
        {
            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,

                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                                jwtOptions.Key))
                };
        });


//
// ============================================================
// APPLICATION COOKIE
// ============================================================
//

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/accessdenied";

    options.Cookie.Name =
        "ElevateWorkforce.Auth";

    options.SlidingExpiration = true;
});


//
// ============================================================
// HTTP CONTEXT
// ============================================================
//

builder.Services.AddHttpContextAccessor();


//
// ============================================================
// MVC
// ============================================================
//

builder.Services.AddControllersWithViews();


//
// ============================================================
// API / OPENAPI
// ============================================================
//

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();


//
// ============================================================
// LOCALIZATION
// ============================================================
//

builder.Services.Configure<RequestLocalizationOptions>(
    options =>
    {
        options.DefaultRequestCulture =
            new Microsoft.AspNetCore.Localization.RequestCulture("en");
    });


//
// ============================================================
// BUILD APPLICATION
// ============================================================
//

var app = builder.Build();


//
// ============================================================
// DATABASE INITIALIZATION
// ============================================================
//

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var logger =
        services.GetRequiredService<ILogger<Program>>();

    try
    {
        var db =
            services.GetRequiredService<ApplicationDbContext>();

        //
        // Apply Entity Framework Core migrations
        //
        await db.Database.MigrateAsync();

        //
        // Seed initial/demo users and data
        //
        await SeedData.InitializeAsync(services);

        logger.LogInformation(
            "Database migration and seed completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(
            ex,
            "Database initialization failed.");

        //
        // Stop the application if the database
        // cannot be initialized.
        //
        throw;
    }
}


//
// ============================================================
// ERROR HANDLING
// ============================================================
//

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/home/error");

    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}


//
// ============================================================
// STATIC FILES
// ============================================================
//

app.UseStaticFiles();


//
// ============================================================
// OPENAPI
// ============================================================
//

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


//
// ============================================================
// ROUTING
// ============================================================
//

app.UseRouting();


//
// ============================================================
// AUTHENTICATION
// ============================================================

app.UseAuthentication();


//
// ============================================================
// AUTHORIZATION
// ============================================================
//

app.UseAuthorization();


//
// ============================================================
// API CONTROLLERS
// ============================================================
//

app.MapControllers();


//
// ============================================================
// MVC ROUTE
// ============================================================
//

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}");


//
// ============================================================
// RUN APPLICATION
// ============================================================
//

app.Run();


//
// ============================================================
// PROGRAM CLASS
// ============================================================
//

public partial class Program
{
}