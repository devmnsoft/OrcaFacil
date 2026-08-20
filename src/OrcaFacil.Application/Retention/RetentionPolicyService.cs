using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Retention;

public sealed record RetentionPreview(Guid PolicyId, string DataType, RetentionAction Action, int MatchedRecords, bool IsDestructive);

public sealed class RetentionPolicyService(IRepository<DataRetentionPolicy> policies, IRepository<DataRetentionRun> runs,
    IUnitOfWork unitOfWork, IAuditService audit)
{
    private static readonly HashSet<string> ProtectedTypes = new(StringComparer.OrdinalIgnoreCase)
        { "receipts", "manual_payments", "payments", "audit_logs" };

    public RetentionPreview Preview(DataRetentionPolicy policy, int matchedRecords)
    {
        if (ProtectedTypes.Contains(policy.DataType) && policy.Action != RetentionAction.Keep)
            throw new InvalidOperationException("Recibos, pagamentos e auditoria crítica não podem ser removidos por retenção.");
        return new(policy.Id, policy.DataType, policy.Action, Math.Max(0, matchedRecords), policy.Action != RetentionAction.Keep);
    }

    public async Task<DataRetentionRun> RecordRunAsync(Guid accountId, Guid userId, RetentionPreview preview,
        bool simulation, string confirmation, CancellationToken ct = default)
    {
        var policy = await policies.GetAsync(preview.PolicyId, ct);
        if (policy is null || policy.AccountId != accountId) throw new UnauthorizedAccessException();
        if (!simulation && confirmation != "EXECUTAR RETENÇÃO") throw new InvalidOperationException("Confirmação forte inválida.");
        var run = new DataRetentionRun { AccountId = accountId, PolicyId = policy.Id, RequestedByUserId = userId,
            IsSimulation = simulation, MatchedRecords = preview.MatchedRecords, AffectedRecords = simulation ? 0 : preview.MatchedRecords,
            StartedAt = DateTime.UtcNow, CompletedAt = DateTime.UtcNow };
        await runs.AddAsync(run, ct);
        await audit.RegisterAsync(userId, simulation ? "Retention.Simulated" : "Retention.Executed", nameof(DataRetentionPolicy),
            policy.Id.ToString(), null, new { run.MatchedRecords, run.AffectedRecords }, new { policy.DataType, policy.Action }, ct, accountId);
        await unitOfWork.SaveChangesAsync(ct);
        return run;
    }
}
