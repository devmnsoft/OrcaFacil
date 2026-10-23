using OrcaFacil.Application.Fiscal;
using OrcaFacil.Domain.Entities;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class FiscalAccountingSnapshotTests
{
    private static readonly DateTime IssuedUtc = new(2026, 9, 15, 12, 30, 0, DateTimeKind.Utc);
    private static readonly Guid AccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ClientId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static FiscalAccountingSource Src(Func<FiscalAccountingSource, FiscalAccountingSource>? mutate = null)
    {
        var s = new FiscalAccountingSource(
            AccountId, ClientId, "NFE-0007", IssuedUtc,
            "Manutenção elétrica", "NFS-e", 1250.90m,
            "Controle manual auditado", "ABC…XYZ");
        return mutate is null ? s : mutate(s);
    }

    [Fact] public void Maps_all_recorded_fields_to_the_snapshot()
    {
        var snap = FiscalAccountingSnapshotMapper.ToSnapshot(Src());
        Assert.Equal(AccountId, snap.AccountId);
        Assert.Equal(ClientId, snap.CustomerId);
        Assert.Equal("NFE-0007", snap.Number);
        Assert.Equal(IssuedUtc, snap.IssuedAt.UtcDateTime);
        Assert.Equal(TimeSpan.Zero, snap.IssuedAt.Offset);
        Assert.Equal("Manutenção elétrica", snap.Service);
        Assert.Equal(1250.90m, snap.GrossAmount);
        Assert.Equal(0m, snap.Retentions);
        Assert.Equal(FiscalDocumentStatus.ManualRegistered, snap.Status);
        Assert.Equal("Controle manual auditado", snap.Origin);
        Assert.Equal("ABC…XYZ", snap.Protocol);
    }

    [Fact] public void Missing_number_is_flagged_not_fabricated()
    {
        Assert.Equal(FiscalAccountingSnapshotMapper.NoNumber, FiscalAccountingSnapshotMapper.ToSnapshot(Src(s => s with { Number = null })).Number);
        Assert.Equal(FiscalAccountingSnapshotMapper.NoNumber, FiscalAccountingSnapshotMapper.ToSnapshot(Src(s => s with { Number = "   " })).Number);
    }

    [Fact] public void Service_falls_back_description_then_doctype_then_generic()
    {
        Assert.Equal("Manutenção elétrica", FiscalAccountingSnapshotMapper.ToSnapshot(Src()).Service);
        Assert.Equal("NFS-e", FiscalAccountingSnapshotMapper.ToSnapshot(Src(s => s with { ServiceDescription = null })).Service);
        Assert.Equal(FiscalAccountingSnapshotMapper.DefaultService, FiscalAccountingSnapshotMapper.ToSnapshot(Src(s => s with { ServiceDescription = null, DocumentType = null })).Service);
    }

    [Fact] public void Missing_amount_defaults_to_zero_and_retention_stays_zero()
    {
        var snap = FiscalAccountingSnapshotMapper.ToSnapshot(Src(s => s with { GrossAmount = null }));
        Assert.Equal(0m, snap.GrossAmount);
        Assert.Equal(0m, snap.Retentions);
        Assert.Equal(0m, snap.GrossAmount - snap.Retentions);
    }

    [Fact] public void Missing_client_defaults_empty_and_provider_defaults_to_manual_control()
    {
        var snap = FiscalAccountingSnapshotMapper.ToSnapshot(Src(s => s with { ClientId = null, ProviderName = null }));
        Assert.Equal(Guid.Empty, snap.CustomerId);
        Assert.Equal(FiscalAccountingSnapshotMapper.DefaultProvider, snap.Origin);
    }

    [Fact] public void Provider_and_protocol_are_trimmed_and_blank_becomes_null()
    {
        var trimmed = FiscalAccountingSnapshotMapper.ToSnapshot(Src(s => s with { Protocol = " X1 ", ProviderName = "  Alfa Contábil " }));
        Assert.Equal("X1", trimmed.Protocol);
        Assert.Equal("Alfa Contábil", trimmed.Origin);
        Assert.Null(FiscalAccountingSnapshotMapper.ToSnapshot(Src(s => s with { Protocol = "   " })).Protocol);
    }

    [Fact] public void Only_issued_requests_are_exportable()
    {
        Assert.True(FiscalAccountingSnapshotMapper.ShouldExport(FiscalRequestStatus.Issued));
        Assert.False(FiscalAccountingSnapshotMapper.ShouldExport(FiscalRequestStatus.Pending));
        Assert.False(FiscalAccountingSnapshotMapper.ShouldExport(FiscalRequestStatus.InReview));
        Assert.False(FiscalAccountingSnapshotMapper.ShouldExport(FiscalRequestStatus.Rejected));
        Assert.False(FiscalAccountingSnapshotMapper.ShouldExport(FiscalRequestStatus.Canceled));
        Assert.False(FiscalAccountingSnapshotMapper.ShouldExport(FiscalRequestStatus.NotRequired));
    }
}
