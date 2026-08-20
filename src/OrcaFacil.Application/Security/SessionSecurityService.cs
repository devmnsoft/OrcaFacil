using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Security;

public sealed class SessionSecurityService(IRepository<SessionRecord> sessions, IUnitOfWork unitOfWork, IAuditService audit)
{
    public IQueryable<SessionRecord> ForUser(Guid accountId, Guid userId) => sessions.Query()
        .Where(x => x.AccountId == accountId && x.UserId == userId && !x.IsDeleted);

    public async Task RevokeAsync(Guid accountId, Guid actorUserId, Guid sessionId, bool canManageAccountSessions,
        CancellationToken ct = default)
    {
        var session = await sessions.GetAsync(sessionId, ct);
        if (session is null || session.AccountId != accountId || (session.UserId != actorUserId && !canManageAccountSessions))
            throw new UnauthorizedAccessException("Não é permitido encerrar esta sessão.");
        if (session.RevokedAt is null) { session.RevokedAt = DateTime.UtcNow; session.Touch(); }
        await audit.RegisterAsync(actorUserId, "Security.SessionRevoked", nameof(SessionRecord), session.Id.ToString(),
            null, new { Revoked = true }, new { TargetUserId = session.UserId }, ct, accountId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
