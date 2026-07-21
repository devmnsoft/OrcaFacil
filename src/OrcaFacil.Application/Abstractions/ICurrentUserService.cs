namespace OrcaFacil.Application.Abstractions;

public interface ICurrentUserService
{
    Guid UserId { get; }
    Guid? TryGetUserId();
    string? Email { get; }
    string? Name { get; }
    string? Role { get; }
    string? Plan { get; }
    bool IsAuthenticated { get; }
    bool IsSuperAdmin { get; }
}
