using System.Globalization;
using OrcaFacil.Application.Fiscal;
using OrcaFacil.Domain.Entities;
using Xunit;

namespace OrcaFacil.UnitTests;

/// <summary>
/// Manual fiscalization correctness. The system tracks which received amounts are fiscalized but never fabricates a
/// fiscal number or access key: these tests pin what makes a receivable fiscalizable, the allowed lifecycle transitions,
/// and how an access key is masked for audit without being stored in full.
/// </summary>
public sealed class FiscalRequestRulesTests
{
    [Theory]
    [InlineData("10", false, true)]   // money received, not canceled -> fiscalizable
    [InlineData("0.5", false, true)]
    [InlineData("0", false, false)]   // nothing received yet
    [InlineData("10", true, false)]   // canceled receivable is not fiscalized here
    public void Is_fiscalizable_requires_received_money_and_not_canceled(string paidRaw, bool canceled, bool expected)
        => Assert.Equal(expected, FiscalRequestRules.IsFiscalizable(Money(paidRaw), canceled));

    private static decimal Money(string raw) => decimal.Parse(raw, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData(FiscalRequestStatus.Pending, true)]
    [InlineData(FiscalRequestStatus.InReview, true)]
    [InlineData(FiscalRequestStatus.Issued, false)]
    [InlineData(FiscalRequestStatus.Canceled, false)]
    [InlineData(FiscalRequestStatus.NotRequired, false)]
    [InlineData(FiscalRequestStatus.Rejected, false)]
    public void Is_active_is_only_the_open_states(FiscalRequestStatus status, bool expected)
        => Assert.Equal(expected, FiscalRequestRules.IsActive(status));

    [Theory]
    [InlineData(FiscalRequestStatus.Issued, true)]
    [InlineData(FiscalRequestStatus.Canceled, true)]
    [InlineData(FiscalRequestStatus.NotRequired, true)]
    [InlineData(FiscalRequestStatus.Rejected, true)]
    [InlineData(FiscalRequestStatus.Pending, false)]
    [InlineData(FiscalRequestStatus.InReview, false)]
    public void Is_settled_is_only_terminal_states(FiscalRequestStatus status, bool expected)
        => Assert.Equal(expected, FiscalRequestRules.IsSettled(status));

    [Fact]
    public void Can_transition_allows_only_the_defined_edges()
    {
        var allowed = new[]
        {
            (FiscalRequestStatus.Pending, FiscalRequestStatus.Issued),
            (FiscalRequestStatus.Pending, FiscalRequestStatus.Canceled),
            (FiscalRequestStatus.Pending, FiscalRequestStatus.NotRequired),
            (FiscalRequestStatus.InReview, FiscalRequestStatus.Issued),
            (FiscalRequestStatus.InReview, FiscalRequestStatus.Canceled),
            (FiscalRequestStatus.InReview, FiscalRequestStatus.NotRequired),
            (FiscalRequestStatus.Issued, FiscalRequestStatus.Canceled),
        };
        foreach (var (from, to) in allowed)
            Assert.True(FiscalRequestRules.CanTransition(from, to), $"{from}->{to} should be allowed");

        foreach (FiscalRequestStatus from in Enum.GetValues<FiscalRequestStatus>())
            foreach (FiscalRequestStatus to in Enum.GetValues<FiscalRequestStatus>())
                if (!allowed.Contains((from, to)))
                    Assert.False(FiscalRequestRules.CanTransition(from, to), $"{from}->{to} should be disallowed");
    }

    [Fact]
    public void An_issued_note_can_be_cancelled_but_not_issued_twice()
    {
        Assert.True(FiscalRequestRules.CanTransition(FiscalRequestStatus.Issued, FiscalRequestStatus.Canceled));
        Assert.False(FiscalRequestRules.CanTransition(FiscalRequestStatus.Issued, FiscalRequestStatus.Issued));
        Assert.False(FiscalRequestRules.CanTransition(FiscalRequestStatus.Canceled, FiscalRequestStatus.Issued));
    }

    [Theory]
    [InlineData(FiscalRequestStatus.Canceled, true)]
    [InlineData(FiscalRequestStatus.NotRequired, true)]
    [InlineData(FiscalRequestStatus.Issued, false)]
    [InlineData(FiscalRequestStatus.Pending, false)]
    public void Requires_reason_flags_cancel_and_not_required(FiscalRequestStatus to, bool expected)
        => Assert.Equal(expected, FiscalRequestRules.RequiresReason(to));

    [Theory]
    [InlineData("123", true)]
    [InlineData("0044/2026", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void Is_valid_external_number_rejects_blank(string? number, bool expected)
        => Assert.Equal(expected, FiscalRequestRules.IsValidExternalNumber(number));

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("12345678", "12345678")]          // 8 chars: kept whole
    [InlineData("123456789", "1234…6789")]         // 9 chars: mask the middle
    [InlineData("ABCD1234EFGH5678IJKL", "ABCD…IJKL")] // ends only, middle hidden
    [InlineData("  123456789  ", "1234…6789")]      // trimmed before masking
    public void Mask_access_key_keeps_ends_and_hides_middle(string? key, string expected)
        => Assert.Equal(expected, FiscalRequestRules.MaskAccessKey(key));
}
