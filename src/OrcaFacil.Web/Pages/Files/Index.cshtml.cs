using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Files;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Files;
[Authorize]
public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService current, IFileStorageService storage) : PageModel
{
 [BindProperty(SupportsGet=true)] public string? Search { get; set; }
 [BindProperty(SupportsGet=true)] public FileAssetCategory? Category { get; set; }
 [BindProperty] public IFormFile? Upload { get; set; }
 [BindProperty] public FileAssetCategory UploadCategory { get; set; } = FileAssetCategory.General;
 public IReadOnlyList<FileAsset> Items { get; private set; }=[];
 public async Task<IActionResult> OnGetAsync(CancellationToken ct) { if(!current.AccountId.HasValue || !await current.HasPermissionAsync("Files.View",ct)) return Forbid(); await Load(ct); return Page(); }
 public async Task<IActionResult> OnPostUploadAsync(CancellationToken ct)
 {
  if(!current.AccountId.HasValue || !await current.HasPermissionAsync("Files.Upload",ct)) return Forbid();
  if(Upload is null){ModelState.AddModelError(nameof(Upload),"Selecione um arquivo.");await Load(ct);return Page();}
  try { var ext=FileUploadPolicy.ValidateAndGetExtension(Upload.FileName,Upload.Length); await using var input=Upload.OpenReadStream(); var saved=await storage.SaveAsync(current.AccountId.Value,Upload.FileName,input,ct); var asset=new FileAsset { AccountId=current.AccountId.Value,UploadedByUserId=current.UserId,OriginalFileName=Path.GetFileName(Upload.FileName),StoredFileName=saved.StoredFileName,StoragePath=saved.RelativePath,ContentType=ContentTypes[ext],Extension=ext,SizeInBytes=saved.SizeInBytes,Sha256Hash=saved.Sha256Hash,Category=UploadCategory,Visibility=FileAssetVisibility.Private }; db.FileAssets.Add(asset); db.DocumentAuditEvents.Add(new(){AccountId=asset.AccountId,UserId=current.UserId,EventType="file.uploaded",EntityType=nameof(FileAsset),EntityId=asset.Id}); await db.SaveChangesAsync(ct); TempData["Success"]="Arquivo armazenado com segurança."; return RedirectToPage(); }
  catch(ArgumentException ex){ModelState.AddModelError(nameof(Upload),ex.Message);await Load(ct);return Page();}
 }
 public async Task<IActionResult> OnGetDownloadAsync(Guid id,CancellationToken ct) { if(!current.AccountId.HasValue||!await current.HasPermissionAsync("Files.Download",ct))return Forbid();var asset=await db.FileAssets.SingleOrDefaultAsync(x=>x.Id==id&&x.AccountId==current.AccountId&&!x.IsDeleted,ct);if(asset is null)return NotFound();var stream=await storage.OpenReadAsync(asset.StoragePath,ct);db.DocumentAuditEvents.Add(new(){AccountId=asset.AccountId,UserId=current.UserId,EventType="file.downloaded",EntityType=nameof(FileAsset),EntityId=asset.Id});await db.SaveChangesAsync(ct);return File(stream,asset.ContentType,asset.OriginalFileName); }
 public async Task<IActionResult> OnPostDeleteAsync(Guid id,CancellationToken ct){if(!current.AccountId.HasValue||!await current.HasPermissionAsync("Files.Delete",ct))return Forbid();var asset=await db.FileAssets.SingleOrDefaultAsync(x=>x.Id==id&&x.AccountId==current.AccountId&&!x.IsDeleted,ct);if(asset is null)return NotFound();asset.MarkAsDeleted();db.DocumentAuditEvents.Add(new(){AccountId=asset.AccountId,UserId=current.UserId,EventType="file.removed",EntityType=nameof(FileAsset),EntityId=asset.Id});await db.SaveChangesAsync(ct);return RedirectToPage();}
 private async Task Load(CancellationToken ct){var id=current.AccountId!.Value;var q=db.FileAssets.AsNoTracking().Where(x=>x.AccountId==id&&!x.IsDeleted);if(!string.IsNullOrWhiteSpace(Search))q=q.Where(x=>EF.Functions.ILike(x.OriginalFileName,$"%{Search.Trim()}%"));if(Category.HasValue)q=q.Where(x=>x.Category==Category);Items=await q.OrderByDescending(x=>x.CreatedAt).Take(200).ToListAsync(ct);}
 private static readonly IReadOnlyDictionary<string,string> ContentTypes=new Dictionary<string,string>{{".pdf","application/pdf"},{".png","image/png"},{".jpg","image/jpeg"},{".jpeg","image/jpeg"},{".webp","image/webp"},{".csv","text/csv"},{".txt","text/plain"}};
}
