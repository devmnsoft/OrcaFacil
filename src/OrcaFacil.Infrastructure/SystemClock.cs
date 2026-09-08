using OrcaFacil.Application.Abstractions;

namespace OrcaFacil.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
