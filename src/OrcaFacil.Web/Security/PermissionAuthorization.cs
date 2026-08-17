using Microsoft.AspNetCore.Authorization;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Security;

namespace OrcaFacil.Web.Security;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

public sealed class PermissionService(ICurrentAccountService currentAccount) : IPermissionService
{
    public Task<bool> IsGrantedAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        if (!PermissionCodes.All.Contains(permissionCode) || !currentAccount.HasAccount) return Task.FromResult(false);
        return currentAccount.HasPermissionAsync(permissionCode, cancellationToken);
    }

    public async Task EnsureGrantedAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        if (!await IsGrantedAsync(permissionCode, cancellationToken))
            throw new UnauthorizedAccessException("Você não possui permissão para executar esta ação.");
    }
}

public sealed class PermissionAuthorizationHandler(IPermissionService permissions)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (await permissions.IsGrantedAsync(requirement.Permission)) context.Succeed(requirement);
    }
}
