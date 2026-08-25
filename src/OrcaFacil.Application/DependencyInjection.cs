using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrcaFacil.Application.Security;

namespace OrcaFacil.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton<ISensitiveDataSanitizer, SensitiveDataSanitizer>();

        return services;
    }
}
