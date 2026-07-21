namespace OrcaFacil.Domain.ValueObjects;

public sealed record DateRange(DateTime StartsAt, DateTime EndsAt)
{
    public DateRange()
        : this(DateTime.UtcNow, DateTime.UtcNow)
    {
    }

    public bool Contains(DateTime date) => date >= StartsAt && date <= EndsAt;
}
