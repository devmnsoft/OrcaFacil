using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

public sealed class CurrentAccountService(
    IHttpContextAccessor httpContextAccessor,
    OrcaFacilDbContext db) : ICurrentAccountService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid UserId => ParseGuid(ClaimTypes.NameIdentifier, "sub")
        ?? throw new UnauthorizedAccessException("Usuário não autenticado.");
    public Guid? AccountId => ParseGuid("account_id");
    public Guid? AccountMemberId => ParseGuid("account_member_id");
    public string? AccountRoleCode => Principal?.FindFirstValue("account_role");
    public AccountStatus? AccountStatus => Enum.TryParse<AccountStatus>(Principal?.FindFirstValue("account_status"), true, out var status) ? status : null;
    public bool IsPlatformUser => Principal?.Claims.Any(x => x.Type == ClaimTypes.Role &&
        x.Value is "SuperAdministrator" or "SuperAdmin" or "PlatformSupport" or "PlatformFinance" or "PlatformAuditor") == true;
    public bool HasAccount => AccountId.HasValue && AccountMemberId.HasValue;

    public async Task EnsureAccountAccessAsync(CancellationToken ct = default)
    {
        if (Principal?.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException("Usuário não autenticado.");

        if (!HasAccount)
        {
            if (IsPlatformUser) return;
            throw new UnauthorizedAccessException("Selecione uma conta ativa para continuar.");
        }

        var valid = await (from member in db.AccountMembers.AsNoTracking()
                           join account in db.BusinessAccounts.AsNoTracking() on member.AccountId equals account.Id
                           where member.Id == AccountMemberId && member.UserId == UserId && member.AccountId == AccountId &&
                                 !member.IsDeleted && member.Status == AccountMemberStatus.Active &&
                                 !account.IsDeleted && account.Status == OrcaFacil.Domain.Enums.AccountStatus.Active
                           select member.Id).AnyAsync(ct);
        if (!valid) throw new UnauthorizedAccessException("O vínculo com a conta não está ativo.");
    }

    public async Task<bool> HasPermissionAsync(string permissionCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(permissionCode)) return false;
        await EnsureAccountAccessAsync(ct);
        if (IsPlatformUser && !HasAccount) return false;

        return await (from member in db.AccountMembers.AsNoTracking()
                      join role in db.Roles.AsNoTracking() on member.RoleCode equals role.Code
                      join rolePermission in db.RolePermissions.AsNoTracking() on role.Id equals rolePermission.RoleId
                      join permission in db.Permissions.AsNoTracking() on rolePermission.PermissionId equals permission.Id
                      where member.Id == AccountMemberId && member.AccountId == AccountId && member.UserId == UserId &&
                            member.Status == AccountMemberStatus.Active && !member.IsDeleted &&
                            permission.Code == permissionCode && !permission.IsDeleted
                      select permission.Id).AnyAsync(ct);
    }

    private Guid? ParseGuid(params string[] claimTypes)
    {
        foreach (var type in claimTypes)
            if (Guid.TryParse(Principal?.FindFirstValue(type), out var value)) return value;
        return null;
    }
}
