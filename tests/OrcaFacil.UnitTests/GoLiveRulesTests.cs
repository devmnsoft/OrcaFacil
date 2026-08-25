using OrcaFacil.Application.GoLive;

namespace OrcaFacil.UnitTests;

public class GoLiveRulesTests
{
    [Fact]
    public void Provisioning_creates_account_owner_and_secure_token()
    {
        var result = new TenantProvisioningService().Prepare(new("Empresa", "123", "owner@empresa.test", 14), false, false, false);
        Assert.NotEqual(Guid.Empty, result.AccountId);
        Assert.NotEqual(Guid.Empty, result.OwnerId);
        Assert.True(Convert.FromBase64String(result.InvitationToken).Length >= 32);
    }

    [Fact]
    public void Provisioning_requires_explicit_duplicate_confirmation() =>
        Assert.Throws<InvalidOperationException>(() => new TenantProvisioningService().Prepare(new("Empresa", "123", "owner@empresa.test", 14), false, true, false));

    [Fact]
    public void Blocking_checklist_prevents_go_live() =>
        Assert.False(new TenantLaunchChecklistService().CanGoLive([new(true, false, null, null), new(false, false, null, null)]));

    [Fact]
    public void Migration_requires_preview_for_same_account()
    {
        var account = Guid.NewGuid();
        var preview = new MigrationPreview(Guid.NewGuid(), account, 2, 1);
        Assert.True(new CustomerMigrationService().ConfirmImport(preview, account).Confirmed);
        Assert.Throws<UnauthorizedAccessException>(() => new CustomerMigrationService().ConfirmImport(preview, Guid.NewGuid()));
    }

    [Fact]
    public void Demo_defaults_block_all_external_side_effects()
    {
        var policy = new DemoAccountService().CreateSafePolicy();
        Assert.True(policy.BlockEmail && policy.BlockWebhook && policy.BlockPayment && policy.BlockFiscal);
    }

    [Fact]
    public void Readiness_is_deterministic_and_explainable()
    {
        var service = new AccountReadinessService();
        var criteria = new[] { new ReadinessCriterion("company", true, 30, "/Settings"), new ReadinessCriterion("services", false, 70, "/Services") };
        var result = service.Calculate(criteria);
        Assert.Equal(30, result.Score);
        Assert.Equal("services", Assert.Single(result.Findings).Code);
    }

    [Fact]
    public void Go_live_requires_minimum_checklist() =>
        Assert.Throws<InvalidOperationException>(() => new GoLiveReviewService().Approve(Guid.NewGuid(), false));
}
