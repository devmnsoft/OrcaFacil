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

    public async Task OnGetAsync(Guid? id, Guid? clientId, Guid? serviceId, Guid[]? serviceIds, Guid? templateId, CancellationToken ct)
    {
        Draft = await _wizard.OpenAsync(_current.UserId, _account.AccountId, id, clientId, ct, serviceIds is { Length: > 0 } ? serviceIds : serviceId is Guid one ? [one] : [], templateId);
        await LoadClients(ct);
    }

    public async Task<IActionResult> OnPostAutosaveAsync([FromBody] SaveBudgetDraftRequest input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.IdempotencyKey)) return BadRequest(new { error = "Identificador de salvamento ausente." });
        var result = await _wizard.SaveAsync(_current.UserId, _account.AccountId, input, ct);
        return result.Succeeded ? new JsonResult(result.Draft) : StatusCode(result.Conflict ? 409 : 400, new { error = result.Error, draft = result.Draft });
    }

    public async Task<IActionResult> OnPostFinalizeAsync([FromBody] SaveBudgetDraftRequest input, CancellationToken ct)
    {
        var result = await _wizard.FinalizeAsync(_current.UserId, _account.AccountId, input, ct);
        return result.Succeeded ? new JsonResult(new { redirectUrl = Url.Page("/Documents/Details", new { id = input.DocumentId }) })
            : StatusCode(result.Conflict ? 409 : 400, new { error = result.Error, draft = result.Draft });
    }

    private async Task LoadClients(CancellationToken ct) => Clients = await _db.Clients.AsNoTracking()
        .Where(x => x.UserId == _current.UserId && x.AccountId == _account.AccountId && !x.IsDeleted).OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).Take(40)
        .Select(x => new ClientChoice(x.Id, x.Name, x.DocumentNumber, x.Phone, x.Email, x.City)).ToListAsync(ct);
}

public sealed record ClientChoice(Guid Id, string Name, string? Document, string? Phone, string? Email, string? City);
