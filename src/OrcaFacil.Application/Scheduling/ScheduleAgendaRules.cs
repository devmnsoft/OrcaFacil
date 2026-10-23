using OrcaFacil.Application.Commercial;

namespace OrcaFacil.Application.Scheduling;

/// <summary>A minimal, DbContext-free description of one scheduled block, used for overlap detection.</summary>
public sealed record ScheduleSlot(Guid Id, Guid? ResourceId, DateTime? StartUtc, DateTime? EndUtc);

/// <summary>
/// Pure rules for the operational agenda. Free of the DbContext so the two things the operator actually
/// trusts here are unit-testable: which civil day a block belongs to (always America/Sao_Paulo by default,
/// never the server zone) and which blocks double-book the same person. The overlap predicate mirrors the
/// strict semantics that <c>FieldScheduleService</c> enforces at save time, so the agenda never disagrees
/// with the dispatcher. Time-zone overloads exist so the logic can be proven with a fixed offset in tests.
/// </summary>
public static class ScheduleAgendaRules
{
    /// <summary>The civil calendar day of an instant, in the account business zone (America/Sao_Paulo).</summary>
    public static DateOnly BusinessToday(DateTime now) => BusinessToday(now, CommercialClock.BusinessTimeZone);

    public static DateOnly BusinessToday(DateTime now, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(ToUtc(now), zone));

    /// <summary>The UTC instant that opens the given business-zone civil day (local midnight projected to UTC).</summary>
    public static DateTime CivilDayStartUtc(DateOnly businessDay) => CivilDayStartUtc(businessDay, CommercialClock.BusinessTimeZone);

    public static DateTime CivilDayStartUtc(DateOnly businessDay, TimeZoneInfo zone)
    {
        var localMidnight = new DateTime(businessDay.Year, businessDay.Month, businessDay.Day, 0, 0, 0, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localMidnight, zone);
    }

    /// <summary>The half-open [startUtc, endUtc) span covering <paramref name="days"/> civil days beginning at <paramref name="businessDay"/>.</summary>
    public static (DateTime StartUtc, DateTime EndUtc) Window(DateOnly businessDay, int days) => Window(businessDay, days, CommercialClock.BusinessTimeZone);

    public static (DateTime StartUtc, DateTime EndUtc) Window(DateOnly businessDay, int days, TimeZoneInfo zone) =>
        (CivilDayStartUtc(businessDay, zone), CivilDayStartUtc(businessDay.AddDays(days), zone));

    /// <summary>A block only participates in the agenda (or a conflict) if it has a real, positive time window.</summary>
    public static bool HasWindow(ScheduleSlot slot) =>
        slot.StartUtc is not null && slot.EndUtc is not null && slot.EndUtc > slot.StartUtc;

    /// <summary>Ids of blocks that overlap at least one other block booked to the SAME person (double-booking).</summary>
    public static ISet<Guid> FindConflicts(IEnumerable<ScheduleSlot> slots)
    {
        var conflicted = new HashSet<Guid>();
        var list = slots.Where(HasWindow).ToList();
        for (var i = 0; i < list.Count; i++)
        {
            var a = list[i];
            if (a.ResourceId is null) continue;
            for (var j = i + 1; j < list.Count; j++)
            {
                var b = list[j];
                if (b.ResourceId is null || b.ResourceId != a.ResourceId) continue;
                if (a.StartUtc < b.EndUtc && b.StartUtc < a.EndUtc)
                {
                    conflicted.Add(a.Id);
                    conflicted.Add(b.Id);
                }
            }
        }
        return conflicted;
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value.ToUniversalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => value,
    };
}
