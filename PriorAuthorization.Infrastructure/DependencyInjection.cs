using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PriorAuthorization.Application.Interfaces;
using PriorAuthorization.Application.Services;
using PriorAuthorization.Application.Validators;
using PriorAuthorization.Infrastructure.Data;
using PriorAuthorization.Infrastructure.External;
using PriorAuthorization.Infrastructure.Mapping;
using PriorAuthorization.Infrastructure.Queue;
using PriorAuthorization.Infrastructure.Repositories;

namespace PriorAuthorization.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPriorAuthorizationPoc(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        services.AddDbContext<PADbContext>(options => options.UseSqlServer(connectionString));

        services.AddSingleton<ICorrelationContext, CorrelationContext>();
        services.AddSingleton<IPAQueue, InMemoryPAQueue>();

        services.AddScoped<IPriorAuthorizationRepository, PriorAuthorizationRepository>();
        services.AddScoped<IPriorAuthorizationValidator, PriorAuthorizationValidator>();
        services.AddScoped<IPriorAuthorizationService, PriorAuthorizationService>();
        services.AddScoped<IPriorAuthorizationProcessor, PriorAuthorizationProcessor>();
        services.AddScoped<IPAAuditService, PAAuditService>();
        services.AddScoped<IFhirMapper, FhirMapper>();

        services.AddHttpClient<IQarClient, MockQarClient>(client =>
        {
            client.BaseAddress = new Uri("https://qar-mock.local/");
        });

        services.AddHttpClient<IServiceNowClient, MockServiceNowClient>(client =>
        {
            client.BaseAddress = new Uri("https://servicenow-mock.local/");
        });

        return services;
    }
}
