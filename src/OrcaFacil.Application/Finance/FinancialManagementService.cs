namespace OrcaFacil.Application.Finance;

public enum FinancialRegime { Cash, Accrual }
public enum ManagementEntryKind { Revenue, Expense, Transfer, Adjustment, Reversal, Provision, Appropriation }
public enum ProjectionConfidence { Confirmed, Expected, Scenario }

public sealed record ManagementEntry(
    Guid AccountId, Guid Id, ManagementEntryKind Kind, decimal Amount,
    DateOnly CompetenceDate, DateOnly? RealizationDate, DateOnly DueDate,
    Guid? ChartAccountId = null, Guid? CostCenterId = null,
    bool IsCanceled = false, string? ManualReason = null);

public sealed record ProjectionEntry(
    Guid AccountId, Guid Id, decimal Amount, DateOnly DueDate, bool IsRevenue,
    ProjectionConfidence Confidence, string Basis, decimal RiskFactor = 1m);

public sealed record CashFlowSummary(decimal OpeningBalance, decimal Inflows, decimal Outflows, decimal ClosingBalance);
public sealed record DreSummary(decimal GrossRevenue, decimal Deductions, decimal NetRevenue, decimal DirectCosts, decimal GrossMargin, decimal OperatingExpenses, decimal ManagementResult, FinancialRegime Regime);
public sealed record AllocationInput(Guid CostCenterId, decimal Percentage, decimal Amount);
public sealed record ClosingChecklist(bool EntriesReconciled, bool ReceivablesReviewed, bool PayablesReviewed, bool PaymentsConfirmed, bool FiscalDocumentsReviewed, bool DreGenerated, bool CashFlowGenerated, bool BudgetReviewed, bool AdjustmentsRegistered, bool Approved)
{
    public bool IsComplete => EntriesReconciled && ReceivablesReviewed && PayablesReviewed && PaymentsConfirmed && FiscalDocumentsReviewed && DreGenerated && CashFlowGenerated && BudgetReviewed && AdjustmentsRegistered && Approved;
}

/// <summary>Deterministic management calculations over tenant-scoped, persisted financial facts.</summary>
public sealed class FinancialManagementService
{
    public const string ManagementDisclaimer = "Os relatórios financeiros desta área são gerenciais. A escrituração contábil oficial depende da contabilidade da empresa e de integrações específicas configuradas.";

    public CashFlowSummary CalculateCashFlow(Guid accountId, decimal openingBalance, DateOnly start, DateOnly end, IEnumerable<ManagementEntry> entries)
    {
        ValidatePeriod(accountId, start, end);
        var realized = Scope(accountId, entries).Where(x => !x.IsCanceled && x.RealizationDate >= start && x.RealizationDate <= end && x.Kind is not ManagementEntryKind.Transfer and not ManagementEntryKind.Provision);
        var inflows = realized.Where(x => x.Kind is ManagementEntryKind.Revenue or ManagementEntryKind.Appropriation).Sum(x => x.Amount);
        var outflows = realized.Where(x => x.Kind is ManagementEntryKind.Expense or ManagementEntryKind.Reversal).Sum(x => x.Amount);
        return new(openingBalance, inflows, outflows, openingBalance + inflows - outflows);
    }

    public IReadOnlyList<(DateOnly Date, decimal Confirmed, decimal Expected, decimal Scenario)> Project(Guid accountId, DateOnly start, DateOnly end, IEnumerable<ProjectionEntry> entries)
    {
        ValidatePeriod(accountId, start, end);
        return entries.Where(x => x.AccountId == accountId && x.DueDate >= start && x.DueDate <= end)
            .Select(x => { if (string.IsNullOrWhiteSpace(x.Basis)) throw new InvalidOperationException("A base da projeção é obrigatória."); if (x.RiskFactor is < 0m or > 1m) throw new InvalidOperationException("O fator de risco deve estar entre zero e um."); return x; })
            .GroupBy(x => x.DueDate).OrderBy(x => x.Key)
            .Select(g => (g.Key,
                Signed(g.Where(x => x.Confidence == ProjectionConfidence.Confirmed)),
                Signed(g.Where(x => x.Confidence == ProjectionConfidence.Expected)),
                Signed(g.Where(x => x.Confidence == ProjectionConfidence.Scenario))))
            .ToArray();
    }

