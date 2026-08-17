namespace OrcaFacil.Application.Abstractions;

public interface IPermissionService
{
    Task<bool> IsGrantedAsync(string permissionCode, CancellationToken cancellationToken = default);
    Task EnsureGrantedAsync(string permissionCode, CancellationToken cancellationToken = default);
}
