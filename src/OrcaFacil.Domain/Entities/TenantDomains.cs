using System.Globalization;
using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public sealed class TenantDomain : Entity
{
    private TenantDomain() { }
    public TenantDomain(Guid accountId, string hostName, TenantDomainType type, Guid createdByUserId)
    {
        if (accountId == Guid.Empty) throw new ArgumentException("A conta é obrigatória.", nameof(accountId));
        AccountId = accountId;
        HostName = hostName;
        NormalizedHostName = NormalizeHost(hostName);
        DomainType = type;
        CreatedByUserId = createdByUserId;
        Status = TenantDomainStatus.PendingVerification;
    }

    public Guid AccountId { get; private set; }
    public string HostName { get; private set; } = string.Empty;
    public string NormalizedHostName { get; private set; } = string.Empty;
    public TenantDomainType DomainType { get; private set; }
    public TenantDomainStatus Status { get; private set; }
    public string? VerificationTokenHash { get; private set; }
    public TenantDomainVerificationMethod VerificationMethod { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public DateTime? ActivatedAt { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }
    public DateTime? LastCheckedAt { get; private set; }
    public string? LastCheckStatus { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    public void SetVerification(string tokenHash, TenantDomainVerificationMethod method)
    {
        if (string.IsNullOrWhiteSpace(tokenHash)) throw new ArgumentException("O hash do token é obrigatório.", nameof(tokenHash));
        VerificationTokenHash = tokenHash; VerificationMethod = method; Status = TenantDomainStatus.PendingVerification; Touch();
    }
    public void RecordCheck(bool verified, string result)
    {
        LastCheckedAt = DateTime.UtcNow; LastCheckStatus = result;
        if (verified) { Status = TenantDomainStatus.Verified; VerifiedAt = LastCheckedAt; }
        else Status = TenantDomainStatus.Failed;
        Touch();
    }
    public void Activate()
    {
        if (Status != TenantDomainStatus.Verified) throw new InvalidOperationException("Um domínio precisa estar verificado antes da ativação.");
        Status = TenantDomainStatus.Active; ActivatedAt = DateTime.UtcNow; DeactivatedAt = null; Touch();
    }
    public void Deactivate() { Status = TenantDomainStatus.Deactivated; DeactivatedAt = DateTime.UtcNow; Touch(); }
    public void Suspend() { Status = TenantDomainStatus.Suspended; Touch(); }
    public void Remove() { Status = TenantDomainStatus.Removed; DeactivatedAt = DateTime.UtcNow; MarkAsDeleted(); }

    public static string NormalizeHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("O domínio é obrigatório.", nameof(host));
        host = host.Trim().TrimEnd('.').ToLowerInvariant();
        if (host.Contains(':' ) || !Uri.CheckHostName(host).Equals(UriHostNameType.Dns) || !host.Contains('.'))
            throw new ArgumentException("Informe um domínio DNS válido, sem protocolo ou porta.", nameof(host));
        if (host == "localhost" || host.EndsWith(".localhost", StringComparison.Ordinal))
            throw new ArgumentException("Localhost não pode ser cadastrado como domínio customizado.", nameof(host));
        return new IdnMapping().GetAscii(host);
    }
}

public sealed class TenantDomainVerification : Entity
{
    public Guid AccountId { get; set; }
    public Guid TenantDomainId { get; set; }
    public TenantDomainVerificationMethod Method { get; set; }
    public bool Succeeded { get; set; }
    public string ResultCode { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? ApprovalReason { get; set; }
}

public sealed class TenantDomainSslCheck : Entity
{
    public Guid AccountId { get; set; }
    public Guid TenantDomainId { get; set; }
    public TenantDomainSslStatus Status { get; set; } = TenantDomainSslStatus.Unknown;
    public DateTime? CertificateExpiresAt { get; set; }
    public string? FailureReason { get; set; }
}

public sealed class TenantEmailDomain : Entity
{
    public Guid AccountId { get; set; }
    public string DomainName { get; set; } = string.Empty;
    public TenantEmailDomainStatus Status { get; set; } = TenantEmailDomainStatus.Pending;
    public DnsPolicyStatus SpfStatus { get; set; } = DnsPolicyStatus.Pending;
    public DnsPolicyStatus DkimStatus { get; set; } = DnsPolicyStatus.Pending;
    public DnsPolicyStatus DmarcStatus { get; set; } = DnsPolicyStatus.Pending;
    public DateTime? VerifiedAt { get; set; }
    public DateTime? LastCheckedAt { get; set; }
}

public sealed class TenantDomainAuditEvent : Entity
{
    public Guid AccountId { get; set; }
    public Guid? TenantDomainId { get; set; }
    public Guid? UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
