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

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));

var jwtOptions = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtOptions>();

if (jwtOptions == null)
{
    throw new InvalidOperationException(
        "Jwt configuration is required.");
}

if (string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    throw new InvalidOperationException(
        "Jwt:Key is missing. " +
        "Please add Jwt__Key to the Render environment variables.");
}

if (Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must contain at least 32 characters.");
}


//
// ============================================================
// IDENTITY
// ============================================================
//

builder.Services.AddIdentity<User, ApplicationRole>(options =>
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


//
// ============================================================
// JWT AUTHENTICATION
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

    options.AccessDeniedPath =
        "/account/accessdenied";

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
        services.GetRequiredService<
            ILogger<Program>>();

    try
    {
        var db =
            services.GetRequiredService<
                ApplicationDbContext>();

        //
        // Apply Entity Framework Core migrations
        //
        await db.Database.MigrateAsync();

        //
        // Seed initial/demo data
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
//

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
// MVC ROUTES
// ============================================================
//

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


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