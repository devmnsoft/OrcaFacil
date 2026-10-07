using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Documents;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Documents;

[Authorize]
public sealed class CreateBudgetModel : PageModel
{
    private readonly ICurrentUserService _current;
    private readonly ICurrentAccountService _account;
    private readonly BudgetWizardService _wizard;
    private readonly OrcaFacilDbContext _db;
    public CreateBudgetModel(ICurrentUserService current, ICurrentAccountService account, BudgetWizardService wizard, OrcaFacilDbContext db)
    { _current = current; _account = account; _wizard = wizard; _db = db; }

    public BudgetWizardViewModel Draft { get; private set; } = default!;
    public IReadOnlyList<ClientChoice> Clients { get; private set; } = [];

    public string? OpenError { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid? id, Guid? clientId, Guid? serviceId, Guid[]? serviceIds, Guid? templateId, CancellationToken ct)
    {
        if (_account.AccountId is null) return Forbid();
        try { await _account.EnsureAccountAccessAsync(ct); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        var opened = await _wizard.OpenAsync(_current.UserId, _account.AccountId, id, clientId, ct, serviceIds is { Length: > 0 } ? serviceIds : serviceId is Guid one ? [one] : [], templateId);
        if (!opened.Succeeded || opened.Draft is null)
        {
            OpenError = opened.Error ?? "Não foi possível abrir o orçamento.";
            return Page();
        }
        if (!id.HasValue) return RedirectToPage(new { id = opened.Draft.DocumentId });
        Draft = opened.Draft;
        await LoadClients(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAutosaveAsync([FromBody] SaveBudgetDraftRequest input, CancellationToken ct)
    {
        if (_account.AccountId is null) return Forbid();
        try { await _account.EnsureAccountAccessAsync(ct); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        if (string.IsNullOrWhiteSpace(input.IdempotencyKey)) return BadRequest(new { error = "Identificador de salvamento ausente." });
        var result = await _wizard.SaveAsync(_current.UserId, _account.AccountId, input, ct);
        return result.Succeeded
            ? new JsonResult(new { draft = result.Draft, notices = result.Notices ?? [], rowVersion = result.Draft?.RowVersion })
            : StatusCode(result.Conflict ? 409 : 400, new { error = result.Error, draft = result.Draft, conflict = result.Conflict });
    }

    public async Task<IActionResult> OnPostFinalizeAsync([FromBody] SaveBudgetDraftRequest input, CancellationToken ct)
    {
        if (_account.AccountId is null) return Forbid();
        try { await _account.EnsureAccountAccessAsync(ct); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        var result = await _wizard.FinalizeAsync(_current.UserId, _account.AccountId, input, ct);
        return result.Succeeded ? new JsonResult(new { redirectUrl = Url.Page("/Documents/Details", new { id = input.DocumentId }) })
            : StatusCode(result.Conflict ? 409 : 400, new { error = result.Error, draft = result.Draft });
    }

    private async Task LoadClients(CancellationToken ct) => Clients = await _db.Clients.AsNoTracking()
        .Where(x => x.AccountId == _account.AccountId && !x.IsDeleted).OrderBy(x => x.Name).Take(40)
        .Select(x => new ClientChoice(x.Id, x.Name, x.DocumentNumber, x.Phone, x.Email, x.City)).ToListAsync(ct);
}

public sealed record ClientChoice(Guid Id, string Name, string? Document, string? Phone, string? Email, string? City);
