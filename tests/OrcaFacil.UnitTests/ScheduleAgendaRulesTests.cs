using OrcaFacil.Application.Scheduling;
using Xunit;

namespace OrcaFacil.UnitTests;

/// <summary>
/// Agenda correctness. Before these rules the "today/week" views bucketed by the server's UTC day and the
/// screen showed server-local times, so late-evening jobs landed in the wrong day; and double-bookings of a
/// single person were invisible until save. These tests pin the civil-day math and the per-person overlap rule.
/// </summary>
public sealed class ScheduleAgendaRulesTests
{
    private static readonly TimeZoneInfo Minus3 = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    private static readonly TimeZoneInfo UtcZone = TimeZoneInfo.Utc;

    private static DateTime U(int y, int mo, int d, int h = 0) => new(y, mo, d, h, 0, 0, DateTimeKind.Utc);
    private static ScheduleSlot Slot(Guid? resource, DateTime? start, DateTime? end) => new(Guid.NewGuid(), resource, start, end);

    [Fact]
    public void Business_day_follows_the_business_zone_not_the_utc_wall_date()
    {
        // Three hours behind UTC: 02:00Z on May 1 is still 23:00 on Apr 30 -> previous civil day.
        Assert.Equal(new DateOnly(2026, 4, 30), ScheduleAgendaRules.BusinessToday(U(2026, 5, 1, 2), Minus3));
        // 04:00Z is 01:00 on May 1 locally -> May 1.
        Assert.Equal(new DateOnly(2026, 5, 1), ScheduleAgendaRules.BusinessToday(U(2026, 5, 1, 4), Minus3));
        Assert.Equal(new DateOnly(2026, 5, 1), ScheduleAgendaRules.BusinessToday(U(2026, 5, 1, 12), Minus3));
    }

    [Fact]
    public void Business_day_matches_wall_date_when_instant_is_utc()
    {
        Assert.Equal(new DateOnly(2026, 5, 1), ScheduleAgendaRules.BusinessToday(U(2026, 5, 1, 2), UtcZone));
    }

    [Fact]
    public void Civil_day_start_is_local_midnight_projected_to_utc()
    {
        // Local midnight in UTC-3 is 03:00 UTC the same calendar day; in UTC it is midnight.
        Assert.Equal(U(2026, 5, 1, 3), ScheduleAgendaRules.CivilDayStartUtc(new DateOnly(2026, 5, 1), Minus3));
        Assert.Equal(U(2026, 5, 1), ScheduleAgendaRules.CivilDayStartUtc(new DateOnly(2026, 5, 1), UtcZone));
    }

    [Fact]
    public void Window_spans_consecutive_civil_days_aligned_to_the_zone()
    {
        var (weekStart, weekEnd) = ScheduleAgendaRules.Window(new DateOnly(2026, 5, 1), 7, Minus3);
        Assert.Equal(U(2026, 5, 1, 3), weekStart);
        Assert.Equal(U(2026, 5, 8, 3), weekEnd);
        Assert.Equal(TimeSpan.FromDays(7), weekEnd - weekStart);

        var (dayStart, dayEnd) = ScheduleAgendaRules.Window(new DateOnly(2026, 5, 1), 1, Minus3);
        Assert.Equal(TimeSpan.FromDays(1), dayEnd - dayStart);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Has_window_requires_a_positive_span(bool valid)
    {
        var slot = valid
            ? Slot(Guid.NewGuid(), U(2026, 5, 1, 9), U(2026, 5, 1, 11))
            : Slot(Guid.NewGuid(), U(2026, 5, 1, 11), U(2026, 5, 1, 9));
        Assert.Equal(valid, ScheduleAgendaRules.HasWindow(slot));
        Assert.False(ScheduleAgendaRules.HasWindow(Slot(Guid.NewGuid(), U(2026, 5, 1, 9), null)));
    }

    [Fact]
    public void Conflict_flags_only_overlaps_booked_to_the_same_person()
    {
        var tech = Guid.NewGuid();
        var other = Guid.NewGuid();
        var a = Slot(tech, U(2026, 5, 1, 9), U(2026, 5, 1, 12));   // 09-12
        var b = Slot(tech, U(2026, 5, 1, 11), U(2026, 5, 1, 13));  // 11-13, overlaps a
        var c = Slot(tech, U(2026, 5, 1, 14), U(2026, 5, 1, 16));  // 14-16, clear gap from b
        var d = Slot(other, U(2026, 5, 1, 9), U(2026, 5, 1, 13));  // different person, overlaps a in time
        var unassigned = Slot(null, U(2026, 5, 1, 10), U(2026, 5, 1, 11)); // no person to double-book
        var bad = Slot(tech, U(2026, 5, 1, 11), U(2026, 5, 1, 9)); // invalid window (end before start)

        var conflicts = ScheduleAgendaRules.FindConflicts(new[] { a, b, c, d, unassigned, bad });

        Assert.Contains(a.Id, conflicts);
        Assert.Contains(b.Id, conflicts);
        Assert.DoesNotContain(c.Id, conflicts);
        Assert.DoesNotContain(d.Id, conflicts);
        Assert.DoesNotContain(unassigned.Id, conflicts);
        Assert.DoesNotContain(bad.Id, conflicts);
        Assert.Equal(2, conflicts.Count);
    }

    [Fact]
    public void Adjacent_windows_do_not_conflict()
    {
        var tech = Guid.NewGuid();
        var a = Slot(tech, U(2026, 5, 1, 9), U(2026, 5, 1, 12));
        var b = Slot(tech, U(2026, 5, 1, 12), U(2026, 5, 1, 14)); // starts exactly when a ends
        var conflicts = ScheduleAgendaRules.FindConflicts(new[] { a, b });
        Assert.Empty(conflicts);
    }

    [Fact]
    public void No_conflict_with_empty_or_single_input()
    {
        Assert.Empty(ScheduleAgendaRules.FindConflicts(Array.Empty<ScheduleSlot>()));
        var solo = Slot(Guid.NewGuid(), U(2026, 5, 1, 9), U(2026, 5, 1, 11));
        Assert.Empty(ScheduleAgendaRules.FindConflicts(new[] { solo }));
    }
}
