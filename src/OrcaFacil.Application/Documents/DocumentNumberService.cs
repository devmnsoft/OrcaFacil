using Microsoft.Extensions.Logging;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Documents;

public class DocumentNumberService : IDocumentNumberService
{
    private readonly IRepository<Document> _documents;
    private readonly ILogger<DocumentNumberService> _logger;

    public DocumentNumberService(IRepository<Document> documents, ILogger<DocumentNumberService> logger)
    {
        _documents = documents;
        _logger = logger;
    }

    public Task<string> NextAsync(Guid userId, DocumentType type, CancellationToken ct = default)
    {
        var prefix = type == DocumentType.Receipt ? "REC" : "ORC";
        var existing = _documents.Query()
            .Where(document => document.UserId == userId && document.Type == type)
            .Select(document => document.Number)
            .ToList();
        var sequence = existing
            .Select(number => int.TryParse(number.Replace(prefix + "-", string.Empty), out var value) ? value : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;
        var next = $"{prefix}-{sequence:000000}";
        _logger.LogInformation("DOCUMENT_NUMBER_GENERATED {UserId} {DocumentType} {Number}", userId, type, next);
        return Task.FromResult(next);
    }
}
