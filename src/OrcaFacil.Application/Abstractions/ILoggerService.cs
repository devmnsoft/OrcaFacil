namespace OrcaFacil.Application.Abstractions;

public interface ILoggerService
{
    Task RegisterAsync(Guid? userId, string eventName, object? metadata, CancellationToken ct = default);
}
