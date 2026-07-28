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

public class UnitOfWork : IUnitOfWork, IAsyncDisposable
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
        var transaction = _transaction;
        try
        {
            await transaction.CommitAsync(ct);
        }
        catch
        {
            _db.ChangeTracker.Clear();
            throw;
        }
        finally
        {
            await transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        var transaction = _transaction;
        _transaction = null;
        try { await transaction.RollbackAsync(ct); }
        finally
        {
            await transaction.DisposeAsync();
            _db.ChangeTracker.Clear();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is null) return;
        var transaction = _transaction;
        _transaction = null;
        try { await transaction.RollbackAsync(CancellationToken.None); }
        finally
        {
            await transaction.DisposeAsync();
            _db.ChangeTracker.Clear();
        }
    }
}
