namespace OrcaFacil.Web.Models;

public sealed class InfrastructureUnavailableViewModel
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required string CorrelationId { get; init; }
    public bool CanRetry { get; init; } = true;
    public string SupportUrl { get; init; } = "/Support";
    public bool IsDevelopment { get; init; }
    public string? DeveloperHint { get; init; }
}
