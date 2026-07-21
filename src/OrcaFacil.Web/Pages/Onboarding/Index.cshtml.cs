using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Profile;

namespace OrcaFacil.Web.Pages.Onboarding;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ICurrentUserService _current;
    private readonly ProfileService _profiles;
    private readonly IDocumentQueries _documents;

    public bool HasIssuerProfile { get; private set; }
    public bool HasDocuments { get; private set; }
    public int ActiveStep => !HasIssuerProfile ? 1 : !HasDocuments ? 2 : 3;

    public IndexModel(ICurrentUserService current, ProfileService profiles, IDocumentQueries documents)
    {
        _current = current;
        _profiles = profiles;
        _documents = documents;
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        HasIssuerProfile = await _profiles.GetAsync(new(_current.UserId), ct) is not null;
        HasDocuments = (await _documents.ListDocumentsAsync(_current.UserId, ct)).Count > 0;
    }
}
