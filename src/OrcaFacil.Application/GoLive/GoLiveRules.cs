using System.Security.Cryptography;

namespace OrcaFacil.Application.GoLive;

public static class Sprint32Permissions
{
    public static readonly string[] All = [
        "TenantProvisioning.View", "TenantProvisioning.Manage", "Implementation.View", "Implementation.Manage",
        "DataMigration.View", "DataMigration.Manage", "DemoAccounts.View", "DemoAccounts.Manage",
        "SalesDemos.View", "SalesDemos.Manage", "Training.View", "Training.Manage", "Training.TrackProgress",
        "Readiness.View", "Readiness.Manage", "Adoption.View", "CustomerSuccess.AssistedOperation",
        "GoLiveReview.View", "GoLiveReview.Manage", "ImplementationReports.View"
    ];
}

public sealed record ProvisioningRequest(string AccountName, string? DocumentNumber, string OwnerEmail, int TrialDays);
public sealed record ProvisioningResult(Guid AccountId, Guid OwnerId, string InvitationToken);

/// <summary>Critical, persistence-independent rules used by the transactional provisioning workflow.</summary>
public sealed class TenantProvisioningService
{
    public ProvisioningResult Prepare(ProvisioningRequest request, bool duplicateConfirmed,
        bool documentAlreadyExists, bool ownerEmailAlreadyExists)
    {
        if (string.IsNullOrWhiteSpace(request.AccountName)) throw new ArgumentException("A conta é obrigatória.");
        if (!request.OwnerEmail.Contains('@', StringComparison.Ordinal)) throw new ArgumentException("E-mail do owner inválido.");
        if (request.TrialDays is < 0 or > 90) throw new ArgumentOutOfRangeException(nameof(request.TrialDays));
        if ((documentAlreadyExists || ownerEmailAlreadyExists) && !duplicateConfirmed)
            throw new InvalidOperationException("A duplicidade precisa ser confirmada antes do provisionamento.");

        return new ProvisioningResult(Guid.NewGuid(), Guid.NewGuid(),
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    }
}

public sealed record LaunchItem(bool Blocking, bool Completed, Guid? CompletedBy, DateTime? CompletedAt);

public sealed class TenantLaunchChecklistService
{
    public bool CanGoLive(IEnumerable<LaunchItem> items) => items.All(x => !x.Blocking || x.Completed);

    public LaunchItem CompleteManual(LaunchItem item, Guid userId, DateTime completedAt)
    {
        if (userId == Guid.Empty) throw new ArgumentException("Usuário é obrigatório.", nameof(userId));
        return item with { Completed = true, CompletedBy = userId, CompletedAt = completedAt };
    }
}

public sealed record ReadinessCriterion(string Code, bool Satisfied, int Weight, string RecommendedAction);
public sealed record ReadinessResult(int Score, IReadOnlyList<ReadinessCriterion> Findings);

public sealed class AccountReadinessService
{
    public ReadinessResult Calculate(IEnumerable<ReadinessCriterion> source)
    {
        var criteria = source.ToArray();
        var total = criteria.Sum(x => Math.Max(0, x.Weight));
        var achieved = criteria.Where(x => x.Satisfied).Sum(x => Math.Max(0, x.Weight));
        var score = total == 0 ? 0 : (int)Math.Round(100m * achieved / total, MidpointRounding.AwayFromZero);
        return new ReadinessResult(score, criteria.Where(x => !x.Satisfied).ToArray());
    }
}

public sealed record MigrationPreview(Guid BatchId, Guid AccountId, int ValidRows, int InvalidRows, bool Confirmed = false);

public sealed class CustomerMigrationService
{
    public MigrationPreview ConfirmImport(MigrationPreview preview, Guid accountId)
    {
        if (preview.AccountId != accountId) throw new UnauthorizedAccessException("A migração pertence a outra conta.");
        if (preview.ValidRows <= 0) throw new InvalidOperationException("Não há linhas válidas na prévia.");
        return preview with { Confirmed = true };
    }

    public static string NormalizeCsvFileName(string fileName)
    {
        var safe = Path.GetFileName(fileName);
        if (!safe.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Somente arquivos CSV são aceitos.");
        return safe;
    }
}

public sealed record DemoPolicy(bool BlockEmail = true, bool BlockWebhook = true, bool BlockPayment = true, bool BlockFiscal = true);
public sealed class DemoAccountService
{
    public DemoPolicy CreateSafePolicy() => new();
    public void ValidateReset(Guid demoAccountId, Guid targetAccountId, bool targetIsDemo)
    {
        if (!targetIsDemo || demoAccountId != targetAccountId)
            throw new InvalidOperationException("O reset é restrito à própria conta de demonstração.");
    }
}

public sealed class GoLiveReviewService
{
    public void Approve(Guid authorizedUserId, bool checklistComplete)
    {
        if (authorizedUserId == Guid.Empty) throw new UnauthorizedAccessException("Aprovação exige usuário autorizado.");
        if (!checklistComplete) throw new InvalidOperationException("Itens bloqueantes impedem o go-live.");
    }

    public void Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A rejeição exige motivo.", nameof(reason));
    }
}
