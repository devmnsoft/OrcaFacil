using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Extensions;

namespace OrcaFacil.Web.Pages.Support;

[Authorize]
public sealed class TicketsModel(OrcaFacilDbContext db, ICurrentAccountService current) : PageModel
{
    public IReadOnlyList<SupportTicket> Tickets { get; private set; } = [];
    [BindProperty, Required, StringLength(180)] public string Subject { get; set; } = string.Empty;
    [BindProperty, Required, StringLength(5000, MinimumLength = 10)] public string Description { get; set; } = string.Empty;
    [BindProperty] public SupportTicketCategory Category { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct) { if (current.AccountId is null) return Forbid(); await Load(ct); return Page(); }
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (current.AccountId is not Guid accountId) return Forbid();
        if (!ModelState.IsValid) { await Load(ct); return Page(); }
        var ticket = new SupportTicket { AccountId = accountId, OpenedByUserId = current.UserId, Protocol = $"SUP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..21].ToUpperInvariant(), Category = Category, Subject = Subject.Trim(), Description = Description.Trim() };
        db.SupportTickets.Add(ticket);
        db.SupportTicketMessages.Add(new SupportTicketMessage { TicketId = ticket.Id, AuthorUserId = current.UserId, Body = ticket.Description });
        var admins = await db.Users.Where(x => x.Role == UserRole.SuperAdmin && x.IsActive && !x.IsDeleted).Select(x => x.Id).ToListAsync(ct);
        foreach (var admin in admins) db.Notifications.Add(new Notification { UserId = admin, Title = "Novo chamado de suporte", Message = $"{ticket.Protocol}: {ticket.Subject}", Category = NotificationCategory.Support, ActionUrl = $"/Admin/Support/Details?id={ticket.Id}", ActionText = "Abrir chamado" });
        await db.SaveChangesAsync(ct); TempData.Success("Chamado aberto. A equipe de suporte foi notificada."); return RedirectToPage("TicketDetails", new { id = ticket.Id });
    }
    private async Task Load(CancellationToken ct) => Tickets = await db.SupportTickets.AsNoTracking().Where(x => x.AccountId == current.AccountId && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
}
