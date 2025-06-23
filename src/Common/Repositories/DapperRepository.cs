using System.Data;
using Common.Interfaces;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Common.Repositories;

public class DapperRepository : IDbRepository
{
    private readonly IDbConnection _connection;
    private readonly ILogger<DapperRepository> _logger;

    public DapperRepository(IDbConnection connection, ILogger<DapperRepository> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null)
    {
        try
        {
            _logger.LogDebug("Executing SQL: {Sql}", sql);
            return await _connection.QuerySingleOrDefaultAsync<T>(sql, param, transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing QuerySingleOrDefaultAsync. SQL: {Sql}", sql);
            throw;
        }
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null)
    {
        try
        {
            _logger.LogDebug("Executing SQL: {Sql}", sql);
            return await _connection.QueryAsync<T>(sql, param, transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing QueryAsync. SQL: {Sql}", sql);
            throw;
        }
    }

    public async Task<T> QueryFirstAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null)
    {
        try
        {
            _logger.LogDebug("Executing SQL: {Sql}", sql);
            return await _connection.QueryFirstAsync<T>(sql, param, transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing QueryFirstAsync. SQL: {Sql}", sql);
            throw;
        }
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null)
    {
        try
        {
            _logger.LogDebug("Executing SQL: {Sql}", sql);
            return await _connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing QueryFirstOrDefaultAsync. SQL: {Sql}", sql);
            throw;
        }
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null, IDbTransaction? transaction = null)
    {
        try
        {
            _logger.LogDebug("Executing SQL: {Sql}", sql);
            return await _connection.ExecuteAsync(sql, param, transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing ExecuteAsync. SQL: {Sql}", sql);
            throw;
        }
    }

    public async Task<T> ExecuteScalarAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null)
    {
        try
        {
            _logger.LogDebug("Executing SQL: {Sql}", sql);
            return await _connection.ExecuteScalarAsync<T>(sql, param, transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing ExecuteScalarAsync. SQL: {Sql}", sql);
            throw;
        }
    }

    public async Task<TReturn> QueryMultipleAsync<TReturn>(string sql, Func<dynamic, TReturn> map, object? param = null, IDbTransaction? transaction = null)
    {
        try
        {
            _logger.LogDebug("Executing SQL: {Sql}", sql);
            using var multi = await _connection.QueryMultipleAsync(sql, param, transaction);
            return map(multi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing QueryMultipleAsync. SQL: {Sql}", sql);
            throw;
        }
    }

    public async Task<(IEnumerable<T> Items, int TotalCount)> QueryPagedAsync<T>(
        string sql, 
        int pageNumber, 
        int pageSize, 
        object? param = null,
        string? countSql = null,
        IDbTransaction? transaction = null)
    {
        try
        {
            _logger.LogDebug("Executing paged query. SQL: {Sql}, Page: {Page}, Size: {Size}", sql, pageNumber, pageSize);

            var offset = (pageNumber - 1) * pageSize;
            var pagedSql = $"{sql} OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            
            countSql ??= $"SELECT COUNT(*) FROM ({sql}) AS CountQuery";

            var items = await _connection.QueryAsync<T>(pagedSql, param, transaction);
            var totalCount = await _connection.QuerySingleAsync<int>(countSql, param, transaction);

            return (items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing QueryPagedAsync. SQL: {Sql}", sql);
            throw;
        }
    }

    public IDbTransaction BeginTransaction()
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();
        return _connection.BeginTransaction();
    }

    public async Task<IDbTransaction> BeginTransactionAsync()
    {
        if (_connection.State != ConnectionState.Open)
            await Task.Run(() => _connection.Open());
        return BeginTransaction();
    }

    public async Task BulkInsertAsync<T>(IEnumerable<T> entities, string tableName, IDbTransaction? transaction = null)
    {
        try
        {
            _logger.LogDebug("Executing bulk insert to table: {TableName}", tableName);
            
            var entityList = entities.ToList();
            if (!entityList.Any()) return;

            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead && p.Name != "Id") // Assuming Id is auto-generated
                .ToArray();

            var columnNames = string.Join(", ", properties.Select(p => p.Name));
            var valueNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));
            
            var sql = $"INSERT INTO {tableName} ({columnNames}) VALUES ({valueNames})";
            
            await _connection.ExecuteAsync(sql, entityList, transaction);
            
            _logger.LogInformation("Bulk inserted {Count} records to {TableName}", entityList.Count, tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing BulkInsertAsync to table: {TableName}", tableName);
            throw;
        }
    }

    public async Task BulkUpdateAsync<T>(IEnumerable<T> entities, string tableName, string[] keyColumns, IDbTransaction? transaction = null)
    {
        try
        {
            _logger.LogDebug("Executing bulk update to table: {TableName}", tableName);
            
            var entityList = entities.ToList();
            if (!entityList.Any()) return;

            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead && !keyColumns.Contains(p.Name))
                .ToArray();

            var setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));
            var whereClause = string.Join(" AND ", keyColumns.Select(k => $"{k} = @{k}"));
            
            var sql = $"UPDATE {tableName} SET {setClause} WHERE {whereClause}";
            
            await _connection.ExecuteAsync(sql, entityList, transaction);
            
            _logger.LogInformation("Bulk updated {Count} records in {TableName}", entityList.Count, tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing BulkUpdateAsync to table: {TableName}", tableName);
            throw;
        }
    }

    public async Task BulkDeleteAsync(string tableName, string whereClause, object? param = null, IDbTransaction? transaction = null)
    {
        try
        {
            _logger.LogDebug("Executing bulk delete from table: {TableName}", tableName);
            
            var sql = $"DELETE FROM {tableName} WHERE {whereClause}";
            var affectedRows = await _connection.ExecuteAsync(sql, param, transaction);
            
            _logger.LogInformation("Bulk deleted {Count} records from {TableName}", affectedRows, tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing BulkDeleteAsync from table: {TableName}", tableName);
            throw;
        }
    }
}