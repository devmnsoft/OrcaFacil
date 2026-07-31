using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.DTOs;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

public sealed record SwitchableAccount(Guid AccountId, string Name, string Role, string Status,
    string PlanCode, bool IsCurrent, bool WasLastUsed);

public sealed record AccountSwitchResult(bool Succeeded, string? Error, SwitchableAccount? Account)
{
    public static AccountSwitchResult Invalid(string message) => new(false, message, null);
}

public interface IAccountSwitcherService
{
    Task<IReadOnlyList<SwitchableAccount>> GetAuthorizedAsync(Guid userId, Guid? currentAccountId,
        CancellationToken cancellationToken = default);
    Task<AccountSwitchResult> SwitchAsync(HttpContext context, Guid requestedAccountId,
        CancellationToken cancellationToken = default);
}

public sealed class AccountSwitcherService(
    OrcaFacilDbContext db,
    IUserSignInService signIn,
    ILogger<AccountSwitcherService> logger) : IAccountSwitcherService
{
    public async Task<IReadOnlyList<SwitchableAccount>> GetAuthorizedAsync(Guid userId, Guid? currentAccountId,
        CancellationToken cancellationToken = default)
    {
        return await (from member in db.AccountMembers.AsNoTracking()
            join account in db.BusinessAccounts.AsNoTracking() on member.AccountId equals account.Id
            where member.UserId == userId && !member.IsDeleted && !account.IsDeleted
            orderby account.Id == currentAccountId descending, account.DisplayName
            select new SwitchableAccount(account.Id, account.DisplayName, member.RoleCode,
                account.Status.ToString(), account.CurrentPlanCode, account.Id == currentAccountId,
                account.Id == currentAccountId))
            .ToListAsync(cancellationToken);
    }

    public async Task<AccountSwitchResult> SwitchAsync(HttpContext context, Guid requestedAccountId,
        CancellationToken cancellationToken = default)
    {
        var userIdText = context.User.FindFirst("user_id")?.Value;
        if (!Guid.TryParse(userIdText, out var userId))
            return AccountSwitchResult.Invalid("Sua sessão expirou. Entre novamente.");

        // The account boundary is resolved from the authenticated user membership; the posted id
        // is only a lookup key and is never accepted as proof of authorization.
        var authorized = await (from member in db.AccountMembers
            join account in db.BusinessAccounts on member.AccountId equals account.Id
            where member.UserId == userId && member.AccountId == requestedAccountId &&
                  !member.IsDeleted && member.Status == AccountMemberStatus.Active &&
                  !account.IsDeleted && account.Status == AccountStatus.Active
            select new { Member = member, Account = account }).SingleOrDefaultAsync(cancellationToken);

        if (authorized is null)
        {
            logger.LogWarning("ACCOUNT_SWITCH_DENIED UserId {UserId} RequestedAccountId {AccountId}", userId, requestedAccountId);
            return AccountSwitchResult.Invalid("Esta conta não está disponível para o seu usuário.");
        }

        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId && x.IsActive && !x.IsDeleted,
            cancellationToken);
        if (user is null) return AccountSwitchResult.Invalid("Usuário não encontrado ou inativo.");

        var previousId = context.User.FindFirst("account_id")?.Value;
        db.ActivityEvents.Add(new ActivityEvent
        {
            AccountId = authorized.Account.Id, ActorUserId = userId, Action = "AccountSwitched",
            EntityType = "BusinessAccount", EntityId = authorized.Account.Id, Result = "Success",
            Summary = $"Conta ativa alterada para {authorized.Account.DisplayName}."
        });
        db.AuditLogs.Add(new AuditLog
        {
            AccountId = authorized.Account.Id, UserId = userId, Action = "AccountSwitched",
            EntityType = "BusinessAccount", EntityId = authorized.Account.Id.ToString(),
            BeforeJson = JsonSerializer.Serialize(new { AccountId = previousId }),
            AfterJson = JsonSerializer.Serialize(new { AccountId = authorized.Account.Id }),
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers.UserAgent.ToString()
        });
        await db.SaveChangesAsync(cancellationToken);

        var summary = new UserSummaryDto(user.Id, user.Name, user.Email, user.Role.ToString(),
            user.Plan.ToString(), user.SessionVersion);
        await signIn.SignInAsync(context, summary, authorized.Account.Id,
            persistent: false, cancellationToken: cancellationToken);

        return new AccountSwitchResult(true, null,
            new SwitchableAccount(authorized.Account.Id, authorized.Account.DisplayName, authorized.Member.RoleCode,
                authorized.Account.Status.ToString(), authorized.Account.CurrentPlanCode, true, true));
    }
}
