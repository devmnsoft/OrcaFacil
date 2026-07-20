using Microsoft.EntityFrameworkCore; using OrcaFacil.Application.Abstractions;
namespace OrcaFacil.Persistence.Repositories;
public class EfRepository<T>(OrcaFacilDbContext db):IRepository<T> where T:class { public Task<T?> GetAsync(Guid id,CancellationToken ct=default)=>db.Set<T>().FindAsync([id],ct).AsTask(); public Task AddAsync(T entity,CancellationToken ct=default)=>db.Set<T>().AddAsync(entity,ct).AsTask(); public void Remove(T entity)=>db.Set<T>().Remove(entity); public IQueryable<T> Query()=>db.Set<T>(); }
public class UnitOfWork(OrcaFacilDbContext db):IUnitOfWork { public Task<int> SaveChangesAsync(CancellationToken ct=default)=>db.SaveChangesAsync(ct); }
