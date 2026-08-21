using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Analytics;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

public sealed record AnalyticsKpis(int QuotesCreated, int QuotesApproved, decimal OpenValue, decimal ApprovedValue, decimal Received, decimal Receivable, decimal Overdue, int WorkOrdersCompleted, int WorkOrdersLate, int ActiveClients, decimal AverageTicket);
public sealed record AnalyticsDashboard(AnalyticsKpis Current, AnalyticsKpis Previous, PeriodComparison ApprovedComparison, PeriodComparison ReceivedComparison, IReadOnlyList<RankingRow> Clients, ForecastResult CommercialForecast);
public sealed record RankingRow(string Label, decimal Value);
public sealed record QualityFindingView(string Severity, string Title, string Description, string ActionUrl);
public sealed record AccountHealthView(int Score, string Classification, IReadOnlyList<string> Positives, IReadOnlyList<string> Attention, IReadOnlyList<string> Actions);

public sealed class AnalyticsV21Service(OrcaFacilDbContext db, ICurrentAccountService currentAccount)
{
    public async Task<AnalyticsDashboard> DashboardAsync(DateOnly start, DateOnly end, CancellationToken ct)
    {
        var accountId = await RequireAccountAsync(ct);
        var current = await KpisAsync(accountId, start, end, ct);
        var previousPeriod = PeriodComparisonService.PreviousEquivalent(start, end);
        var previous = await KpisAsync(accountId, previousPeriod.Start, previousPeriod.End, ct);
        var topClients = await db.Documents.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.Type == DocumentType.Budget && x.ClientDecision == ClientDecision.Approved && DateOnly.FromDateTime(x.IssueDate) >= start && DateOnly.FromDateTime(x.IssueDate) <= end)
            .GroupBy(x => x.ClientName).Select(x => new RankingRow(x.Key, x.Sum(y => y.Total))).OrderByDescending(x => x.Value).Take(5).ToListAsync(ct);
        var open = await db.Documents.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.Type == DocumentType.Budget && x.ClientDecision == ClientDecision.Pending && x.Status != "Cancelled")
            .Select(x => new { x.Total, x.PublicEnabled, x.ValidUntil, x.ClientId }).ToListAsync(ct);
        var forecast = ForecastService.Calculate(open.Select(x => (x.Total, x.PublicEnabled ? 55 : 30, x.ClientId.HasValue)));
        return new(current, previous, PeriodComparisonService.Compare(current.ApprovedValue, previous.ApprovedValue), PeriodComparisonService.Compare(current.Received, previous.Received), topClients, forecast);
    }

    public async Task<IReadOnlyList<QualityFindingView>> DataQualityAsync(CancellationToken ct)
    {
        var accountId = await RequireAccountAsync(ct);
        var findings = new List<QualityFindingView>();
        var clients = await db.Clients.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.IsActive).ToListAsync(ct);
        findings.AddRange(clients.Where(x => string.IsNullOrWhiteSpace(x.DocumentNumber)).Select(x => new QualityFindingView("High", $"{x.Name}: documento ausente", "Cadastre CPF ou CNPJ para completar o cliente.", $"/Clients/Edit/{x.Id}")));
        findings.AddRange(clients.Where(x => string.IsNullOrWhiteSpace(x.Phone)).Select(x => new QualityFindingView("Medium", $"{x.Name}: telefone ausente", "Inclua um telefone para permitir o acompanhamento comercial.", $"/Clients/Edit/{x.Id}")));
        findings.AddRange(clients.Where(x => string.IsNullOrWhiteSpace(x.Email)).Select(x => new QualityFindingView("Medium", $"{x.Name}: e-mail ausente", "Inclua um e-mail válido para envio de documentos.", $"/Clients/Edit/{x.Id}")));
        var invalidQuotes = await db.Documents.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.Type == DocumentType.Budget && (!x.ValidUntil.HasValue || !x.Items.Any())).Select(x => new { x.Id, x.Number, x.ValidUntil }).ToListAsync(ct);
        findings.AddRange(invalidQuotes.Select(x => new QualityFindingView("High", $"Proposta {x.Number} incompleta", x.ValidUntil.HasValue ? "A proposta não possui itens." : "A proposta não possui validade.", $"/Documents/Edit/{x.Id}")));
        var unpaid = await db.WorkOrders.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.Status == WorkOrderStatus.Completed && !x.PaymentReceived).Select(x => new { x.Id, x.Number }).ToListAsync(ct);
        findings.AddRange(unpaid.Select(x => new QualityFindingView("High", $"OS {x.Number} concluída sem pagamento", "Registre o recebimento ou revise a situação financeira.", $"/WorkOrders/Details/{x.Id}")));
        return findings.OrderBy(x => x.Severity).ToList();
    }

    public async Task<AccountHealthView> AccountHealthAsync(CancellationToken ct)
    {
        var accountId = await RequireAccountAsync(ct);
        var positives = new List<string>(); var attention = new List<string>(); var actions = new List<string>(); var score = 0;
        async Task Factor(bool ok, int points, string positive, string problem, string action) { if (ok) { score += points; positives.Add(positive); } else { attention.Add(problem); actions.Add(action); } await Task.CompletedTask; }
        await Factor(await db.CompanyBrandingProfiles.AsNoTracking().AnyAsync(x => x.AccountId == accountId && !x.IsDeleted, ct), 15, "Identidade da empresa configurada", "Identidade da empresa incompleta", "Configure a identidade em Configurações.");
        await Factor(await db.ServiceCatalogItems.AsNoTracking().AnyAsync(x => x.AccountId == accountId && !x.IsDeleted && x.IsActive, ct), 15, "Catálogo de serviços ativo", "Nenhum serviço ativo", "Cadastre ao menos um serviço.");
        await Factor(await db.Clients.AsNoTracking().AnyAsync(x => x.AccountId == accountId && !x.IsDeleted && x.IsActive, ct), 15, "Base de clientes ativa", "Nenhum cliente ativo", "Cadastre seu primeiro cliente.");
        await Factor(await db.Documents.AsNoTracking().AnyAsync(x => x.AccountId == accountId && !x.IsDeleted && x.PublicEnabled, ct), 15, "Propostas já enviadas", "Nenhuma proposta enviada", "Envie uma proposta por link público.");
        var overdue = await db.FinancialEntries.AsNoTracking().CountAsync(x => x.AccountId == accountId && !x.IsDeleted && x.Status == FinancialEntryStatus.Overdue, ct);
        await Factor(overdue == 0, 20, "Sem recebíveis vencidos", $"{overdue} recebível(is) vencido(s)", "Revise a régua de cobrança.");
        var late = await db.WorkOrders.AsNoTracking().CountAsync(x => x.AccountId == accountId && !x.IsDeleted && x.Status == WorkOrderStatus.Overdue, ct);
        await Factor(late == 0, 20, "Sem ordens de serviço atrasadas", $"{late} OS atrasada(s)", "Replaneje as OS em atraso.");
        var classification = score >= 80 ? "Saudável" : score >= 60 ? "Atenção leve" : score >= 40 ? "Atenção" : "Crítico";
        return new(score, classification, positives, attention, actions.Distinct().ToList());
    }

    private async Task<AnalyticsKpis> KpisAsync(Guid accountId, DateOnly start, DateOnly end, CancellationToken ct)
    {
        var quotes = db.Documents.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.Type == DocumentType.Budget && DateOnly.FromDateTime(x.IssueDate) >= start && DateOnly.FromDateTime(x.IssueDate) <= end);
        var created = await quotes.CountAsync(ct); var approved = await quotes.CountAsync(x => x.ClientDecision == ClientDecision.Approved, ct);
        var approvedValue = await quotes.Where(x => x.ClientDecision == ClientDecision.Approved).SumAsync(x => x.Total, ct);
        var openValue = await quotes.Where(x => x.ClientDecision == ClientDecision.Pending).SumAsync(x => x.Total, ct);
        var payments = db.ManualPayments.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.Status == FinancialRecordStatus.Active && DateOnly.FromDateTime(x.PaidAt) >= start && DateOnly.FromDateTime(x.PaidAt) <= end);
        var received = await payments.SumAsync(x => x.Amount, ct);
        var receivable = await db.FinancialEntries.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.Status != FinancialEntryStatus.Canceled && x.Status != FinancialEntryStatus.Paid).SumAsync(x => x.Amount - x.PaidAmount, ct);
        var overdue = await db.FinancialEntries.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.Status == FinancialEntryStatus.Overdue).SumAsync(x => x.Amount - x.PaidAmount, ct);
        var completed = await db.WorkOrders.AsNoTracking().CountAsync(x => x.AccountId == accountId && !x.IsDeleted && x.Status == WorkOrderStatus.Completed && x.CompletedAt.HasValue && DateOnly.FromDateTime(x.CompletedAt.Value) >= start && DateOnly.FromDateTime(x.CompletedAt.Value) <= end, ct);
        var late = await db.WorkOrders.AsNoTracking().CountAsync(x => x.AccountId == accountId && !x.IsDeleted && x.Status == WorkOrderStatus.Overdue, ct);
        var clients = await db.Clients.AsNoTracking().CountAsync(x => x.AccountId == accountId && !x.IsDeleted && x.IsActive, ct);
        return new(created, approved, openValue, approvedValue, received, receivable, overdue, completed, late, clients, approved == 0 ? 0 : approvedValue / approved);
    }

    private async Task<Guid> RequireAccountAsync(CancellationToken ct) { await currentAccount.EnsureAccountAccessAsync(ct); return currentAccount.AccountId ?? throw new UnauthorizedAccessException("Selecione uma conta."); }
}
