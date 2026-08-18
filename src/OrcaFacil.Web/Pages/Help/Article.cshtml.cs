using Microsoft.AspNetCore.Mvc; using Microsoft.AspNetCore.Mvc.RazorPages; using Microsoft.EntityFrameworkCore; using OrcaFacil.Domain.Entities; using OrcaFacil.Persistence;
namespace OrcaFacil.Web.Pages.Help;
public sealed class ArticleModel(OrcaFacilDbContext db):PageModel { public KnowledgeBaseArticle Article{get;private set;}=null!; public async Task<IActionResult> OnGetAsync(string slug,CancellationToken ct){Article=(await db.KnowledgeBaseArticles.AsNoTracking().SingleOrDefaultAsync(x=>x.Slug==slug&&x.IsPublished&&!x.IsDeleted,ct))!;return Article is null?NotFound():Page();} }
