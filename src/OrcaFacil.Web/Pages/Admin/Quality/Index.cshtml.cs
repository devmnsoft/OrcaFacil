using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Quality;
using OrcaFacil.Application.Security;

namespace OrcaFacil.Web.Pages.Admin.Quality;

[Authorize(Policy = "Permission:" + PermissionCodes.QualityView)]
public sealed class IndexModel(FunctionalQualityService quality, ICurrentAccountService account) : PageModel
{
    public FunctionalQualitySnapshot Snapshot { get; private set; } = new([], [], DateTimeOffset.MinValue);

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var permission = HttpContext.Request.Path.Value switch
        {
            var path when path is not null && path.EndsWith("/SourceAudit", StringComparison.OrdinalIgnoreCase) => PermissionCodes.QualitySourceAuditView,
            var path when path is not null && path.EndsWith("/BusinessRules", StringComparison.OrdinalIgnoreCase) => PermissionCodes.QualityBusinessRulesView,
            var path when path is not null && path.EndsWith("/Readiness", StringComparison.OrdinalIgnoreCase) => PermissionCodes.QualityReadinessView,
            _ => PermissionCodes.QualityView,
        };
        if (!await account.HasPermissionAsync(permission, cancellationToken)) return Forbid();
        Snapshot = quality.Evaluate(DateTimeOffset.UtcNow);
        return Page();
    }
}
