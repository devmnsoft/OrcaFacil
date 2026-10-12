using OrcaFacil.Application.Commercial;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class CommercialJourneyRuleTests
{
    [Fact]
    public void Unspecified_receipt_time_uses_business_zone_not_the_process_zone()
    {
        var civil = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);
        Assert.Contains(CommercialClock.BusinessTimeZone.Id, new[] { "America/Sao_Paulo", "E. South America Standard Time" });
        var utc = CommercialClock.NormalizeToUtc(civil);
        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        Assert.Equal(new DateTime(2026, 6, 15, 15, 0, 0, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void Same_payment_key_matches_only_the_same_order_amount_method_and_instant()
    {
        var when = new DateTime(2026, 6, 15, 15, 0, 0, DateTimeKind.Utc);
        var order = Guid.NewGuid();
        Assert.True(ManualPaymentIdempotency.Matches(order, 300m, "pix", when, order, 300m, "pix", when.AddMilliseconds(400)));
        Assert.False(ManualPaymentIdempotency.Matches(order, 300m, "pix", when, order, 700m, "pix", when));
        Assert.False(ManualPaymentIdempotency.Matches(order, 300m, "pix", when, Guid.NewGuid(), 300m, "pix", when));
        Assert.False(ManualPaymentIdempotency.Matches(order, 300m, "pix", when, order, 300m, "cash", when));
        Assert.False(ManualPaymentIdempotency.Matches(order, 300m, "pix", when, order, 300m, "pix", when.AddMinutes(1)));
    }

    [Fact]
    public void Partial_receipts_use_decimal_and_stop_at_the_order_total()
    {
        var total = 1000m;
        var first = CommercialCalculator.Round(300m);
        var second = CommercialCalculator.Round(700m);
        var balance = total - first;
        Assert.Equal(700m, balance);
        Assert.Equal(0m, balance - second);
        Assert.True(CommercialCalculator.Round(0.01m) > 0m);
        Assert.Throws<ArgumentException>(() => CommercialCalculator.Calculate([new(1, 1000m, 1000.01m)]));
    }

    [Fact]
    public void Dashboard_document_metrics_are_scoped_by_account_and_keep_the_actor()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var queries = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Queries", "DashboardQueries.cs"));
        var experience = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Web", "Services", "DashboardExperienceService.cs"));
        Assert.Contains("account_id = @accountId", queries);
        Assert.DoesNotContain("where user_id = @userId and is_deleted = false", queries);
        Assert.Contains("actorUserId", queries);
        Assert.Contains("currentUser.UserId", experience);
        Assert.Contains("currentAccount.AccountId", experience);
    }

    [Fact]
    public void Repeated_conversion_looks_up_the_existing_order_before_the_approved_guard()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var source = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Services", "CommercialJourneyService.cs"));
        var lookup = source.IndexOf("SourceDocumentId == document.Id", StringComparison.Ordinal);
        var approved = source.IndexOf("status != DocumentStatus.Approved", StringComparison.Ordinal);
        Assert.True(lookup > 0 && approved > lookup);
        Assert.Contains("IdempotencyConflict", source);
        Assert.Contains("ManualPaymentIdempotency.Matches", source);
        Assert.Contains("FinancialRecordStatus.Active", source);
    }

    [Fact]
    public void Direct_budget_receipt_respects_blocking_financial_divergence()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var source = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Services", "ReceiptApplicationService.cs"));
        var balanceLookup = source.IndexOf("balances.GetForDocumentAsync", StringComparison.Ordinal);
        var blockGuard = source.IndexOf("balance.HasBlockingDivergence", StringComparison.Ordinal);
        var paymentCreation = source.IndexOf("db.ManualPayments.Add(payment)", StringComparison.Ordinal);

        Assert.True(balanceLookup > 0);
        Assert.True(blockGuard > balanceLookup);
        Assert.True(paymentCreation > blockGuard);
        Assert.Contains("vínculo incompatível entre orçamento, ordem e recebimento", source);
    }

    [Fact]
    public void Work_order_balance_delegates_nullable_origin_to_the_canonical_classifier()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var source = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Services", "CommercialBalanceService.cs"));

        Assert.Contains("CommercialPaymentClassifier.IsSelectedOrigin(payment.DocumentId, payment.WorkOrderId, documentId, selectedWorkOrderId)", source);
        Assert.DoesNotContain("documentId.HasValue && CommercialPaymentClassifier.IsSelectedOrigin", source);
        Assert.True(CommercialPaymentClassifier.IsSelectedOrigin(null, Guid.NewGuid(), null, Guid.NewGuid()) is false);

        var selectedOrderId = Guid.NewGuid();
        Assert.True(CommercialPaymentClassifier.IsSelectedOrigin(null, selectedOrderId, null, selectedOrderId));
    }

    [Fact]
    public void Sharing_link_success_returns_to_the_accessible_mobile_actions_panel()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var model = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Web", "Pages", "Documents", "Details.cshtml.cs"));
        var page = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Web", "Pages", "Documents", "Details.cshtml"));
        var script = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Web", "wwwroot", "js", "commercial-workspace.js"));

        Assert.Contains("return RedirectToPage(new { id, tab = \"sharing\" })", model);
        Assert.Contains("aria-controls=\"document-command-sharing document-command-manage document-command-danger\"", page);
        Assert.Contains("if (event.key !== 'Escape'", script);
        Assert.Contains("commandToggle?.focus({ preventScroll: true })", script);
        Assert.Contains("if (requestedRawTab === 'sharing') focusCommandSection('sharing')", script);
    }

    [Fact]
    public void Quote_list_and_detail_share_the_canonical_next_action_resolver()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var listService = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Services", "QuoteWorkspaceService.cs"));
        var detailService = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Services", "CommercialWorkspaceQueryService.cs"));
        var resolver = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Application", "Commercial", "CommercialNextActionResolver.cs"));

        Assert.Contains("CommercialNextActionResolver.ResolveQuoteAction", listService);
        Assert.Contains("CommercialNextActionResolver.ResolveDocumentAction", detailService);
        Assert.Contains("visualizacao nao e aprovacao", resolver);
        Assert.Contains("WorkOrderPaid > 0m && context.LatestPaymentId is not null", resolver);
        Assert.DoesNotContain("private static NextActionDescriptor NextAction", listService);
        Assert.DoesNotContain("private static CommercialNextAction ResolveNext", detailService);
    }

    [Fact]
    public void Current_revision_decision_is_selected_deterministically_without_any_change_request_shortcut()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var listService = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Services", "QuoteWorkspaceService.cs"));
        var detailService = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Services", "CommercialWorkspaceQueryService.cs"));

        Assert.Contains("GroupBy(r => r.DocumentId)", listService);
        Assert.Contains("OrderByDescending(r => r.VersionNumber).ThenByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).First()", listService);
        Assert.DoesNotContain(".ToDictionaryAsync(r => r.DocumentId, r => r", listService);
        Assert.Contains("decisionByCurrentRevision.TryGetValue(currentRev.Id", listService);
        Assert.DoesNotContain("changeRequestSet", listService);

        Assert.Contains("decision.Decision == PublicDocumentDecisionType.ChangeRequested", detailService);
        Assert.DoesNotContain("decisions.Any(x => x.DocumentRevisionId == currentRevision.Id && x.Decision == PublicDocumentDecisionType.ChangeRequested)", detailService);
    }

    [Fact]
    public void Multiple_current_revisions_block_contract_commands_explicitly()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var service = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Services", "CommercialJourneyService.cs"));

        Assert.Contains("currentCandidates.Count > 1", service);
        Assert.Contains("revisionCandidates.Count > 1", service);
        Assert.Contains("mais de uma versão vigente", service);
        Assert.DoesNotContain("SingleOrDefaultAsync(x => x.AccountId == AccountId && x.DocumentId == document.Id && x.IsCurrent", service);
        Assert.DoesNotContain("SingleOrDefaultAsync(x => x.AccountId == AccountId && x.DocumentId == documentId && x.IsCurrent", service);
    }

    [Fact]
    public void Document_sequence_status_compatibility_is_incremental_and_idempotent()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var consolidated = File.ReadAllText(Path.Combine(root, "database", "script_completop.sql"));
        var hotfix = File.ReadAllText(Path.Combine(root, "database", "hotfix_document_sequences_status_compat_v73.sql"));

        Assert.Contains(@"\ir hotfix_document_sequences_status_compat_v73.sql", consolidated);
        Assert.Contains("lower(m.status::text) IN ('1', 'active')", hotfix);
        Assert.Contains("lower(a.status::text) IN ('1', 'active')", hotfix);
        Assert.Contains("HAVING count(DISTINCT m.account_id) = 1", hotfix);
        Assert.Contains("CREATE UNIQUE INDEX IF NOT EXISTS ux_document_sequences_account_type", hotfix);
        Assert.Contains("CREATE UNIQUE INDEX IF NOT EXISTS ux_documents_account_type_number", hotfix);
    }

    [Fact]
    public void Document_next_action_preserves_route_tab_for_navigation_actions()
    {
        var share = CommercialNextActionResolver.ResolveDocumentAction(new(
            Guid.NewGuid(),
            "Ready"));
        Assert.Null(share.Handler);
        Assert.Equal("/Documents/Details", share.Page);
        Assert.NotNull(share.RouteValues);
        Assert.Equal("sharing", share.RouteValues!["tab"]);

        var finance = CommercialNextActionResolver.ResolveDocumentAction(new(
            Guid.NewGuid(),
            "ConvertedToWorkOrder",
            Balance: new CommercialBalance(Guid.NewGuid(), Guid.NewGuid(), "WorkOrder", 1000, 1000, 0, 0, 0, [], []),
            WorkOrderId: Guid.NewGuid(),
            WorkOrderStatus: "Completed",
            WorkOrderPaid: 1000,
            WorkOrderBalance: 0));
        Assert.Equal("finance", finance.RouteValues!["tab"]);
    }

    [Fact]
    public void Work_order_next_actions_distinguish_create_from_open()
    {
        var approved = CommercialNextActionResolver.ResolveDocumentAction(new(Guid.NewGuid(), "Approved"));
        Assert.Equal("create-work-order", approved.Code);
        Assert.Equal("WorkOrder", approved.Handler);

        var open = CommercialNextActionResolver.ResolveDocumentAction(new(
            Guid.NewGuid(),
            "ConvertedToWorkOrder",
            WorkOrderId: Guid.NewGuid(),
            WorkOrderStatus: "Cancelled"));
        Assert.Equal("open-work-order", open.Code);
        Assert.Null(open.Handler);
        Assert.Equal("/WorkOrders/Details", open.Page);
    }

    [Fact]
    public void Receipt_pending_payment_wins_over_an_older_existing_receipt()
    {
        var next = CommercialNextActionResolver.ResolveDocumentAction(new(
            Guid.NewGuid(),
            "ConvertedToWorkOrder",
            WorkOrderId: Guid.NewGuid(),
            WorkOrderStatus: "Completed",
            WorkOrderPaid: 1000,
            WorkOrderBalance: 0,
            LatestPaymentId: Guid.NewGuid(),
            LatestReceiptId: Guid.NewGuid()));

        Assert.Equal("receipt-pending", next.Code);
        Assert.Equal("Emitir recibo", next.Label);
    }

    [Fact]
    public void Document_details_exposes_financial_review_without_hiding_excluded_amounts()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var contracts = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Application", "Commercial", "CommercialWorkspaceContracts.cs"));
        var service = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Services", "CommercialWorkspaceQueryService.cs"));
        var page = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Web", "Pages", "Documents", "Details.cshtml"));

        Assert.Contains("CommercialFinancialReviewView", contracts);
        Assert.Contains("ExcludedAmount", contracts);
        Assert.Contains("BuildFinancialReview", service);
        Assert.Contains("CommercialPaymentClassifier", service);
        Assert.Contains("Excluído por inconsistência", page);
        Assert.Contains("Consulta de histórico, comprovantes válidos existentes e estorno continuam disponíveis", page);
    }
}
