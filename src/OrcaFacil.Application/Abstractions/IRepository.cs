namespace OrcaFacil.Application.Abstractions;

public interface IRepository<T> where T : class
{
    Task<T?> GetAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Remove(T entity);
    IQueryable<T> Query();
}
