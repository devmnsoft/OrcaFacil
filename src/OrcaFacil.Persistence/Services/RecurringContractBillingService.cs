using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Contracts;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Services;

/// <summary>Cross-account billing lifecycle for recurring client contracts. Before this service existed,
/// NextBillingDate was set once and never advanced, no competence was generated automatically, and
/// Overdue/Expired were only computed on read by reports.</summary>
public sealed class RecurringContractBillingService(OrcaFacilDbContext db) : IRecurringContractBillingService
{
    private const int BatchSize = 500;
    private const int MaxPeriodsPerRun = 12;

    public async Task<RecurringBillingRunResult> RunAsync(DateOnly today, CancellationToken ct = default)
    {
        var generated = await GenerateDueChargesAsync(today, ct);
        var overdue = await MarkOverdueAsync(today, ct);
        var expired = await ExpireEndedContractsAsync(today, ct);
        return new(generated, overdue, expired);
    }

    private async Task<int> GenerateDueChargesAsync(DateOnly today, CancellationToken ct)
    {
        var contracts = await db.RecurringContracts
            .Where(x => !x.IsDeleted && x.Status == ContractStatus.Active && x.NextBillingDate != null && x.NextBillingDate <= today)
            .OrderBy(x => x.NextBillingDate)
            .Take(BatchSize)
            .ToListAsync(ct);

        var generated = 0;
        foreach (var contract in contracts)
        {
            var next = contract.NextBillingDate!.Value;
            for (var i = 0; i < MaxPeriodsPerRun && RecurringBillingRules.ShouldGenerate(contract.Status, next, contract.EndDate, today); i++)
            {
                var competence = RecurringBillingRules.CompetenceOf(next);
                // The unique (account, contract, competence) index and this check keep a period from being charged twice,
                // including a period that was already registered by hand.
                var exists = await db.ContractPayments.AnyAsync(x => x.AccountId == contract.AccountId && x.ContractId == contract.Id && x.Competence == competence && !x.IsDeleted, ct);
                if (!exists)
                {
                    AddCharge(contract, competence, next);
                    generated++;
                }
                next = RecurringBillingRules.AdvanceDue(next, contract.Periodicity, contract.CustomPeriodMonths, contract.DueDay);
            }
            contract.NextBillingDate = next;
            contract.Touch();
        }

        await db.SaveChangesAsync(ct);
        return generated;
    }

    private async Task<int> MarkOverdueAsync(DateOnly today, CancellationToken ct)
    {
        var payments = await db.ContractPayments
            .Where(x => !x.IsDeleted && x.Status == RecurringPaymentStatus.Pending && x.DueDate < today)
            .OrderBy(x => x.DueDate)
            .Take(BatchSize)
            .ToListAsync(ct);
        if (payments.Count == 0) return 0;

        var ids = payments.Select(x => x.Id).ToList();
        var entries = await db.FinancialEntries
            .Where(x => !x.IsDeleted && x.Status == FinancialEntryStatus.Pending && x.ContractPaymentId != null && ids.Contains(x.ContractPaymentId.Value))
            .ToListAsync(ct);

        var marked = 0;
        foreach (var payment in payments)
        {
            if (!RecurringBillingRules.ShouldMarkOverdue(payment.Status, payment.DueDate, today)) continue;
            payment.Status = RecurringPaymentStatus.Overdue;
            payment.Touch();
            marked++;
            db.Add(new ActivityEvent
            {
                AccountId = payment.AccountId, Action = "RecurringChargeOverdue", EntityType = "RecurringContract", EntityId = payment.ContractId,
                Summary = $"Cobrança de {payment.Competence:MM/yyyy} vencida em {payment.DueDate:dd/MM/yyyy}."
            });
        }
        foreach (var entry in entries)
        {
            entry.Status = FinancialEntryStatus.Overdue;
            entry.Touch();
        }

        await db.SaveChangesAsync(ct);
        return marked;
    }

    private async Task<int> ExpireEndedContractsAsync(DateOnly today, CancellationToken ct)
    {
        var contracts = await db.RecurringContracts
            .Where(x => !x.IsDeleted && x.Status == ContractStatus.Active && x.EndDate != null && x.EndDate < today)
            .OrderBy(x => x.EndDate)
            .Take(BatchSize)
            .ToListAsync(ct);

        var expired = 0;
        foreach (var contract in contracts)
        {
            if (!RecurringBillingRules.ShouldExpire(contract.Status, contract.EndDate, today)) continue;
            contract.Status = ContractStatus.Expired;
            contract.Touch();
            expired++;
            db.Add(new ActivityEvent
            {
                AccountId = contract.AccountId, Action = "RecurringContractExpired", EntityType = "RecurringContract", EntityId = contract.Id,
                Summary = $"Contrato {contract.Number} expirou em {contract.EndDate:dd/MM/yyyy} sem renovação registrada."
            });
        }

        await db.SaveChangesAsync(ct);
        return expired;
    }

    private void AddCharge(RecurringContract contract, DateOnly competence, DateOnly dueDate)
    {
        var payment = new ContractPayment
        {
            AccountId = contract.AccountId, ContractId = contract.Id, ClientId = contract.ClientId, Competence = competence,
            DueDate = dueDate, Amount = contract.RecurringAmount, Status = RecurringPaymentStatus.Pending
        };
        db.Add(payment);
        // Same receivable the manual "Registrar previsão" path creates, so the charge shows in Financeiro.
        db.Add(new FinancialEntry
        {
            AccountId = contract.AccountId, ClientId = contract.ClientId, ContractId = contract.Id, ContractPaymentId = payment.Id,
            Origin = FinancialEntryOrigin.Contract, Description = $"{contract.Title} · {competence:MM/yyyy}",
            DueDate = dueDate, Amount = contract.RecurringAmount, Status = FinancialEntryStatus.Pending
        });
        db.Add(new ActivityEvent
        {
            AccountId = contract.AccountId, Action = "RecurringChargeGenerated", EntityType = "RecurringContract", EntityId = contract.Id,
            Summary = $"Cobrança de {competence:MM/yyyy} gerada, vencimento {dueDate:dd/MM/yyyy}."
        });
    }
}
