
using ElevateWorkforce.Application;
using ElevateWorkforce.Domain.Entities;
using ElevateWorkforce.Infrastructure;
using ElevateWorkforce.Infrastructure.Data;
using ElevateWorkforce.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// APPLICATION SERVICES
// ============================================================

builder.Services.AddApplication();

// ============================================================
// INFRASTRUCTURE SERVICES
// ============================================================

builder.Services.AddInfrastructure(builder.Configuration);

// ============================================================
// JWT CONFIGURATION
// ============================================================

var jwtOptions = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtOptions>();

if (jwtOptions is null)
{
    throw new InvalidOperationException(
        "JWT configuration is missing. Configure the Jwt section.");
}

if (string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    throw new InvalidOperationException(
        "Jwt:Key is missing. Add Jwt__Key to Render environment variables.");
}

if (Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must contain at least 32 bytes.");
}

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
{
    throw new InvalidOperationException(
        "Jwt:Issuer is missing. Add Jwt__Issuer to Render environment variables.");
}

if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
{
    throw new InvalidOperationException(
        "Jwt:Audience is missing. Add Jwt__Audience to Render environment variables.");
}

// Keep JwtOptions available through dependency injection.
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));

// ============================================================
// IDENTITY
// ============================================================

builder.Services
    .AddIdentity<User, ApplicationRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;

        options.User.RequireUniqueEmail = true;

        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(10);

        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ============================================================
// JWT AUTHENTICATION
// ============================================================

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
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

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.Key)),

                ClockSkew = TimeSpan.FromMinutes(1)
            };
    });

// ============================================================
// APPLICATION COOKIE
// ============================================================

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/accessdenied";

    options.Cookie.Name = "ElevateWorkforce.Auth";
    options.SlidingExpiration = true;
});

// ============================================================
// HTTP CONTEXT
// ============================================================

builder.Services.AddHttpContextAccessor();

// ============================================================
// MVC
// ============================================================

builder.Services.AddControllersWithViews();

// ============================================================
// API / OPENAPI
// ============================================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// ============================================================
// LOCALIZATION
// ============================================================

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en");
});

// ============================================================
// BUILD APPLICATION
// ============================================================

var app = builder.Build();

// ============================================================
// DATABASE MIGRATIONS AND SEED DATA
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        await db.Database.MigrateAsync();
        await SeedData.InitializeAsync(services);

        logger.LogInformation(
            "Database migration and seed completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database initialization failed.");
        throw;
    }
}

// ============================================================
// ERROR HANDLING
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/home/error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// ============================================================
// STATIC FILES AND ROUTING
// ============================================================

app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();

// ============================================================
// AUTHENTICATION AND AUTHORIZATION
// ============================================================

app.UseAuthentication();
app.UseAuthorization();

// ============================================================
// ENDPOINTS
// ============================================================

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// ============================================================
// PROGRAM CLASS
// ============================================================

public partial class Program
{
}