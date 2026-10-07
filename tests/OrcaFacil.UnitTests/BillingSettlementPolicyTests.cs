using OrcaFacil.Application.Billing;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class BillingSettlementPolicyTests
{
    private static GatewayPaymentFacts Approved(decimal amount = 99m, bool live = false, string currency = "BRL") =>
        new("approved", amount, currency, "inv:1", live, new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc), 0m, true);

    private static SettlementEvidence Evidence(
        bool linked = true,
        decimal expected = 99m,
        bool extended = false,
        bool reversed = false,
        bool blocked = false,
        string environment = "Sandbox",
        int months = 1,
        bool alreadyApproved = false) =>
        new(environment, expected, "BRL", linked, linked, blocked, alreadyApproved, false, extended, reversed, months);

    [Fact]
    public void Authorized_subscription_does_not_settle_coverage()
    {
        var facts = new GatewayPaymentFacts("authorized", 99m, "BRL", "inv:1", false, DateTime.UtcNow, 0m, true);
        var decision = BillingSettlementPolicy.Decide(facts, Evidence());
        Assert.Equal(SettlementAction.ObserveWithoutBenefit, decision.Action);
        Assert.Equal("not_a_settled_payment", decision.Code);
    }

    [Fact]
    public void Approved_payment_without_amount_or_currency_is_not_proof()
    {
        var facts = new GatewayPaymentFacts("approved", null, null, "inv:1", null, null, 0m, true);
        var decision = BillingSettlementPolicy.Decide(facts, Evidence());
        Assert.Equal(SettlementAction.Unresolved, decision.Action);
        Assert.False(decision.CompleteEvent);
    }

    [Fact]
    public void Partial_amount_does_not_extend_coverage()
    {
        var decision = BillingSettlementPolicy.Decide(Approved(40m), Evidence());
        Assert.Equal(SettlementAction.RecordPartialWithoutCoverage, decision.Action);
    }

    [Fact]
    public void Full_monthly_and_annual_amounts_extend_once()
    {
        var monthly = BillingSettlementPolicy.Decide(Approved(), Evidence());
        var annual = BillingSettlementPolicy.Decide(Approved(990m), Evidence(expected: 990m, months: 12));
        Assert.Equal(SettlementAction.ApplyFullCoverage, monthly.Action);
        Assert.Equal(1, monthly.CycleMonths);
        Assert.Equal(SettlementAction.ApplyFullCoverage, annual.Action);
        Assert.Equal(12, annual.CycleMonths);

        var duplicate = BillingSettlementPolicy.Decide(Approved(), Evidence(extended: true, alreadyApproved: true));
        Assert.Equal(SettlementAction.KeepCurrentState, duplicate.Action);
    }

    [Fact]
    public void Early_renewal_starts_from_the_current_paid_through_date()
    {
        var paidAt = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
        var current = new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(current.AddMonths(1), BillingSettlementPolicy.NextCoverage(current, paidAt, 1));
        Assert.Equal(current.AddMonths(12), BillingSettlementPolicy.NextCoverage(current, paidAt, 12));
    }

    [Fact]
    public void Missing_internal_link_and_environment_mismatch_do_not_activate_benefits()
    {
        Assert.Equal("billing_link_missing", BillingSettlementPolicy.Decide(Approved(), Evidence(linked: false)).Code);
        Assert.Equal("environment_mismatch", BillingSettlementPolicy.Decide(Approved(live: true), Evidence()).Code);
    }

    [Fact]
    public void Refund_reverses_only_an_extended_payment_and_keeps_admin_block_independent()
    {
        var refund = new GatewayPaymentFacts("refunded", 99m, "BRL", "inv:1", false, null, 99m, true);
        var withoutCoverage = BillingSettlementPolicy.Decide(refund, Evidence());
        var withCoverage = BillingSettlementPolicy.Decide(refund, Evidence(extended: true));
        var blocked = BillingSettlementPolicy.Decide(Approved(), Evidence(blocked: true));

        Assert.Equal(SettlementAction.ObserveWithoutBenefit, withoutCoverage.Action);
        Assert.Equal(SettlementAction.ReverseMatchedCoverage, withCoverage.Action);
        Assert.False(withCoverage.UpdateFinancialStatus);
        Assert.False(blocked.UpdateFinancialStatus);
        Assert.Equal(SettlementAction.ApplyFullCoverage, blocked.Action);
    }

    [Fact]
    public void Late_pending_event_does_not_regress_an_approved_payment()
    {
        var pending = new GatewayPaymentFacts("pending", null, null, null, null, null, 0m, true);
        var decision = BillingSettlementPolicy.Decide(pending, Evidence(alreadyApproved: true));
        Assert.Equal(SettlementAction.KeepCurrentState, decision.Action);
    }

    [Fact]
    public void Invalid_payload_stays_retryable()
    {
        var facts = BillingSettlementPolicy.Read("500", "não é json");
        var decision = BillingSettlementPolicy.Decide(facts, Evidence());
        Assert.Equal(SettlementAction.Transient, decision.Action);
        Assert.False(decision.CompleteEvent);
    }
}
