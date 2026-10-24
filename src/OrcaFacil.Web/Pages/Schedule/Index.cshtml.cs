using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Scheduling;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Schedule;

/// <summary>One agenda row pre-shaped for the view: times already projected to the business zone and a conflict flag.</summary>
public sealed record ScheduleItemView(
    Guid Id, string Number, string Title, string Status,
    string? StartLabel, string? StartDayLabel, string? EndLabel,
    bool HasResource, bool Conflict);

[Authorize]
public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService account) : PageModel
{
    public IReadOnlyList<ScheduleItemView> Items { get; private set; } = [];
    public int ConflictCount { get; private set; }
    public string View { get; private set; } = "week";

    public async Task OnGetAsync(string view = "week", WorkOrderStatus? status = null, CancellationToken ct = default)
    {
        View = view;
        var nowUtc = DateTime.UtcNow;
        var today = ScheduleAgendaRules.BusinessToday(nowUtc);
        var (startUtc, endUtc) = view switch
        {
            "today" => ScheduleAgendaRules.Window(today, 1),
            "week" => ScheduleAgendaRules.Window(today, 7),
            _ => ScheduleAgendaRules.Window(today, 7),
        };

        var query = db.WorkOrders.AsNoTracking()
            .Where(x => x.AccountId == account.AccountId && !x.IsDeleted
                        && x.Status != WorkOrderStatus.Completed && x.Status != WorkOrderStatus.Cancelled);

        query = view switch
        {
            "overdue" => query.Where(x => x.ScheduledEnd < nowUtc),
            "unscheduled" => query.Where(x => x.ScheduledStart == null),
            _ => query.Where(x => x.ScheduledStart >= startUtc && x.ScheduledStart < endUtc),
        };
        if (status.HasValue) query = query.Where(x => x.Status == status);

        var orders = await query.OrderBy(x => x.ScheduledStart).Take(100).ToListAsync(ct);
        var conflicts = ScheduleAgendaRules.FindConflicts(orders.Select(o => new ScheduleSlot(o.Id, o.AssignedUserId, o.ScheduledStart, o.ScheduledEnd)));
        ConflictCount = conflicts.Count;
        Items = orders.Select(o => ToView(o, conflicts.Contains(o.Id))).ToList();
    }

    private static ScheduleItemView ToView(WorkOrder o, bool conflict)
    {
        DateTime? start = o.ScheduledStart.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(o.ScheduledStart.Value, CommercialClock.BusinessTimeZone) : null;
        DateTime? end = o.ScheduledEnd.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(o.ScheduledEnd.Value, CommercialClock.BusinessTimeZone) : null;
        return new ScheduleItemView(
            o.Id, o.Number, o.Title, o.Status.ToString(),
            start?.ToString("HH:mm"), start?.ToString("ddd, dd/MM"), end?.ToString("HH:mm"),
            o.AssignedUserId.HasValue, conflict);
    }
}
