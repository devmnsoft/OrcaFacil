using OrcaFacil.Application.Abstractions;

namespace OrcaFacil.Application.Services;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
