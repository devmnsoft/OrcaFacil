namespace OrcaFacil.Application.Abstractions;

public interface ICurrentUserService
{
    Guid UserId { get; }
    Guid? TryGetUserId();
}
