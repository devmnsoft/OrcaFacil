using System.Globalization;
using System.Text.Json;

namespace OrcaFacil.Application.Billing;

public enum SettlementAction
{
    ObserveWithoutBenefit,
    Unresolved,
    Transient,
    ApplyFullCoverage,
    RecordPartialWithoutCoverage,
    ReverseMatchedCoverage,
    KeepCurrentState
}

public sealed record GatewayPaymentFacts(
    string Status,
    decimal? Amount,
    string? Currency,
    string? ExternalReference,
    bool? LiveMode,
    DateTime? ApprovedAt,
    decimal RefundedAmount,
    bool PayloadParsed);

public sealed record SettlementEvidence(
    string ExpectedEnvironment,
    decimal? ExpectedAmount,
    string? ExpectedCurrency,
    bool LinkedToInvoice,
    bool LinkedToSubscription,
    bool AccountAdministrativelyBlocked,
    bool PaymentAlreadyApproved,
    bool PaymentAlreadyReversed,
    bool CoverageExtended,
    bool CoverageReversed,
    int CycleMonths);

public sealed record SettlementDecision(
    SettlementAction Action,
    string Code,
    bool CompleteEvent,
    decimal Amount,
    int CycleMonths,
    bool UpdateFinancialStatus);

public static class BillingSettlementPolicy
{
    public const string EffectExtended = "coverage_extended";
    public const string EffectReversed = "coverage_reversed";

