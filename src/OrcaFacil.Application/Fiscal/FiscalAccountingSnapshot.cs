namespace OrcaFacil.Application.Fiscal;

/// <summary>
/// One <see cref="OrcaFacil.Domain.Entities.FiscalDocumentRequest"/> that was actually ISSUED in audited manual control,
/// shaped into fields for the accountant package. Carries only what the operator recorded; nothing is fabricated — a
/// missing number is flagged rather than invented, retention stays zero (manual control tracks none), and a missing
/// client/provider fall back to safe defaults. Kept free of the DbContext so the mapping is unit-testable.
/// </summary>
public sealed record FiscalAccountingSource(
    Guid AccountId,
    Guid? ClientId,
    string? Number,
    DateTime IssuedAtUtc,
    string? ServiceDescription,
    string? DocumentType,
    decimal? GrossAmount,
    string? ProviderName,
    string? Protocol);

/// <summary>Pure mapping from an issued manual-control request onto <see cref="FiscalDocumentSnapshot"/>.</summary>
public static class FiscalAccountingSnapshotMapper
{
    /// <summary>Shown when the operator did not record a document number — never fabricated.</summary>
    public const string NoNumber = "(sem número)";

    /// <summary>Origin stamped when no provider name is present (matches the manual-control provider).</summary>
    public const string DefaultProvider = "Controle manual auditado";

    /// <summary>Service label when neither the receivable description nor a document type is available.</summary>
    public const string DefaultService = "Serviço";

    /// <summary>Only notes actually issued belong in the accountant package (open/canceled/not-required are excluded).</summary>
    public static bool ShouldExport(OrcaFacil.Domain.Entities.FiscalRequestStatus status) =>
        status == OrcaFacil.Domain.Entities.FiscalRequestStatus.Issued;

    public static FiscalDocumentSnapshot ToSnapshot(FiscalAccountingSource source) => new(
        source.AccountId,
        source.ClientId ?? Guid.Empty,
        string.IsNullOrWhiteSpace(source.Number) ? NoNumber : source.Number!.Trim(),
        new DateTimeOffset(DateTime.SpecifyKind(source.IssuedAtUtc, DateTimeKind.Utc), TimeSpan.Zero),
        ServiceFor(source.ServiceDescription, source.DocumentType),
        source.GrossAmount ?? 0m,
        0m, // manual control carries no retention data; the accountant reconciles downstream
        FiscalDocumentStatus.ManualRegistered, // manually registered, never a municipality authorization
        string.IsNullOrWhiteSpace(source.ProviderName) ? DefaultProvider : source.ProviderName!.Trim(),
        string.IsNullOrWhiteSpace(source.Protocol) ? null : source.Protocol!.Trim());

    private static string ServiceFor(string? description, string? documentType) =>
        !string.IsNullOrWhiteSpace(description) ? description!.Trim()
        : (!string.IsNullOrWhiteSpace(documentType) ? documentType!.Trim() : DefaultService);
}
