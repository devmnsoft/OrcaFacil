using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
    private IDbContextTransaction? _transaction;

    public UnitOfWork(OrcaFacilDbContext db) => _db = db;

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null) throw new InvalidOperationException("Já existe uma transação em andamento.");
        _transaction = await _db.Database.BeginTransactionAsync(ct);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null) throw new InvalidOperationException("Não existe uma transação em andamento.");
        await _transaction.CommitAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        await _transaction.RollbackAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
        _db.ChangeTracker.Clear();
    }
}
