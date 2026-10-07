namespace OrcaFacil.Application.Abstractions;

public interface IUnitOfWork
{
    /// <summary>Indica se já existe uma transação aberta neste escopo (permite comandos compostos participarem da transação ambiente).</summary>
    bool HasActiveTransaction { get; }
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
