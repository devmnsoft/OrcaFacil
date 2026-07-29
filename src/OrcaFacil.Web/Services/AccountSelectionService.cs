using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

public sealed record AccountSelection(
    Guid AccountId,
    Guid AccountMemberId,
    string AccountName,
    string Role,
    string Status,
    string EffectivePlanCode);

public interface IAccountSelectionService
{
    Task<(AccountSelection? Selected, int AvailableAccounts)> SelectAsync(
        Guid userId,
        Guid? preferredAccountId,
        CancellationToken cancellationToken = default);
}

public sealed class AccountSelectionService(OrcaFacilDbContext db) : IAccountSelectionService
{
    public async Task<(AccountSelection? Selected, int AvailableAccounts)> SelectAsync(
        Guid userId,
        Guid? preferredAccountId,
        CancellationToken cancellationToken = default)
    {
        var memberships = await (from member in db.AccountMembers.AsNoTracking()
            join account in db.BusinessAccounts.AsNoTracking() on member.AccountId equals account.Id
            where member.UserId == userId && !member.IsDeleted && member.Status == AccountMemberStatus.Active &&
                  !account.IsDeleted && account.Status == AccountStatus.Active
            orderby preferredAccountId.HasValue && account.Id == preferredAccountId.Value descending,
                member.RoleCode == "Owner" descending,
                member.RoleCode == "Administrator" descending,
                member.JoinedAt,
                account.CreatedAt,
                account.Id,
                member.Id
            select new AccountSelection(account.Id, member.Id, account.DisplayName, member.RoleCode,
                account.Status.ToString(), account.CurrentPlanCode))
            .Take(2)
            .ToListAsync(cancellationToken);

        return (memberships.FirstOrDefault(), memberships.Count);
    }
}