    public DreSummary CalculateDre(Guid accountId, DateOnly start, DateOnly end, FinancialRegime regime, IEnumerable<ManagementEntry> entries, ISet<Guid> revenue, ISet<Guid> deductions, ISet<Guid> directCosts, ISet<Guid> operatingExpenses)
    {
        ValidatePeriod(accountId, start, end);
        var facts = Scope(accountId, entries).Where(x => !x.IsCanceled && DateFor(x, regime) is { } date && date >= start && date <= end).ToArray();
        decimal Sum(ISet<Guid> accounts) => facts.Where(x => x.ChartAccountId.HasValue && accounts.Contains(x.ChartAccountId.Value)).Sum(x => x.Amount);
        var gross = Sum(revenue); var deduction = Sum(deductions); var costs = Sum(directCosts); var expenses = Sum(operatingExpenses); var net = gross - deduction;
        return new(gross, deduction, net, costs, net - costs, expenses, net - costs - expenses, regime);
    }

    public static void ValidateManualEntry(ManagementEntry entry, bool canMakeTechnicalAdjustment)
    {
        if (entry.AccountId == Guid.Empty) throw new InvalidOperationException("AccountId é obrigatório.");
        if (entry.Amount <= 0m && !(entry.Kind == ManagementEntryKind.Adjustment && canMakeTechnicalAdjustment)) throw new InvalidOperationException("O valor deve ser maior que zero.");
        if (string.IsNullOrWhiteSpace(entry.ManualReason)) throw new InvalidOperationException("Lançamento manual exige motivo.");
    }

    public static void ValidateAllocation(decimal entryAmount, IEnumerable<AllocationInput> allocations, string? manualReason = null)
    {
        var rows = allocations.ToArray();
        if (rows.Length == 0 || rows.Any(x => x.CostCenterId == Guid.Empty || x.Percentage <= 0m || x.Amount < 0m) || rows.Select(x => x.CostCenterId).Distinct().Count() != rows.Length || rows.Sum(x => x.Percentage) != 100m || rows.Sum(x => x.Amount) != entryAmount)
            throw new InvalidOperationException("O rateio deve ser único por centro, fechar em 100% e no valor integral do lançamento.");
        if (manualReason is not null && string.IsNullOrWhiteSpace(manualReason)) throw new InvalidOperationException("Rateio manual exige motivo.");
    }

    public static void ValidateClosing(ClosingChecklist checklist) { if (!checklist.IsComplete) throw new InvalidOperationException("O fechamento exige a conclusão e aprovação de todo o checklist."); }
    public static void ValidateReopening(string reason, bool hasPermission) { if (!hasPermission) throw new UnauthorizedAccessException("Permissão de reabertura obrigatória."); if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Reabertura exige motivo."); }

    private static IEnumerable<ManagementEntry> Scope(Guid accountId, IEnumerable<ManagementEntry> entries) { if (accountId == Guid.Empty) throw new InvalidOperationException("AccountId é obrigatório."); return entries.Where(x => x.AccountId == accountId); }
    private static void ValidatePeriod(Guid accountId, DateOnly start, DateOnly end) { if (accountId == Guid.Empty) throw new InvalidOperationException("AccountId é obrigatório."); if (end < start) throw new ArgumentException("Período inválido."); }
    private static DateOnly? DateFor(ManagementEntry entry, FinancialRegime regime) => regime == FinancialRegime.Cash ? entry.RealizationDate : entry.CompetenceDate;
    private static decimal Signed(IEnumerable<ProjectionEntry> entries) => entries.Sum(x => (x.IsRevenue ? 1m : -1m) * x.Amount * x.RiskFactor);
}
