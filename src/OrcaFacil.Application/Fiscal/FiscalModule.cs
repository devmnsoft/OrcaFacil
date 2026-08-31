using System.Globalization;
using System.Text;
using System.Text.Json;

namespace OrcaFacil.Application.Fiscal;

public enum FiscalProviderState { NotConfigured, Configured, Healthy, Degraded, Failed, Disabled }
public enum FiscalDocumentStatus { Draft, ReadyToIssue, Queued, Sent, Processing, Authorized, Rejected, Failed, Canceled, CorrectionRequested, Substituted, ManualRegistered }
public enum FiscalCompanyReadiness { Incomplete, ReadyForManualControl, ReadyForSandbox, ReadyForProduction, Blocked }
public enum FiscalCertificateStatus { NotConfigured, Configured, Valid, ExpiringSoon, Expired, Invalid, ManualRequired }
public enum FiscalOperationMode { Online, Manual }

public sealed record FiscalProviderResult(FiscalProviderState State, string Message, string? ProviderReference = null, string? Protocol = null)
{
    public bool HasRealAuthorization => State == FiscalProviderState.Healthy && !string.IsNullOrWhiteSpace(ProviderReference) && !string.IsNullOrWhiteSpace(Protocol);
}
public sealed record FiscalIssueContext(Guid AccountId, Guid CustomerId, string OriginType, Guid OriginId, decimal GrossAmount,
    bool CompanyReady, bool CustomerReady, bool ServicesConfigured, bool CertificateValid, bool ProviderConfigured,
    FiscalOperationMode Mode, bool Confirmed, string? ManualJustification = null);
public sealed record FiscalDocumentSnapshot(Guid AccountId, Guid CustomerId, string Number, DateTimeOffset IssuedAt,
    string Service, decimal GrossAmount, decimal Retentions, FiscalDocumentStatus Status, string Origin, string? Protocol);
public sealed record FiscalWebhookEnvelope(string Provider, string EventId, string EventType, string Payload, string? Signature);

public interface IFiscalProvider { string Name { get; } Task<FiscalProviderResult> HealthAsync(CancellationToken cancellationToken = default); }
public interface INfseProvider : IFiscalProvider, IFiscalDocumentIssuer, IFiscalDocumentStatusProvider, IFiscalDocumentCancellationProvider, IFiscalDocumentPdfProvider, IFiscalDocumentXmlProvider { }
public interface IFiscalDocumentIssuer { Task<FiscalProviderResult> IssueAsync(FiscalIssueContext request, CancellationToken cancellationToken = default); }
public interface IFiscalDocumentStatusProvider { Task<FiscalProviderResult> QueryStatusAsync(string providerReference, CancellationToken cancellationToken = default); }
public interface IFiscalDocumentCancellationProvider { Task<FiscalProviderResult> CancelAsync(string providerReference, string reason, CancellationToken cancellationToken = default); }
public interface IFiscalDocumentPdfProvider { Task<Stream?> DownloadPdfAsync(string providerReference, CancellationToken cancellationToken = default); }
public interface IFiscalDocumentXmlProvider { Task<Stream?> DownloadXmlAsync(string providerReference, CancellationToken cancellationToken = default); }
public interface IFiscalWebhookVerifier { bool Verify(ReadOnlySpan<byte> payload, string? signature); }
public interface IFiscalCertificateStore
{
    Task StoreProtectedAsync(Guid accountId, Stream pfx, string password, CancellationToken cancellationToken = default);
    Task<FiscalCertificateStatus> GetStatusAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid accountId, CancellationToken cancellationToken = default);
}

public static class FiscalRules
{
    public const string NotConfiguredMessage = "Emissão fiscal automática não está configurada. Use apenas controle manual auditado ou configure um provedor fiscal real.";
    public const string AccountingDisclaimer = "As regras fiscais e retenções devem ser conferidas pela contabilidade da empresa. O sistema aplica os parâmetros configurados e mantém auditoria.";

    public static void ValidateIssue(FiscalIssueContext request)
    {
        if (request.AccountId == Guid.Empty || request.CustomerId == Guid.Empty || request.OriginId == Guid.Empty) throw new ArgumentException("Conta, cliente e origem válidos são obrigatórios.");
        if (request.GrossAmount <= 0) throw new ArgumentOutOfRangeException(nameof(request.GrossAmount), "O valor fiscal deve ser positivo.");
        if (!request.CompanyReady) throw new InvalidOperationException("Complete o perfil fiscal da empresa antes de emitir.");
        if (!request.CustomerReady) throw new InvalidOperationException("Complete os dados fiscais do cliente antes de emitir.");
        if (!request.ServicesConfigured) throw new InvalidOperationException("Configure o código fiscal de todos os serviços antes de emitir.");
        if (!request.Confirmed) throw new InvalidOperationException("Revise a prévia e confirme explicitamente a emissão.");
        if (request.Mode == FiscalOperationMode.Online && (!request.ProviderConfigured || !request.CertificateValid)) throw new InvalidOperationException(NotConfiguredMessage);
        if (request.Mode == FiscalOperationMode.Manual && string.IsNullOrWhiteSpace(request.ManualJustification)) throw new InvalidOperationException("O controle manual exige justificativa auditável.");
    }