    public static GatewayPaymentFacts Read(string? reportedStatus, string? rawJson)
    {
        var status = (reportedStatus ?? string.Empty).Trim().ToLowerInvariant();
        if (IsTransientStatus(status))
            return new GatewayPaymentFacts(status, null, null, null, null, null, 0m, false);

        if (string.IsNullOrWhiteSpace(rawJson))
            return new GatewayPaymentFacts(status, null, null, null, null, null, 0m, false);

        try
        {
            using var document = JsonDocument.Parse(rawJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return new GatewayPaymentFacts(status, null, null, null, null, null, 0m, false);

            var payloadStatus = ReadString(root, "status");
            if (!string.IsNullOrWhiteSpace(payloadStatus))
                status = payloadStatus.Trim().ToLowerInvariant();

            decimal? amount = null;
            if (root.TryGetProperty("transaction_amount", out var amountElement) && amountElement.TryGetDecimal(out var parsedAmount))
                amount = decimal.Round(parsedAmount, 2, MidpointRounding.AwayFromZero);

            decimal refunded = 0m;
            if (root.TryGetProperty("transaction_amount_refunded", out var refundedElement) && refundedElement.TryGetDecimal(out var parsedRefunded))
                refunded = decimal.Round(parsedRefunded, 2, MidpointRounding.AwayFromZero);

            bool? liveMode = null;
            if (root.TryGetProperty("live_mode", out var liveElement) && liveElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
                liveMode = liveElement.GetBoolean();

            DateTime? approvedAt = null;
            if (root.TryGetProperty("date_approved", out var approvedElement) && approvedElement.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(approvedElement.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsedApproved))
                approvedAt = parsedApproved.ToUniversalTime();

            return new GatewayPaymentFacts(
                status,
                amount,
                ReadString(root, "currency_id"),
                ReadString(root, "external_reference"),
                liveMode,
                approvedAt,
                refunded,
                true);
        }
        catch (JsonException)
        {
            return new GatewayPaymentFacts(status, null, null, null, null, null, 0m, false);
        }
    }

    public static SettlementDecision Decide(GatewayPaymentFacts facts, SettlementEvidence evidence)
    {
        if (!facts.PayloadParsed || IsTransientStatus(facts.Status))
            return new(SettlementAction.Transient, "transient", false, 0m, evidence.CycleMonths, false);

        if (IsLateRegression(facts.Status, evidence))
            return new(SettlementAction.KeepCurrentState, "late_event_ignored", true, 0m, evidence.CycleMonths, false);

        if (facts.Status is "refunded" or "charged_back")
            return DecideReversal(facts, evidence);

        if (facts.Status is not "approved")
            return new(SettlementAction.ObserveWithoutBenefit, facts.Status is "authorized" or "active" ? "not_a_settled_payment" : "observed", true, 0m, evidence.CycleMonths, false);

        if (facts.Amount is not decimal amount || amount <= 0m || string.IsNullOrWhiteSpace(facts.Currency) || facts.LiveMode is null || facts.ApprovedAt is null)
            return new(SettlementAction.Unresolved, "payment_proof_missing", false, 0m, evidence.CycleMonths, false);

        if (!EnvironmentMatches(evidence.ExpectedEnvironment, facts.LiveMode.Value))
            return new(SettlementAction.ObserveWithoutBenefit, "environment_mismatch", true, 0m, evidence.CycleMonths, false);

        if (!evidence.LinkedToSubscription)
            return new(SettlementAction.Unresolved, "billing_link_missing", false, 0m, evidence.CycleMonths, false);

        if (evidence.ExpectedAmount is not decimal expected || expected <= 0m || string.IsNullOrWhiteSpace(evidence.ExpectedCurrency))
            return new(SettlementAction.Unresolved, "expected_amount_missing", false, 0m, evidence.CycleMonths, false);

        if (!string.Equals(facts.Currency, evidence.ExpectedCurrency, StringComparison.OrdinalIgnoreCase))
            return new(SettlementAction.ObserveWithoutBenefit, "currency_mismatch", true, 0m, evidence.CycleMonths, false);

        if (amount > expected)
            return new(SettlementAction.ObserveWithoutBenefit, "amount_exceeds_obligation", true, 0m, evidence.CycleMonths, false);

        if (amount < expected)
            return new(SettlementAction.RecordPartialWithoutCoverage, "partial_payment", true, amount, evidence.CycleMonths, false);

        if (evidence.CoverageExtended)
            return new(SettlementAction.KeepCurrentState, "coverage_already_applied", true, amount, evidence.CycleMonths, !evidence.AccountAdministrativelyBlocked);

        return new(SettlementAction.ApplyFullCoverage, "approved", true, amount, evidence.CycleMonths, !evidence.AccountAdministrativelyBlocked);
    }

    public static int CycleMonths(string? billingCycle) =>
        string.Equals(billingCycle, "annual", StringComparison.OrdinalIgnoreCase) ? 12 : 1;

    public static DateTime NextCoverage(DateTime? currentCoverage, DateTime paidAtUtc, int cycleMonths)
    {
        var months = cycleMonths == 12 ? 12 : 1;
        var baseline = currentCoverage.HasValue && currentCoverage.Value > paidAtUtc ? currentCoverage.Value : paidAtUtc;
        return baseline.AddMonths(months);
    }

    public static DateTime? ReverseCoverage(DateTime? currentCoverage, int cycleMonths)
    {
        if (!currentCoverage.HasValue) return null;
        var months = cycleMonths == 12 ? 12 : 1;
        return currentCoverage.Value.AddMonths(-months);
    }

    private static SettlementDecision DecideReversal(GatewayPaymentFacts facts, SettlementEvidence evidence)
    {
        if (!evidence.CoverageExtended || evidence.CoverageReversed || evidence.PaymentAlreadyReversed)
            return new(SettlementAction.ObserveWithoutBenefit, "reversal_without_new_effect", true, 0m, evidence.CycleMonths, false);

        var amount = facts.RefundedAmount > 0m ? facts.RefundedAmount : facts.Amount ?? evidence.ExpectedAmount ?? 0m;
        if (amount <= 0m)
            return new(SettlementAction.Unresolved, "refund_amount_missing", false, 0m, evidence.CycleMonths, false);

        return new(SettlementAction.ReverseMatchedCoverage, facts.Status, true, amount, evidence.CycleMonths, false);
    }

    private static bool IsLateRegression(string status, SettlementEvidence evidence)
    {
        if (!evidence.PaymentAlreadyApproved) return false;
        return status is "pending" or "authorized" or "active" or "in_process" or "in_mediation";
    }

    private static bool EnvironmentMatches(string expectedEnvironment, bool liveMode)
    {
        var production = expectedEnvironment.Equals("Production", StringComparison.OrdinalIgnoreCase);
        return production ? liveMode : !liveMode;
    }

    private static bool IsTransientStatus(string status) =>
        status is "provider_not_configured" or "timeout" or "429" or "408" or "500" or "502" or "503" or "504"
        || (int.TryParse(status, NumberStyles.None, CultureInfo.InvariantCulture, out var code) && code >= 500);

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
}
