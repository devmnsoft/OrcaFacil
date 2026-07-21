using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Infrastructure;

public static class SuperAdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SuperAdminSeeder");
        var email = Environment.GetEnvironmentVariable("ORCAFACIL_ADMIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("ORCAFACIL_ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("SuperAdmin seed ignorado: variáveis ORCAFACIL_ADMIN_EMAIL/ORCAFACIL_ADMIN_PASSWORD não configuradas.");
            return;
        }

        var users = scope.ServiceProvider.GetRequiredService<IRepository<UserAccount>>();
        if (users.Query().Any(user => user.Email == email.Trim().ToLowerInvariant())) return;
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await users.AddAsync(new UserAccount { Name = "SuperAdmin", Email = email.Trim().ToLowerInvariant(), PasswordHash = hasher.Hash(password), Role = UserRole.SuperAdmin, Plan = PlanType.Pro, IsActive = true }, ct);
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("SuperAdmin criado via seed opcional.");
    }
}
