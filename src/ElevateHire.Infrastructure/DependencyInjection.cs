using ElevateHire.Application.Interfaces;
using ElevateHire.Application.Services;
using ElevateHire.Infrastructure.AI;
using ElevateHire.Infrastructure.Caching;
using ElevateHire.Infrastructure.Data;
using ElevateHire.Infrastructure.Email;
using ElevateHire.Infrastructure.Repositories;
using ElevateHire.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElevateHire.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings__DefaultConnection"]
            ?? "Host=localhost;Port=5432;Database=elevatehire;Username=elevatehire;Password=elevatehire";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        var redisConnection = configuration.GetConnectionString("Redis")
            ?? configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnection))
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
        else
            services.AddDistributedMemoryCache();

        services.AddSingleton<ICacheService, RedisCacheService>();
        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.AddScoped<IEmailService, EmailService>();

        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IJobSeekerRepository>(sp => sp.GetRequiredService<IUserRepository>());
        services.AddScoped<IEmployerRepository, EmployerRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        var storageRoot = configuration["FileStorage:RootPath"] ?? "wwwroot/uploads";
        services.AddSingleton<IFileStorageService>(
            new FileStorageService(new FileStorageOptions { RootPath = storageRoot }));

        services.Configure<GeminiOptions>(configuration.GetSection("Gemini"));
        services.AddHttpClient<GeminiService>();
        services.AddScoped<IAIService>(sp =>
            new AIService(sp.GetRequiredService<GeminiService>()));

        return services;
    }
}