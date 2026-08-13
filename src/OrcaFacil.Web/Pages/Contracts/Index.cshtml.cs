using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Contracts;

[Authorize]
public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService account) : PageModel
{
    public IReadOnlyList<Row> Items { get; private set; } = [];
    public int ActiveCount { get; private set; }
    public decimal MonthlyRevenue { get; private set; }
    public async Task OnGetAsync(string? status, Guid? clientId, CancellationToken ct)
    {
        var query = db.RecurringContracts.AsNoTracking().Where(x => x.AccountId == account.AccountId && !x.IsDeleted);
        if (Enum.TryParse<ContractStatus>(status, true, out var parsed)) query = query.Where(x => x.Status == parsed);
        if (clientId.HasValue) query = query.Where(x => x.ClientId == clientId);
        Items = await (from contract in query join client in db.Clients.AsNoTracking() on contract.ClientId equals client.Id
            orderby contract.EndDate, contract.Title select new Row(contract.Id, contract.Number, contract.Title, client.Name, contract.Status,
                contract.RecurringAmount, contract.Periodicity, contract.StartDate, contract.EndDate, contract.NextBillingDate)).ToListAsync(ct);
        ActiveCount = Items.Count(x => x.Status == ContractStatus.Active);
        MonthlyRevenue = Items.Where(x => x.Status == ContractStatus.Active).Sum(x => MonthlyEquivalent(x.Amount, x.Periodicity));
    }
    private static decimal MonthlyEquivalent(decimal value, RecurrencePeriod period) => period switch { RecurrencePeriod.Bimonthly => value / 2, RecurrencePeriod.Quarterly => value / 3, RecurrencePeriod.Semiannual => value / 6, RecurrencePeriod.Annual => value / 12, _ => value };
    public sealed record Row(Guid Id, string Number, string Title, string Client, ContractStatus Status, decimal Amount, RecurrencePeriod Periodicity, DateOnly StartDate, DateOnly? EndDate, DateOnly? NextBillingDate);
}
