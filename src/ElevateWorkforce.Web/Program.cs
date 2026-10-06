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
// Application services
//
builder.Services.AddApplication();

//
// Infrastructure services
//
builder.Services.AddInfrastructure(builder.Configuration);

//
// JWT configuration
//
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));

//
// Identity
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
// JWT validation
//
var jwtOptions = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "Jwt configuration is required.");

if (string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    throw new InvalidOperationException(
        "Jwt:Key is required.");
}

if (Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must contain at least 32 characters.");
}

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
// Application cookie
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
// HTTP context
//
builder.Services.AddHttpContextAccessor();

//
// MVC
//
builder.Services.AddControllersWithViews();

//
// API / OpenAPI
//
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

//
// Localization
//
builder.Services.Configure<RequestLocalizationOptions>(
    options =>
    {
        options.DefaultRequestCulture =
            new Microsoft.AspNetCore.Localization.RequestCulture("en");
    });

var app = builder.Build();

//
// Database initialization
//
// The application automatically:
// 1. Connects to PostgreSQL
// 2. Applies EF Core migrations
// 3. Seeds demo data
//
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var logger =
        services.GetRequiredService<
            ILogger<Program>>();

    var db =
        services.GetRequiredService<
            ApplicationDbContext>();

    try
    {
        await db.Database.MigrateAsync();

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
// Production error handling
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
// Static files
//
app.UseStaticFiles();

//
// OpenAPI only in Development
//
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//
// Routing
//
app.UseRouting();

//
// Authentication
//
app.UseAuthentication();

//
// Authorization
//
app.UseAuthorization();

//
// API controllers
//
app.MapControllers();

//
// MVC routes
//
app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program
{
}