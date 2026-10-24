using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Fiscal;

/// <summary>
/// Manual fiscalization workflow over client receivables. It opens a <see cref="FiscalDocumentRequest"/> and drives it
/// through its lifecycle so the operator always knows which received amounts are fiscalized. It deliberately never
/// fabricates a fiscal number or an access key: those are recorded by whoever actually issued the note, keeping the
/// system in "audited manual control" until a real provider is configured.
/// </summary>
public interface IFiscalDocumentWorkflowService
{
    /// <summary>Opens a note request for a receivable that already received money. Idempotent while one request is open.</summary>
    Task<FiscalDocumentRequest> RequestAsync(Guid accountId, Guid actorUserId, FinancialEntry receivable, string? fiscalDocumentType, string? notes, CancellationToken ct = default);

    /// <summary>Records that the note was issued: the real number plus an optional (masked) access key and the auditable justification.</summary>
    Task<FiscalDocumentRequest> RegisterIssuanceAsync(Guid accountId, Guid actorUserId, Guid requestId, string externalNumber, string? accessKey, DateTime? issuedAt, string justification, CancellationToken ct = default);

    /// <summary>Cancels an open or issued request, with an auditable reason.</summary>
    Task<FiscalDocumentRequest> CancelAsync(Guid accountId, Guid actorUserId, Guid requestId, string reason, CancellationToken ct = default);

    /// <summary>Marks the receivable as not requiring a note, with an auditable reason.</summary>
    Task<FiscalDocumentRequest> MarkNotRequiredAsync(Guid accountId, Guid actorUserId, Guid requestId, string reason, CancellationToken ct = default);
}
