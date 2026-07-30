using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.DTOs;
using OrcaFacil.Application.Profile;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Pages.Dashboard;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ICurrentUserService _current;
    private readonly IDashboardQueries _queries;
    private readonly ProfileService _profiles;
    private readonly INextBestActionService _nextBestAction;
    public NextBestAction? NextAction { get; private set; }
    public string FirstName => string.IsNullOrWhiteSpace(_current.Name) ? "bem-vindo" : _current.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

    public DashboardDto? Dashboard { get; private set; }
    public bool HasIssuerProfile { get; private set; }

    public IndexModel(ICurrentUserService current, IDashboardQueries queries, ProfileService profiles, INextBestActionService nextBestAction)
    {
        _current = current;
        _queries = queries;
        _profiles = profiles;
        _nextBestAction = nextBestAction;
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Dashboard = await _queries.GetDashboardAsync(_current.UserId, ct);
        HasIssuerProfile = await _profiles.GetAsync(new(_current.UserId), ct) is not null;
        NextAction = await _nextBestAction.GetAsync(ct);
    }
}
