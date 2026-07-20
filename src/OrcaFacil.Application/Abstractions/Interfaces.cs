using OrcaFacil.Domain.Entities; using OrcaFacil.Domain.Enums; using OrcaFacil.Shared;
namespace OrcaFacil.Application.Abstractions;
public interface IUnitOfWork { Task<int> SaveChangesAsync(CancellationToken ct=default); }
public interface IRepository<T> where T:class { Task<T?> GetAsync(Guid id,CancellationToken ct=default); Task AddAsync(T entity,CancellationToken ct=default); void Remove(T entity); IQueryable<T> Query(); }
public interface IDocumentQueries { Task<IReadOnlyList<DocumentSummaryDto>> ListDocumentsAsync(Guid userId,CancellationToken ct=default); }
public interface IPdfService { Task<byte[]> GenerateDocumentPdfAsync(Document document, IssuerProfile? issuer, PlanType plan, CancellationToken ct=default); }
public interface IPasswordHasher { string Hash(string password); bool Verify(string password,string hash); }
public interface IClock { DateTime UtcNow { get; } }
public record DocumentItemDto(string Description, decimal Quantity, decimal UnitPrice, decimal Discount);
public record DocumentSummaryDto(Guid Id,string Type,string Number,string Status,string ClientName,decimal Total,DateTime CreatedAt);
public record PublicQuoteDto(string Number,string ClientName,decimal Total,IReadOnlyList<DocumentItemDto> Items,string? Notes);
