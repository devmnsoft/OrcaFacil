namespace OrcaFacil.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public bool IsDeleted { get; protected set; }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void MarkAsDeleted()
    {
        IsDeleted = true;
        Touch();
    }
}

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
