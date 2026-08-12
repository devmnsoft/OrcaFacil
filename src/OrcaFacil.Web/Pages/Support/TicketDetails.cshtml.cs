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
public sealed class TicketDetailsModel(OrcaFacilDbContext db, ICurrentAccountService current) : PageModel
{
    public SupportTicket Ticket { get; private set; } = null!; public IReadOnlyList<SupportTicketMessage> Messages { get; private set; } = [];
    [BindProperty, Required, StringLength(5000, MinimumLength = 2)] public string Reply { get; set; } = string.Empty;
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct) => await Load(id, ct) ? Page() : NotFound();
    public async Task<IActionResult> OnPostReplyAsync(Guid id, CancellationToken ct) { if (!await Load(id, ct)) return NotFound(); if (Ticket.Status == SupportTicketStatus.Closed) ModelState.AddModelError("", "Este chamado está encerrado."); if (!ModelState.IsValid) return Page(); db.SupportTicketMessages.Add(new() { TicketId=id, AuthorUserId=current.UserId, Body=Reply.Trim() }); Ticket.Status=SupportTicketStatus.Open; Ticket.Touch(); await db.SaveChangesAsync(ct); TempData.Success("Resposta enviada."); return RedirectToPage(new{id}); }
    public async Task<IActionResult> OnPostCloseAsync(Guid id, CancellationToken ct) { if (!await Load(id, ct)) return NotFound(); Ticket.Status=SupportTicketStatus.Closed; Ticket.ClosedAt=DateTime.UtcNow; Ticket.Touch(); await db.SaveChangesAsync(ct); TempData.Success("Chamado encerrado."); return RedirectToPage(new{id}); }
    private async Task<bool> Load(Guid id,CancellationToken ct) { Ticket=(await db.SupportTickets.SingleOrDefaultAsync(x=>x.Id==id&&x.AccountId==current.AccountId&&!x.IsDeleted,ct))!; if(Ticket is null)return false; Messages=await db.SupportTicketMessages.AsNoTracking().Where(x=>x.TicketId==id&&!x.IsDeleted&&!x.IsInternal).OrderBy(x=>x.CreatedAt).ToListAsync(ct); return true; }
}
