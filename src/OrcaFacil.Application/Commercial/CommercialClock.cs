namespace OrcaFacil.Application.Commercial;

/// <summary>
/// Normalizes commercial instants to UTC using the account business zone.
/// Unspecified values are civil time in America/Sao_Paulo, never the server zone.
/// </summary>
public static class CommercialClock
{
    public static TimeZoneInfo BusinessTimeZone { get; } = ResolveBusinessTimeZone();

    public static DateTime NormalizeToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), BusinessTimeZone)
    };

    public static bool SameUtcSecond(DateTime left, DateTime right)
    {
        var a = left.Kind == DateTimeKind.Utc ? left : DateTime.SpecifyKind(left, DateTimeKind.Utc);
        var b = right.Kind == DateTimeKind.Utc ? right : DateTime.SpecifyKind(right, DateTimeKind.Utc);
        return a.Ticks / TimeSpan.TicksPerSecond == b.Ticks / TimeSpan.TicksPerSecond;
    }

    private static TimeZoneInfo ResolveBusinessTimeZone()
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        return TimeZoneInfo.Utc;
    }
}
