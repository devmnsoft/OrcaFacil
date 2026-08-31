using Microsoft.AspNetCore.DataProtection;
using OrcaFacil.Application.Fiscal;

namespace OrcaFacil.Infrastructure.Fiscal;

public sealed class NoopFiscalProvider : INfseProvider
{
    public string Name => "Não configurado";
    private static FiscalProviderResult Result() => new(FiscalProviderState.NotConfigured, FiscalRules.NotConfiguredMessage);
    public Task<FiscalProviderResult> HealthAsync(CancellationToken cancellationToken = default) => Task.FromResult(Result());
    public Task<FiscalProviderResult> IssueAsync(FiscalIssueContext request, CancellationToken cancellationToken = default) => Task.FromResult(Result());
    public Task<FiscalProviderResult> QueryStatusAsync(string providerReference, CancellationToken cancellationToken = default) => Task.FromResult(Result());
    public Task<FiscalProviderResult> CancelAsync(string providerReference, string reason, CancellationToken cancellationToken = default) => Task.FromResult(Result());
    public Task<Stream?> DownloadPdfAsync(string providerReference, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null);
    public Task<Stream?> DownloadXmlAsync(string providerReference, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null);
}

public sealed class ManualFiscalProvider : IFiscalProvider
{
    public string Name => "Controle manual auditado";
    public Task<FiscalProviderResult> HealthAsync(CancellationToken cancellationToken = default) => Task.FromResult(new FiscalProviderResult(FiscalProviderState.Configured, "Controle manual auditado; não representa autorização de prefeitura."));
}

public sealed class ProtectedFiscalCertificateStore(IDataProtectionProvider protection) : IFiscalCertificateStore
{
    private readonly IDataProtector protector = protection.CreateProtector("OrcaFacil.Fiscal.Certificate.v1");
    private readonly Dictionary<Guid, byte[]> protectedCertificates = [];
    public async Task StoreProtectedAsync(Guid accountId, Stream pfx, string password, CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty || string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Conta e senha do certificado são obrigatórias.");
        using var buffer = new MemoryStream(); await pfx.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length == 0 || buffer.Length > 10 * 1024 * 1024) throw new InvalidOperationException("Certificado vazio ou acima do limite de 10 MB.");
        var secret = Convert.ToBase64String(buffer.ToArray()) + "\n" + password;
        protectedCertificates[accountId] = protector.Protect(System.Text.Encoding.UTF8.GetBytes(secret));
    }
    public Task<FiscalCertificateStatus> GetStatusAsync(Guid accountId, CancellationToken cancellationToken = default) => Task.FromResult(protectedCertificates.ContainsKey(accountId) ? FiscalCertificateStatus.Configured : FiscalCertificateStatus.NotConfigured);
    public Task RemoveAsync(Guid accountId, CancellationToken cancellationToken = default) { protectedCertificates.Remove(accountId); return Task.CompletedTask; }
}