    public static decimal CalculateNet(decimal gross, params decimal[] retentionPercentages)
    {
        if (gross < 0 || retentionPercentages.Any(x => x < 0 || x > 100)) throw new ArgumentOutOfRangeException(nameof(retentionPercentages));
        var retained = retentionPercentages.Sum(x => decimal.Round(gross * x / 100m, 2, MidpointRounding.AwayFromZero));
        if (retained > gross) throw new InvalidOperationException("As retenções não podem tornar o valor líquido negativo.");
        return gross - retained;
    }

    public static void EnsureMutable(FiscalDocumentStatus status)
    {
        if (status is FiscalDocumentStatus.Authorized or FiscalDocumentStatus.Canceled or FiscalDocumentStatus.Substituted or FiscalDocumentStatus.ManualRegistered)
            throw new InvalidOperationException("O documento fiscal consolidado não pode ser editado diretamente.");
    }
    public static void RequireReason(string? reason, string operation)
    { if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10) throw new ArgumentException($"Informe um motivo auditável para {operation}.", nameof(reason)); }
    public static void EnsureSameAccount(Guid expected, Guid actual)
    { if (expected == Guid.Empty || expected != actual) throw new UnauthorizedAccessException("O recurso fiscal não pertence à conta atual."); }
    public static bool IsStatusQueryEligible(FiscalDocumentStatus status, bool isManual) => !isManual && status is FiscalDocumentStatus.Sent or FiscalDocumentStatus.Processing;
}

