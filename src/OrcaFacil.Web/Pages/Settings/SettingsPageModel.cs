using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Settings;

[Authorize]
public class SettingsPageModel(OrcaFacilDbContext db, ICurrentAccountService current, IAuditService audit, IWebHostEnvironment environment) : PageModel
{
    [BindProperty] public AccountSettings Input { get; set; } = new();
    [BindProperty] public IFormFile? MainLogo { get; set; }
    [BindProperty] public IFormFile? CompactLogo { get; set; }
    public BusinessAccount Account { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!current.AccountId.HasValue) return Forbid();
        Account = await db.BusinessAccounts.AsNoTracking().SingleAsync(x => x.Id == current.AccountId && !x.IsDeleted, ct);
        Input = await db.AccountSettings.AsNoTracking().SingleOrDefaultAsync(x => x.AccountId == current.AccountId && !x.IsDeleted, ct)
            ?? new AccountSettings { AccountId = current.AccountId.Value };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!current.AccountId.HasValue) return Forbid();
        if (!await CanManageAsync(ct)) return Forbid();
        if (!ModelState.IsValid) { await LoadAccountAsync(ct); return Page(); }
        var accountId = current.AccountId.Value;
        var stored = await db.AccountSettings.SingleOrDefaultAsync(x => x.AccountId == accountId && !x.IsDeleted, ct);
        var before = stored is null ? null : new { stored.UpdatedAt };
        if (stored is null)
        {
            stored = new AccountSettings { AccountId = accountId };
            db.AccountSettings.Add(stored);
        }
        var mainLogo = await SaveLogoAsync(MainLogo, accountId, "main", ct);
        var compactLogo = await SaveLogoAsync(CompactLogo, accountId, "compact", ct);
        if (!ModelState.IsValid) { await LoadAccountAsync(ct); return Page(); }
        CopyEditable(Input, stored);
        if (mainLogo is not null) stored.LogoPath = mainLogo;
        if (compactLogo is not null) stored.CompactLogoPath = compactLogo;
        await audit.RegisterAsync(current.UserId, "settings.updated", "AccountSettings", stored.Id.ToString(), before,
            new { Section = Request.Path.Value, stored.UpdatedAt }, null, ct, accountId);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Configurações salvas com segurança.";
        return Redirect(Request.Path);
    }

    private Task<bool> CanManageAsync(CancellationToken ct) => current.IsPlatformUser
        ? Task.FromResult(true)
        : current.HasPermissionAsync("account.edit", ct);

    private async Task LoadAccountAsync(CancellationToken ct) => Account = await db.BusinessAccounts.AsNoTracking()
        .SingleAsync(x => x.Id == current.AccountId && !x.IsDeleted, ct);

    private static void CopyEditable(AccountSettings source, AccountSettings target)
    {
        var accountId = target.AccountId;
        typeof(AccountSettings).GetProperties().Where(p => p.CanRead && p.CanWrite && p.Name is not (nameof(AccountSettings.AccountId) or nameof(AccountSettings.Id) or nameof(AccountSettings.CreatedAt) or nameof(AccountSettings.UpdatedAt) or nameof(AccountSettings.IsDeleted)))
            .ToList().ForEach(p => p.SetValue(target, p.GetValue(source)));
        target.AccountId = accountId;
        target.Touch();
    }

    private async Task<string?> SaveLogoAsync(IFormFile? file, Guid accountId, string variant, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return null;
        if (file.Length > 2 * 1024 * 1024) { ModelState.AddModelError(string.Empty, "Cada logo deve ter no máximo 2 MB."); return null; }
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg" or ".webp")) { ModelState.AddModelError(string.Empty, "Use uma imagem PNG, JPG ou WebP."); return null; }
        byte[] header = new byte[12];
        await using var input = file.OpenReadStream();
        var read = await input.ReadAsync(header.AsMemory(0, header.Length), ct);
        var valid = read >= 4 && ((header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4e && header[3] == 0x47) ||
            (header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff) ||
            (read >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50));
        if (!valid) { ModelState.AddModelError(string.Empty, "O conteúdo do arquivo não corresponde a uma imagem permitida."); return null; }
        input.Position = 0;
        var relative = $"uploads/branding/{accountId:N}/{variant}-{Guid.NewGuid():N}{extension}";
        var destination = Path.Combine(environment.WebRootPath, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        await using var output = System.IO.File.Create(destination);
        await input.CopyToAsync(output, ct);
        return "/" + relative;
    }
}
