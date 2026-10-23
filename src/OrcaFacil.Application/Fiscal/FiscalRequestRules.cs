using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Fiscal;

/// <summary>
/// Pure lifecycle rules for manually fiscalizing a client receivable via <see cref="FiscalDocumentRequest"/>.
/// The operator stays in control: the system tracks and audits which received amounts are fiscalized but never
/// fabricates a number or an access key — those are recorded by whoever actually issued the note. Free of the
/// DbContext so the transition/idempotency decisions are unit-testable, like the other rule sets in the repo.
/// </summary>
public static class FiscalRequestRules
{
    /// <summary>A receivable is fiscalizable once some money was actually received and it is not canceled.</summary>
    public static bool IsFiscalizable(decimal paidAmount, bool canceled) => !canceled && paidAmount > 0m;

    /// <summary>An open request blocks a new one for the same origin (requesting again is a no-op replay).</summary>
    public static bool IsActive(FiscalRequestStatus status) => status is FiscalRequestStatus.Pending or FiscalRequestStatus.InReview;

    /// <summary>Settled requests no longer move; re-fiscalizing means opening a fresh request.</summary>
    public static bool IsSettled(FiscalRequestStatus status) =>
        status is FiscalRequestStatus.Issued or FiscalRequestStatus.Canceled or FiscalRequestStatus.NotRequired or FiscalRequestStatus.Rejected;

    /// <summary>The allowed moves of a manual fiscalization request. Terminal states (except issued-cancel) are final.</summary>
    public static bool CanTransition(FiscalRequestStatus from, FiscalRequestStatus to) => (from, to) switch
    {
        (FiscalRequestStatus.Pending, FiscalRequestStatus.Issued) => true,
        (FiscalRequestStatus.Pending, FiscalRequestStatus.Canceled) => true,
        (FiscalRequestStatus.Pending, FiscalRequestStatus.NotRequired) => true,
        (FiscalRequestStatus.InReview, FiscalRequestStatus.Issued) => true,
        (FiscalRequestStatus.InReview, FiscalRequestStatus.Canceled) => true,
        (FiscalRequestStatus.InReview, FiscalRequestStatus.NotRequired) => true,
        (FiscalRequestStatus.Issued, FiscalRequestStatus.Canceled) => true,
        _ => false,
    };

    /// <summary>Leaving an open state for cancel/not-applicable needs an auditable reason.</summary>
    public static bool RequiresReason(FiscalRequestStatus to) => to is FiscalRequestStatus.Canceled or FiscalRequestStatus.NotRequired;

    /// <summary>The issued document must carry the real number the operator sees on the note.</summary>
    public static bool IsValidExternalNumber(string? number) => !string.IsNullOrWhiteSpace(number);

    /// <summary>Keeps an access key auditable without persisting it in full — shows only the ends.</summary>
    public static string MaskAccessKey(string? key)
    {
        var k = (key ?? string.Empty).Trim();
        return k.Length <= 8 ? k : $"{k[..4]}…{k[^4..]}";
    }
}
