using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Areas.Admin.Pages.EmailOutbox;

[Authorize(Policy = "SuperAdminOnly")]
public sealed class IndexModel(OrcaFacilDbContext db, IAuditService audit) : PageModel
{
    public IReadOnlyList<EmailOutboxMessage> Items { get; private set; } = [];
    public async Task OnGetAsync(EmailOutboxStatus? status, CancellationToken ct)
    {
        var query = db.EmailOutboxMessages.AsNoTracking();
        if (status.HasValue) query = query.Where(x => x.Status == status);
        Items = await query.OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync(ct);
    }

    public async Task<IActionResult> OnPostRetryAsync(Guid id, CancellationToken ct)
    {
        var message = await db.EmailOutboxMessages.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (message is null) return NotFound();
        if (message.Status is not (EmailOutboxStatus.Failed or EmailOutboxStatus.DeadLetter))
        { TempData["Error"] = "Somente mensagens falhas podem ser reprocessadas."; return RedirectToPage(); }
        var previous = message.Status;
        message.Status = EmailOutboxStatus.Pending;
        message.NextAttemptAt = DateTime.UtcNow;
        message.ProcessingInstanceId = null;
        message.ProcessingStartedAt = null;
        await audit.RegisterAsync(null, "EmailOutbox.Retry", nameof(EmailOutboxMessage), id.ToString(), new { Status = previous }, new { message.Status }, null, ct);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Mensagem devolvida à fila com segurança.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id, CancellationToken ct)
    {
        var message = await db.EmailOutboxMessages.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (message is null) return NotFound();
        if (message.Status is not (EmailOutboxStatus.Pending or EmailOutboxStatus.Failed))
        { TempData["Error"] = "Somente mensagens pendentes ou falhas podem ser canceladas."; return RedirectToPage(); }
        var previous = message.Status;
        message.Status = EmailOutboxStatus.Canceled;
        message.ProcessingInstanceId = null;
        message.ProcessingStartedAt = null;
        await audit.RegisterAsync(null, "EmailOutbox.Cancel", nameof(EmailOutboxMessage), id.ToString(), new { Status = previous }, new { message.Status }, null, ct, message.AccountId);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Mensagem cancelada; ela não será processada.";
        return RedirectToPage();
    }
}
