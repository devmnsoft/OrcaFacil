using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;

namespace OrcaFacil.Persistence.Repositories;

public class EfRepository<T> : IRepository<T> where T : class
{
    private readonly OrcaFacilDbContext _db;

    public EfRepository(OrcaFacilDbContext db) => _db = db;

    public Task<T?> GetAsync(Guid id, CancellationToken ct = default) => _db.Set<T>().FindAsync([id], ct).AsTask();
    public Task AddAsync(T entity, CancellationToken ct = default) => _db.Set<T>().AddAsync(entity, ct).AsTask();
    public void Remove(T entity) => _db.Set<T>().Remove(entity);
    public IQueryable<T> Query() => _db.Set<T>().AsQueryable();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly OrcaFacilDbContext _db;

    public UnitOfWork(OrcaFacilDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
