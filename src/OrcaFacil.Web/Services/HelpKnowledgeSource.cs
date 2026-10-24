using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Ai;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

/// <summary>
/// Fonte dos artigos publicados que podem fundamentar a ajuda inteligente.
/// Somente artigos publicados e não excluídos entram no corpus.
/// </summary>
public interface IHelpKnowledgeSource
{
    Task<IReadOnlyList<AiSource>> GetPublishedAsync(Guid accountId, CancellationToken ct = default);
}

public sealed class EfHelpKnowledgeSource(OrcaFacilDbContext db) : IHelpKnowledgeSource
{
    public async Task<IReadOnlyList<AiSource>> GetPublishedAsync(Guid accountId, CancellationToken ct = default)
    {
        var articles = await db.KnowledgeBaseArticles.AsNoTracking()
            .Where(x => x.IsPublished && !x.IsDeleted)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Title)
            .Take(50)
            .Select(x => new { x.Id, x.Title, x.Slug, x.Summary, x.Content })
            .ToListAsync(ct);

        return articles.Select(a => new AiSource(accountId, "article", a.Id.ToString(), a.Title,
            $"/Help/Article?slug={a.Slug}", $"{a.Summary}\n{a.Content}")).ToArray();
    }
}
