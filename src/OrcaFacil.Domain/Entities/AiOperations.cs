using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public sealed class AiUsageLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public Guid UserId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public int EstimatedTokens { get; set; }
    public decimal EstimatedCost { get; set; }
    public int DurationMs { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? SanitizedError { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class AiSuggestionCard : Entity
{
    public Guid AccountId { get; set; }
    public string DataJson { get; set; } = "{}";
}
