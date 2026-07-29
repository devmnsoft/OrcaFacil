using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Documents;

public enum QuoteLifecycleCode
{
    None, DocumentNotFound, AccessDenied, InvalidStatus, InvalidDocument, NoItems,
    RevisionAlreadyExists, PublicLinkUnavailable, PublicLinkExpired, PublicLinkRevoked,
    DecisionAlreadyRegistered, VersionOutdated, PlanLimitReached, ConcurrencyConflict, Unexpected
}

public record QuoteLifecycleResult(bool Succeeded, QuoteLifecycleCode Code, string Message, Guid? DocumentId = null,
    Guid? RevisionId = null, Guid? PublicAccessId = null, DocumentStatus? CurrentStatus = null, string? CorrelationId = null);
public sealed record PublicQuoteResult(bool Succeeded, QuoteLifecycleCode Code, string Message, Guid? DocumentId = null,
    Guid? RevisionId = null, Guid? PublicAccessId = null, DocumentStatus? CurrentStatus = null, string? CorrelationId = null, string? PublicToken = null);
public sealed record PublicDecisionResult(bool Succeeded, QuoteLifecycleCode Code, string Message, Guid? DocumentId = null,
    Guid? RevisionId = null, Guid? PublicAccessId = null, DocumentStatus? CurrentStatus = null, string? CorrelationId = null, Guid? DecisionId = null);
public sealed record RevisionResult(bool Succeeded, QuoteLifecycleCode Code, string Message, Guid? DocumentId = null,
    Guid? RevisionId = null, Guid? PublicAccessId = null, DocumentStatus? CurrentStatus = null, string? CorrelationId = null,
    int? VersionNumber = null, string? SnapshotHash = null, bool Reused = false);
