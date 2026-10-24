using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Fiscal;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

/// <summary>
/// Drives <see cref="FiscalDocumentRequest"/> for a client receivable in "audited manual control". Every transition is
/// checked against <see cref="FiscalRequestRules"/> and the request is always account-scoped and idempotent while open.
/// The number/access key are recorded by the operator; the provider is stamped as manual control, never a real authorization.
/// </summary>
public sealed class FiscalDocumentWorkflowService(
    OrcaFacilDbContext db,
    ILogger<FiscalDocumentWorkflowService> logger) : IFiscalDocumentWorkflowService
{
    /// <summary>Matches <c>ManualFiscalProvider.Name</c>: the audited manual-control mode is not a municipality authorization.</summary>
    private const string ManualControlProvider = "Controle manual auditado";
    private const string DefaultDocumentType = "NFS-e";

    public async Task<FiscalDocumentRequest> RequestAsync(
        Guid accountId, Guid actorUserId, FinancialEntry receivable, string? fiscalDocumentType, string? notes, CancellationToken ct = default)
    {
        FiscalRules.EnsureSameAccount(accountId, receivable.AccountId);
        if (!FiscalRequestRules.IsFiscalizable(receivable.PaidAmount, receivable.Status == FinancialEntryStatus.Canceled))
            throw new InvalidOperationException("Só é possível solicitar nota para um recebível com valor já recebido.");

        // Idempotent while an open request exists for this receivable: replay instead of duplicating.
        var existing = await db.FiscalDocumentRequests.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AccountId == accountId && x.ReceivableId == receivable.Id && !x.IsDeleted, ct);
        if (existing is not null && FiscalRequestRules.IsActive(existing.Status))
        {
            logger.LogInformation("FISCAL_REQUEST_REPLAY Request {RequestId} for Receivable {ReceivableId} is already open.", existing.Id, receivable.Id);
            return existing;
        }

        var request = new FiscalDocumentRequest
        {
            AccountId = accountId,
            ClientId = receivable.ClientId,
            ReceivableId = receivable.Id,
            Status = FiscalRequestStatus.Pending,
            FiscalDocumentType = string.IsNullOrWhiteSpace(fiscalDocumentType) ? DefaultDocumentType : fiscalDocumentType.Trim(),
            RequestedAt = DateTime.UtcNow,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
        };
        db.FiscalDocumentRequests.Add(request);
        db.ActivityEvents.Add(AuditEvent(
            accountId, actorUserId, "FiscalNoteRequested", request.Id,
            $"Nota fiscal solicitada para o recebível \"{receivable.Description}\" (valor recebido {receivable.PaidAmount:C})."));
        await db.SaveChangesAsync(ct);
        return request;
    }

    public async Task<FiscalDocumentRequest> RegisterIssuanceAsync(
        Guid accountId, Guid actorUserId, Guid requestId, string externalNumber, string? accessKey, DateTime? issuedAt, string justification, CancellationToken ct = default)
    {
        if (!FiscalRequestRules.IsValidExternalNumber(externalNumber))
            throw new ArgumentException("Informe o número do documento fiscal emitido.", nameof(externalNumber));
        FiscalRules.RequireReason(justification, "o registro manual");

        var request = await LoadForAccountAsync(accountId, requestId, ct);
        if (!FiscalRequestRules.CanTransition(request.Status, FiscalRequestStatus.Issued))
            throw new InvalidOperationException($"Não é possível registrar emissão a partir do estado {request.Status}.");

        request.Status = FiscalRequestStatus.Issued;
        request.ExternalNumber = externalNumber.Trim();
        request.ExternalKeyMasked = FiscalRequestRules.MaskAccessKey(accessKey);
        request.IssuedAt = issuedAt ?? DateTime.UtcNow;
        request.ProviderName = ManualControlProvider;
        request.Touch();
        db.ActivityEvents.Add(AuditEvent(
            accountId, actorUserId, "FiscalNoteIssued", request.Id,
            $"Nota fiscal registrada manualmente: nº {request.ExternalNumber}" +
            (string.IsNullOrWhiteSpace(request.ExternalKeyMasked) ? "" : $" (chave {request.ExternalKeyMasked}).")));
        await db.SaveChangesAsync(ct);
        return request;
    }

    public async Task<FiscalDocumentRequest> CancelAsync(Guid accountId, Guid actorUserId, Guid requestId, string reason, CancellationToken ct = default)
        => await SettleAsync(accountId, actorUserId, requestId, FiscalRequestStatus.Canceled, reason, "FiscalNoteCanceled", ct);

    public async Task<FiscalDocumentRequest> MarkNotRequiredAsync(Guid accountId, Guid actorUserId, Guid requestId, string reason, CancellationToken ct = default)
        => await SettleAsync(accountId, actorUserId, requestId, FiscalRequestStatus.NotRequired, reason, "FiscalNoteNotRequired", ct);

    private async Task<FiscalDocumentRequest> SettleAsync(
        Guid accountId, Guid actorUserId, Guid requestId, FiscalRequestStatus to, string reason, string action, CancellationToken ct)
    {
        var isNotRequired = to == FiscalRequestStatus.NotRequired;
        FiscalRules.RequireReason(reason, isNotRequired ? "o registro como sem necessidade" : "o cancelamento");

        var request = await LoadForAccountAsync(accountId, requestId, ct);
        if (!FiscalRequestRules.CanTransition(request.Status, to))
            throw new InvalidOperationException($"Não é possível mover para {to} a partir do estado {request.Status}.");

        request.Status = to;
        if (to == FiscalRequestStatus.Canceled) request.CanceledAt = DateTime.UtcNow;
        request.Notes = CombineNotes(request.Notes, reason);
        request.Touch();
        db.ActivityEvents.Add(AuditEvent(
            accountId, actorUserId, action, request.Id,
            isNotRequired
                ? $"Recebível marcado como sem necessidade de nota. Motivo: {reason.Trim()}"
                : $"Nota fiscal cancelada. Motivo: {reason.Trim()}"));
        await db.SaveChangesAsync(ct);
        return request;
    }

    private static string CombineNotes(string? existing, string reason)
    {
        var trimmed = reason.Trim();
        if (string.IsNullOrWhiteSpace(existing)) return $"Motivo: {trimmed}";
        return existing.Contains("Motivo:", StringComparison.Ordinal) ? existing : $"{existing}\nMotivo: {trimmed}";
    }

    private async Task<FiscalDocumentRequest> LoadForAccountAsync(Guid accountId, Guid requestId, CancellationToken ct)
    {
        var request = await db.FiscalDocumentRequests.FirstOrDefaultAsync(
            x => x.Id == requestId && x.AccountId == accountId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Solicitação de nota fiscal não encontrada.");
        FiscalRules.EnsureSameAccount(accountId, request.AccountId);
        return request;
    }

    private static ActivityEvent AuditEvent(Guid accountId, Guid actorUserId, string action, Guid entityId, string summary) => new()
    {
        AccountId = accountId,
        ActorUserId = actorUserId,
        Action = action,
        EntityType = nameof(FiscalDocumentRequest),
        EntityId = entityId,
        Summary = summary,
    };
}