public sealed class FiscalIssueRequestService
{
    public FiscalDocumentStatus ValidatePreview(FiscalIssueContext request) { FiscalRules.ValidateIssue(request); return request.Mode == FiscalOperationMode.Manual ? FiscalDocumentStatus.ManualRegistered : FiscalDocumentStatus.ReadyToIssue; }
}
public sealed class FiscalCompanyProfileService
{
    public FiscalCompanyReadiness Evaluate(string? legalName, string? taxDocument, string? city, string? state, bool productionConfirmed)
    {
        if (new[] { legalName, taxDocument, city, state }.Any(string.IsNullOrWhiteSpace)) return FiscalCompanyReadiness.Incomplete;
        var digits = new string((taxDocument ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length is not (11 or 14) || state!.Trim().Length != 2) return FiscalCompanyReadiness.Blocked;
        return productionConfirmed ? FiscalCompanyReadiness.ReadyForProduction : FiscalCompanyReadiness.ReadyForManualControl;
    }
}
public sealed class FiscalCertificateService
{
    public FiscalCertificateStatus Evaluate(DateTimeOffset? validUntil, bool configured, DateTimeOffset now)
    {
        if (!configured) return FiscalCertificateStatus.NotConfigured;
        if (validUntil is null) return FiscalCertificateStatus.Invalid;
        if (validUntil <= now) return FiscalCertificateStatus.Expired;
        return validUntil <= now.AddDays(30) ? FiscalCertificateStatus.ExpiringSoon : FiscalCertificateStatus.Valid;
    }
}
public sealed class FiscalProviderSettingsService { public string Mask(string? secret) => string.IsNullOrWhiteSpace(secret) ? "Não configurado" : $"••••{secret[^Math.Min(4, secret.Length)..]}"; }
public sealed class FiscalServiceCodeService { public void Validate(string? description, decimal rate) { if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Descrição obrigatória."); if (rate < 0) throw new ArgumentOutOfRangeException(nameof(rate)); } }
public sealed class FiscalCustomerProfileService { public bool IsReady(string? document, string? address, string? city, string? state) { var digits=new string((document??"").Where(char.IsDigit).ToArray()); return digits.Length is 11 or 14 && !string.IsNullOrWhiteSpace(address) && !string.IsNullOrWhiteSpace(city) && state?.Trim().Length==2; } }
public sealed class FiscalTaxProfileService { public decimal ValidateRate(decimal rate) => rate is < 0 or > 100 ? throw new ArgumentOutOfRangeException(nameof(rate)) : rate; }
public sealed class FiscalRpsService { public void Validate(string? series, long number) { if(string.IsNullOrWhiteSpace(series)||number<=0) throw new ArgumentException("Série e número de RPS válidos são obrigatórios."); } }
public sealed class FiscalRpsBatchService { public IReadOnlyList<string> FindDuplicates(IEnumerable<(string Series,long Number)> items) => items.GroupBy(x=>$"{x.Series.Trim().ToUpperInvariant()}:{x.Number}").Where(x=>x.Count()>1).Select(x=>x.Key).ToArray(); }
public sealed class FiscalDocumentFileService { public void AuthorizeDownload(Guid accountId, Guid fileAccountId, bool hasPermission) { FiscalRules.EnsureSameAccount(accountId,fileAccountId); if(!hasPermission) throw new UnauthorizedAccessException("Permissão de download fiscal obrigatória."); } }
public sealed class FiscalHealthService { public IReadOnlyList<string> Diagnose(bool companyReady,bool certificateValid,bool providerHealthy) { var warnings=new List<string>(); if(!companyReady)warnings.Add("Perfil fiscal incompleto"); if(!certificateValid)warnings.Add("Certificado ausente, inválido ou vencido"); if(!providerHealthy)warnings.Add(FiscalRules.NotConfiguredMessage); return warnings; } }
public sealed class FiscalReportService { public IReadOnlyDictionary<FiscalDocumentStatus,int> ByStatus(Guid accountId,IEnumerable<FiscalDocumentSnapshot> rows)=>rows.Where(x=>x.AccountId==accountId).GroupBy(x=>x.Status).ToDictionary(x=>x.Key,x=>x.Count()); }
public sealed class FiscalRetentionService { public decimal CalculateNet(decimal gross, params decimal[] percentages) => FiscalRules.CalculateNet(gross, percentages); }
public sealed class NfseService
{
    public void EnsureEditable(FiscalDocumentStatus status) => FiscalRules.EnsureMutable(status);
    public FiscalDocumentStatus RegisterProviderAuthorization(FiscalProviderResult result)
    { if (!result.HasRealAuthorization) throw new InvalidOperationException("A autorização exige identificador e protocolo reais do provedor."); return FiscalDocumentStatus.Authorized; }
    public FiscalDocumentStatus RegisterManual(string? justification) { FiscalRules.RequireReason(justification, "o registro manual"); return FiscalDocumentStatus.ManualRegistered; }
}
public sealed class FiscalDocumentCancellationService { public void Validate(Guid accountId, Guid documentAccountId, string? reason) { FiscalRules.EnsureSameAccount(accountId, documentAccountId); FiscalRules.RequireReason(reason, "o cancelamento"); } }
public sealed class FiscalDocumentCorrectionService { public void Validate(Guid accountId, Guid documentAccountId, string? reason) { FiscalRules.EnsureSameAccount(accountId, documentAccountId); FiscalRules.RequireReason(reason, "a correção"); } }
public sealed class FiscalDocumentSubstitutionService { public void Validate(Guid accountId, Guid originalAccountId, Guid originalDocumentId) { FiscalRules.EnsureSameAccount(accountId, originalAccountId); if (originalDocumentId == Guid.Empty) throw new ArgumentException("Documento original obrigatório."); } }
public sealed class FiscalDocumentStatusQueryService { public bool ShouldQuery(FiscalDocumentStatus status, bool isManual) => FiscalRules.IsStatusQueryEligible(status, isManual); }

public sealed class FiscalWebhookService
{
    private readonly HashSet<string> processed = new(StringComparer.Ordinal);
    public bool Accept(FiscalWebhookEnvelope envelope, IFiscalWebhookVerifier verifier)
    {
        if (string.IsNullOrWhiteSpace(envelope.Provider) || string.IsNullOrWhiteSpace(envelope.EventId)) return false;
        var payload = Encoding.UTF8.GetBytes(envelope.Payload);
        if (!verifier.Verify(payload, envelope.Signature)) return false;
        lock (processed) return processed.Add($"{envelope.Provider}:{envelope.EventId}");
    }
}
public sealed class FiscalAccountingExportService
{
    public byte[] ExportCsv(Guid accountId, IEnumerable<FiscalDocumentSnapshot> documents)
    {
        var rows = documents.Where(x => x.AccountId == accountId).OrderBy(x => x.IssuedAt).ToArray();
        var csv = new StringBuilder("documento,cliente,data_emissao,servico,valor_bruto,retencoes,valor_liquido,status,origem,protocolo\n");
        foreach (var x in rows) csv.AppendLine(string.Join(',', Q(x.Number), Q(x.CustomerId), Q(x.IssuedAt.ToString("O")), Q(x.Service), x.GrossAmount.ToString(CultureInfo.InvariantCulture), x.Retentions.ToString(CultureInfo.InvariantCulture), (x.GrossAmount-x.Retentions).ToString(CultureInfo.InvariantCulture), Q(x.Status), Q(x.Origin), Q(x.Protocol)));
        return new UTF8Encoding(true).GetBytes(csv.ToString());
    }
    public byte[] ExportJson(Guid accountId, IEnumerable<FiscalDocumentSnapshot> documents) => JsonSerializer.SerializeToUtf8Bytes(documents.Where(x => x.AccountId == accountId));
    private static string Q(object? value) => $"\"{value?.ToString()?.Replace("\"", "\"\"")}\"";
}
