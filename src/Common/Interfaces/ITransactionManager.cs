using System.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Common.Interfaces;

public interface ITransactionManager
{
    // Entity Framework transactions
    Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default);

    // Raw SQL transactions (for Dapper)
    IDbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
    Task CommitTransactionAsync(IDbTransaction transaction);
    Task RollbackTransactionAsync(IDbTransaction transaction);

    // Execute with transaction
    Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
        Func<Task> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default);

    // Execute with raw SQL transaction
    Task<T> ExecuteInDbTransactionAsync<T>(
        Func<IDbTransaction, Task<T>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default);

    Task ExecuteInDbTransactionAsync(
        Func<IDbTransaction, Task> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default);

    // Current transaction info
    bool HasActiveTransaction { get; }
    IDbContextTransaction? CurrentTransaction { get; }
}