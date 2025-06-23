using System.Data;
using Microsoft.Data.SqlClient;
using Common.Interfaces;
using Common.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "DefaultConnection",
        DatabaseProvider provider = DatabaseProvider.SqlServer)
        where TContext : DbContext
    {
        var connectionString = configuration.GetConnectionString(connectionStringName);

        services.AddDbContext<TContext>(options =>
        {
            switch (provider)
            {
                case DatabaseProvider.SqlServer:
                    options.UseSqlServer(connectionString);
                    break;
                case DatabaseProvider.PostgreSQL:
                    options.UseNpgsql(connectionString);
                    break;
                case DatabaseProvider.InMemory:
                    options.UseInMemoryDatabase(connectionStringName);
                    break;
                default:
                    throw new ArgumentException($"Unsupported database provider: {provider}");
            }
        });

        return services;
    }

    public static IServiceCollection AddDapperRepository(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "DefaultConnection",
        DatabaseProvider provider = DatabaseProvider.SqlServer)
    {
        var connectionString = configuration.GetConnectionString(connectionStringName);

        services.AddScoped<IDbConnection>(sp =>
        {
            return provider switch
            {
                DatabaseProvider.SqlServer => new SqlConnection(connectionString),
                DatabaseProvider.PostgreSQL => new NpgsqlConnection(connectionString),
                _ => throw new ArgumentException($"Dapper not supported for provider: {provider}")
            };
        });

        services.AddScoped<IDbRepository, DapperRepository>();
        
        return services;
    }

    public static IServiceCollection AddInMemoryDatabase<TContext>(
        this IServiceCollection services,
        string databaseName = "TestDatabase")
        where TContext : DbContext
    {
        services.AddDbContext<TContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        return services;
    }
}

public enum DatabaseProvider
{
    SqlServer,
    PostgreSQL,
    InMemory
}