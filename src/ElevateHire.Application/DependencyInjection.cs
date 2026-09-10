using ElevateHire.Application.Interfaces;
using ElevateHire.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ElevateHire.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICompanyService, CompanyService>();

        return services;
    }
}