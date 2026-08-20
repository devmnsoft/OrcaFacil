using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Security;

public sealed class SensitiveDataAccessService(IRepository<SensitiveDataAccessLog> logs, IUnitOfWork unitOfWork)
{
    public async Task RegisterAsync(Guid accountId, Guid userId, string entityType, Guid entityId, string accessType,
        string reason, string ipAddress, string userAgent, Guid correlationId, CancellationToken ct = default)
    {
        if (reason.Contains('@') || reason.Any(char.IsDigit) && reason.Count(char.IsDigit) > 5)
            throw new ArgumentException("O motivo não pode conter o valor do dado sensível.", nameof(reason));
        await logs.AddAsync(new SensitiveDataAccessLog { AccountId = accountId, UserId = userId, EntityType = entityType,
            EntityId = entityId, AccessType = accessType, Reason = reason, IpAddress = ipAddress,
            UserAgent = userAgent, CorrelationId = correlationId }, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
