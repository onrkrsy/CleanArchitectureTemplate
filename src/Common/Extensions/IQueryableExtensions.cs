using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Common.Extensions;

public static class IQueryableExtensions
{
    public static async Task<(IEnumerable<T> Items, int TotalCount)> ToPaginatedListAsync<T>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> source,
        bool condition,
        Expression<Func<T, bool>> predicate)
    {
        return condition ? source.Where(predicate) : source;
    }

    public static IQueryable<T> OrderByIf<T, TKey>(
        this IQueryable<T> source,
        bool condition,
        Expression<Func<T, TKey>> keySelector)
    {
        return condition ? source.OrderBy(keySelector) : source;
    }

    public static IQueryable<T> OrderByDescendingIf<T, TKey>(
        this IQueryable<T> source,
        bool condition,
        Expression<Func<T, TKey>> keySelector)
    {
        return condition ? source.OrderByDescending(keySelector) : source;
    }
}