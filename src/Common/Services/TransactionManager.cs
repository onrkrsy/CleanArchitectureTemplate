using System.Data;
using Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Common.Services;

public class TransactionManager : ITransactionManager
{
    private readonly DbContext _context;
    private readonly IDbConnection? _dbConnection;
    private readonly ILogger<TransactionManager> _logger;
    private IDbContextTransaction? _currentTransaction;

    public TransactionManager(DbContext context, ILogger<TransactionManager> logger, IDbConnection? dbConnection = null)
    {
        _context = context;
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public bool HasActiveTransaction => _currentTransaction != null;
    public IDbContextTransaction? CurrentTransaction => _currentTransaction;

    public async Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, 
        CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            _logger.LogWarning("A transaction is already active. Returning current transaction.");
            return _currentTransaction;
        }

        _logger.LogDebug("Beginning EF transaction with isolation level: {IsolationLevel}", isolationLevel);
        _currentTransaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
        return _currentTransaction;
    }

    public async Task CommitTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Committing EF transaction");
            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Transaction committed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing transaction");
            throw;
        }
        finally
        {
            if (_currentTransaction == transaction)
            {
                _currentTransaction = null;
            }
            transaction.Dispose();
        }
    }

    public async Task RollbackTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Rolling back EF transaction");
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogInformation("Transaction rolled back successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back transaction");
            throw;
        }
        finally
        {
            if (_currentTransaction == transaction)
            {
                _currentTransaction = null;
            }
            transaction.Dispose();
        }
    }

    public IDbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        if (_dbConnection == null)
        {
            throw new InvalidOperationException("DbConnection is not configured for raw SQL transactions");
        }

        _logger.LogDebug("Beginning raw SQL transaction with isolation level: {IsolationLevel}", isolationLevel);
        
        if (_dbConnection.State != ConnectionState.Open)
        {
            _dbConnection.Open();
        }

        return _dbConnection.BeginTransaction(isolationLevel);
    }

    public async Task CommitTransactionAsync(IDbTransaction transaction)
    {
        try
        {
            _logger.LogDebug("Committing raw SQL transaction");
            transaction.Commit();
            _logger.LogInformation("Raw SQL transaction committed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing raw SQL transaction");
            throw;
        }
        finally
        {
            transaction.Dispose();
        }
        
        await Task.CompletedTask;
    }

    public async Task RollbackTransactionAsync(IDbTransaction transaction)
    {
        try
        {
            _logger.LogDebug("Rolling back raw SQL transaction");
            transaction.Rollback();
            _logger.LogInformation("Raw SQL transaction rolled back successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back raw SQL transaction");
            throw;
        }
        finally
        {
            transaction.Dispose();
        }
        
        await Task.CompletedTask;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var transaction = await BeginTransactionAsync(isolationLevel, cts.Token);
        
        try
        {
            _logger.LogDebug("Executing operation in transaction");
            var result = await operation();
            await CommitTransactionAsync(transaction, cts.Token);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in transactional operation, rolling back");
            await RollbackTransactionAsync(transaction, cts.Token);
            throw;
        }
    }

    public async Task ExecuteInTransactionAsync(
        Func<Task> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async () =>
        {
            await operation();
            return true;
        }, isolationLevel, timeoutSeconds, cancellationToken);
    }

    public async Task<T> ExecuteInDbTransactionAsync<T>(
        Func<IDbTransaction, Task<T>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var transaction = BeginTransaction(isolationLevel);
        
        try
        {
            _logger.LogDebug("Executing operation in raw SQL transaction");
            var result = await operation(transaction);
            await CommitTransactionAsync(transaction);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in raw SQL transactional operation, rolling back");
            await RollbackTransactionAsync(transaction);
            throw;
        }
    }

    public async Task ExecuteInDbTransactionAsync(
        Func<IDbTransaction, Task> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        await ExecuteInDbTransactionAsync(async (transaction) =>
        {
            await operation(transaction);
            return true;
        }, isolationLevel, timeoutSeconds, cancellationToken);
    }
}