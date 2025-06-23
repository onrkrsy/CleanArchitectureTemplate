using System.Data;

namespace Common.Interfaces;

public interface IDbRepository
{
    // Query operations
    Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null);
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null);
    Task<T> QueryFirstAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null);
    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null);

    // Execute operations
    Task<int> ExecuteAsync(string sql, object? param = null, IDbTransaction? transaction = null);
    Task<T> ExecuteScalarAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null);

    // Multiple result sets
    Task<TReturn> QueryMultipleAsync<TReturn>(string sql, Func<dynamic, TReturn> map, object? param = null, IDbTransaction? transaction = null);

    // Paging support
    Task<(IEnumerable<T> Items, int TotalCount)> QueryPagedAsync<T>(
        string sql, 
        int pageNumber, 
        int pageSize, 
        object? param = null,
        string? countSql = null,
        IDbTransaction? transaction = null);

    // Transaction support
    IDbTransaction BeginTransaction();
    Task<IDbTransaction> BeginTransactionAsync();

    // Bulk operations
    Task BulkInsertAsync<T>(IEnumerable<T> entities, string tableName, IDbTransaction? transaction = null);
    Task BulkUpdateAsync<T>(IEnumerable<T> entities, string tableName, string[] keyColumns, IDbTransaction? transaction = null);
    Task BulkDeleteAsync(string tableName, string whereClause, object? param = null, IDbTransaction? transaction = null);
}