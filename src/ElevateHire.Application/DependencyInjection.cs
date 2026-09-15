using ElevateWorkforce.Application.Interfaces;
using ElevateWorkforce.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ElevateWorkforce.Application;

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