using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Persistence.Diagnostics;

namespace OrcaFacil.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.TryAddScoped<IDatabaseSchemaContractService, DatabaseSchemaContractService>();

        return services;
    }
}
