using OrcaFacil.Application.Fiscal;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class FiscalControlTests
{
    private static FiscalIssueContext Ready(FiscalOperationMode mode = FiscalOperationMode.Online) => new(Guid.NewGuid(), Guid.NewGuid(), "Invoice", Guid.NewGuid(), 100, true, true, true, true, true, mode, true, mode == FiscalOperationMode.Manual ? "Registro conferido pela contabilidade" : null);

    [Fact] public void Company_customer_and_service_readiness_are_required()
    {
        var valid=Ready();
        Assert.Throws<InvalidOperationException>(() => FiscalRules.ValidateIssue(valid with { CompanyReady=false }));
        Assert.Throws<InvalidOperationException>(() => FiscalRules.ValidateIssue(valid with { CustomerReady=false }));
        Assert.Throws<InvalidOperationException>(() => FiscalRules.ValidateIssue(valid with { ServicesConfigured=false }));
    }
    [Fact] public void Online_issue_requires_real_provider_and_valid_certificate()
    {
        var valid=Ready();
        Assert.Throws<InvalidOperationException>(() => FiscalRules.ValidateIssue(valid with { ProviderConfigured=false }));
        Assert.Throws<InvalidOperationException>(() => FiscalRules.ValidateIssue(valid with { CertificateValid=false }));
    }
    [Fact] public void Manual_control_requires_auditable_justification() => Assert.Throws<InvalidOperationException>(() => FiscalRules.ValidateIssue(Ready(FiscalOperationMode.Manual) with { ManualJustification=" " }));
    [Fact] public void Retentions_never_create_negative_net()
    {
        Assert.Equal(85m, FiscalRules.CalculateNet(100, 5, 10));
        Assert.Throws<InvalidOperationException>(() => FiscalRules.CalculateNet(100, 80, 30));
    }
    [Fact] public void Authorization_requires_real_provider_identifiers()
    {
        var service=new NfseService();
        Assert.Throws<InvalidOperationException>(() => service.RegisterProviderAuthorization(new(FiscalProviderState.Healthy,"ok")));
        Assert.Equal(FiscalDocumentStatus.Authorized, service.RegisterProviderAuthorization(new(FiscalProviderState.Healthy,"ok","provider-42","protocol-42")));
        Assert.Equal(FiscalDocumentStatus.ManualRegistered, service.RegisterManual("Registro validado manualmente"));
    }
    [Fact] public void Consolidated_documents_are_immutable() => Assert.Throws<InvalidOperationException>(() => FiscalRules.EnsureMutable(FiscalDocumentStatus.Authorized));
    [Fact] public void Cancellation_and_correction_require_reason_and_tenant()
    {
        var account=Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => new FiscalDocumentCancellationService().Validate(account,account,"curto"));
        Assert.Throws<UnauthorizedAccessException>(() => new FiscalDocumentCorrectionService().Validate(account,Guid.NewGuid(),"Correção conferida pelo fiscal"));
    }
    [Fact] public void Status_queries_ignore_manual_and_final_documents()
    {
        var service=new FiscalDocumentStatusQueryService();
        Assert.True(service.ShouldQuery(FiscalDocumentStatus.Processing,false)); Assert.False(service.ShouldQuery(FiscalDocumentStatus.Processing,true)); Assert.False(service.ShouldQuery(FiscalDocumentStatus.Authorized,false));
    }
    [Fact] public void Webhook_is_verified_and_idempotent()
    {
        var service=new FiscalWebhookService(); var item=new FiscalWebhookEnvelope("real","evt-1","document.authorized","{}","ok");
        Assert.True(service.Accept(item,new Verifier())); Assert.False(service.Accept(item,new Verifier())); Assert.False(service.Accept(item with { EventId="evt-2" },new RejectingVerifier()));
    }
    [Fact] public void Accounting_export_is_isolated_by_account_and_contains_real_values()
    {
        var account=Guid.NewGuid(); var rows=new[]{new FiscalDocumentSnapshot(account,Guid.NewGuid(),"1",DateTimeOffset.UtcNow,"Serviço",100,10,FiscalDocumentStatus.Authorized,"Invoice","p"),new FiscalDocumentSnapshot(Guid.NewGuid(),Guid.NewGuid(),"OTHER",DateTimeOffset.UtcNow,"Serviço",1,0,FiscalDocumentStatus.Draft,"Manual",null)};
        var csv=System.Text.Encoding.UTF8.GetString(new FiscalAccountingExportService().ExportCsv(account,rows)); Assert.Contains("\"1\"",csv); Assert.DoesNotContain("OTHER",csv);
    }
    private sealed class Verifier:IFiscalWebhookVerifier { public bool Verify(ReadOnlySpan<byte> payload,string? signature)=>signature=="ok"; }
    private sealed class RejectingVerifier:IFiscalWebhookVerifier { public bool Verify(ReadOnlySpan<byte> payload,string? signature)=>false; }
}
