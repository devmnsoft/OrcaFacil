using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Web.Pages.SuperAdmin.Finance;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class FinancialIntegrityDiagnosticsTests
{
    private static FinancialPaymentRow Payment(Guid accountId, Guid clientId, decimal amount,
        Guid? documentId = null, Guid? workOrderId = null, bool reversed = false, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), accountId, clientId, amount, reversed, documentId, workOrderId, DateTime.UtcNow);

    private static FinancialDocumentRow Document(Guid accountId, Guid clientId, string number = "ORC-1",
        bool deleted = false, decimal total = 1000m) =>
        new(Guid.NewGuid(), accountId, clientId, number, total, deleted);

    private static FinancialWorkOrderRow WorkOrder(Guid accountId, Guid clientId, Guid? sourceDocumentId,
        string number = "OS-1", bool cancelled = false) =>
        new(Guid.NewGuid(), accountId, clientId, sourceDocumentId, number, cancelled, false);

    private static FinancialReceiptRow Receipt(Guid accountId, Guid paymentId, decimal amount, string number = "REC-1",
        bool cancelled = false) =>
        new(Guid.NewGuid(), accountId, amount, paymentId, number, cancelled, false);

    [Fact]
    public void Consistent_graph_reports_no_divergence()
    {
        var accountId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var document = Document(accountId, clientId);
        var order = WorkOrder(accountId, clientId, document.Id);
        var payment = Payment(accountId, clientId, 500m, document.Id, order.Id);

        var findings = FinancialIntegrityAnalyzer.Analyze([payment], [document], [order], [Receipt(accountId, payment.Id, 500m)]);

        Assert.Empty(findings);
    }

    [Fact]
    public void Payment_without_origin_is_critical()
    {
        var accountId = Guid.NewGuid();
        var payment = Payment(accountId, Guid.NewGuid(), 250m);

        var findings = FinancialIntegrityAnalyzer.Analyze([payment], [], [], []);

        var finding = Assert.Single(findings);
        Assert.Equal("payment-without-origin", finding.TypeCode);
        Assert.Equal(FinancialDivergenceSeverity.Critical, finding.Severity);
        Assert.Equal(payment.Id, finding.OccurrenceId);
    }

    [Fact]
    public void Incompatible_link_between_document_and_work_order_is_critical()
    {
        var accountId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var originDocument = Document(accountId, clientId, "ORC-ORIGEM");
        var otherDocument = Document(accountId, clientId, "ORC-OUTRO");
        var order = WorkOrder(accountId, clientId, originDocument.Id);
        var payment = Payment(accountId, clientId, 800m, otherDocument.Id, order.Id);

        var findings = FinancialIntegrityAnalyzer.Analyze([payment], [originDocument, otherDocument], [order], []);

        Assert.Contains(findings, x => x.TypeCode == "payment-incompatible-link" &&
                                       x.Severity == FinancialDivergenceSeverity.Critical);
    }

    [Fact]
    public void Payment_client_mismatch_with_work_order_is_high()
    {
        var accountId = Guid.NewGuid();
        var document = Document(accountId, Guid.NewGuid());
        var order = WorkOrder(accountId, Guid.NewGuid(), document.Id);
        var payment = Payment(accountId, Guid.NewGuid(), 100m, document.Id, order.Id);

        var findings = FinancialIntegrityAnalyzer.Analyze([payment], [document], [order], []);

        Assert.Contains(findings, x => x.TypeCode == "payment-client-mismatch" &&
                                       x.Severity == FinancialDivergenceSeverity.High);
    }

    [Fact]
    public void Payment_referencing_a_deleted_document_is_reported()
    {
        var accountId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var document = Document(accountId, clientId, deleted: true);
        var payment = Payment(accountId, clientId, 300m, document.Id);

        var findings = FinancialIntegrityAnalyzer.Analyze([payment], [document], [], []);

        Assert.Contains(findings, x => x.TypeCode == "payment-missing-document");
    }

    [Fact]
    public void Cross_account_link_is_a_critical_isolation_failure()
    {
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var foreignDocument = Document(accountB, Guid.NewGuid());
        var payment = Payment(accountA, Guid.NewGuid(), 999m, foreignDocument.Id);

        var findings = FinancialIntegrityAnalyzer.Analyze([payment], [foreignDocument], [], []);

        Assert.Contains(findings, x => x.TypeCode == "payment-cross-account-link" &&
                                       x.Severity == FinancialDivergenceSeverity.Critical);
    }

    [Fact]
    public void Work_order_without_source_document_is_medium()
    {
        var accountId = Guid.NewGuid();
        var order = WorkOrder(accountId, Guid.NewGuid(), Guid.NewGuid(), "OS-404");

        var findings = FinancialIntegrityAnalyzer.Analyze([], [], [order], []);

        var finding = Assert.Single(findings);
        Assert.Equal("workorder-missing-document", finding.TypeCode);
        Assert.Equal(FinancialDivergenceSeverity.Medium, finding.Severity);
        Assert.Equal(order.Id, finding.OccurrenceId);
    }

    [Fact]
    public void Active_payment_on_cancelled_work_order_is_high()
    {
        var accountId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var document = Document(accountId, clientId);
        var order = WorkOrder(accountId, clientId, document.Id, "OS-CANCELADA", cancelled: true);
        var payment = Payment(accountId, clientId, 400m, document.Id, order.Id);

        var findings = FinancialIntegrityAnalyzer.Analyze([payment], [document], [order], []);

        var finding = Assert.Single(findings, x => x.TypeCode == "active-payment-on-cancelled-workorder");
        Assert.Equal(400m, finding.Amount);
    }

    [Fact]
    public void Receipt_for_reversed_payment_is_high()
    {
        var accountId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var document = Document(accountId, clientId);
        var payment = Payment(accountId, clientId, 100m, document.Id, reversed: true);

        var findings = FinancialIntegrityAnalyzer.Analyze([payment], [document], [], [Receipt(accountId, payment.Id, 100m)]);

        Assert.Contains(findings, x => x.TypeCode == "receipt-for-reversed-payment");
    }

    [Fact]
    public void Receipt_without_matching_payment_is_high()
    {
        var accountId = Guid.NewGuid();

        var findings = FinancialIntegrityAnalyzer.Analyze([], [], [], [Receipt(accountId, Guid.NewGuid(), 90m)]);

        var finding = Assert.Single(findings);
        Assert.Equal("receipt-missing-payment", finding.TypeCode);
    }

    [Fact]
    public void Receipt_amount_mismatch_is_medium()
    {
        var accountId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var document = Document(accountId, clientId);
        var payment = Payment(accountId, clientId, 120m, document.Id);

        var findings = FinancialIntegrityAnalyzer.Analyze([payment], [document], [], [Receipt(accountId, payment.Id, 100m)]);

        var finding = Assert.Single(findings, x => x.TypeCode == "receipt-amount-mismatch");
        Assert.Equal(20m, finding.Amount);
    }

    [Fact]
    public void Cancelled_receipt_is_not_reported()
    {
        var accountId = Guid.NewGuid();

        var findings = FinancialIntegrityAnalyzer.Analyze([], [], [], [Receipt(accountId, Guid.NewGuid(), 90m, cancelled: true)]);

        Assert.Empty(findings);
    }

    [Fact]
    public void Finding_id_is_deterministic_for_the_same_occurrence()
    {
        var accountId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var payment = Payment(accountId, Guid.NewGuid(), 10m, id: paymentId);

        var first = FinancialIntegrityAnalyzer.Analyze([payment], [], [], []);
        var second = FinancialIntegrityAnalyzer.Analyze([payment], [], [], []);

        Assert.Equal(first[0].FindingId, second[0].FindingId);
        Assert.Equal($"payment-without-origin:{paymentId:N}", first[0].FindingId);
    }

    [Fact]
    public void Report_aggregates_severities_and_affected_amount()
    {
        var accountId = Guid.NewGuid();
        var orphan = Payment(accountId, Guid.NewGuid(), 100m);
        var receiptless = Receipt(accountId, Guid.NewGuid(), 50m);
        var findings = FinancialIntegrityAnalyzer.Analyze([orphan], [], [], [receiptless]);
        var report = new FinancialIntegrityReport(findings, 1, DateTime.UtcNow);

        Assert.True(report.HasFindings);
        Assert.Equal(1, report.CriticalCount);
        Assert.Equal(1, report.HighCount);
        Assert.Equal(150m, report.AffectedAmount);
    }

    [Fact]
    public void Critical_findings_are_ordered_first()
    {
        var accountId = Guid.NewGuid();
        var orphan = Payment(accountId, Guid.NewGuid(), 1m);
        var order = WorkOrder(accountId, Guid.NewGuid(), Guid.NewGuid());

        var findings = FinancialIntegrityAnalyzer.Analyze([orphan], [], [order], []);

        Assert.Equal(FinancialDivergenceSeverity.Critical, findings[0].Severity);
    }

    [Fact]
    public void Divergence_screen_is_restricted_to_super_admin()
    {
        var attribute = typeof(DivergencesModel).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("SuperAdminOnly", attribute!.Policy);
    }
}
