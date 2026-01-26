using System.Linq.Expressions;
using System.Reflection;
using AutoPartShop.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsShop.Infrastructure.Extensions;

public static class QueryableExtensions
{
    #region Sorting Extensions

    /// <summary>
    /// Dynamically applies multiple OrderBy/ThenBy clauses to an IQueryable.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="source">The IQueryable source</param>
    /// <param name="sorts">Array of tuples: (PropertyName, Ascending)</param>
    public static IQueryable<T> OrderByMultiple<T>(
        this IQueryable<T> source,
        params (string PropertyName, bool Ascending)[] sorts)
    {
        if (sorts == null || sorts.Length == 0)
            return source;

        bool firstSort = true;

        foreach (var (propertyName, ascending) in sorts)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                continue;

            // Get property info
            var property = typeof(T).GetProperty(propertyName,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (property == null)
                throw new ArgumentException($"Property '{propertyName}' not found on type '{typeof(T).Name}'.");

            // Build expression: x => x.PropertyName
            var parameter = Expression.Parameter(typeof(T), "x");
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExpression = Expression.Lambda(propertyAccess, parameter);

            string methodName;
            if (firstSort)
            {
                methodName = ascending ? "OrderBy" : "OrderByDescending";
                firstSort = false;
            }
            else
            {
                methodName = ascending ? "ThenBy" : "ThenByDescending";
            }

            // Call OrderBy / ThenBy dynamically
            source = typeof(Queryable).GetMethods()
                .First(m => m.Name == methodName
                            && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), property.PropertyType)
                .Invoke(null, new object[] { source, orderByExpression }) as IQueryable<T>
                ?? source;
        }

        return source;
    }

    /// <summary>
    /// Apply sorting from SortOption collection
    /// </summary>
    public static IQueryable<T> ApplySorting<T>(
        this IQueryable<T> source,
        IReadOnlyList<SortOption>? sorts)
    {
        if (sorts == null || sorts.Count == 0)
            return source;

        var sortTuples = sorts
            .Select(s => (s.Field, s.IsAscending))
            .ToArray();

        return source.OrderByMultiple(sortTuples);
    }

    /// <summary>
    /// Apply sorting from SortOption collection with a default sort
    /// </summary>
    public static IQueryable<T> ApplySorting<T>(
        this IQueryable<T> source,
        IReadOnlyList<SortOption>? sorts,
        Expression<Func<T, object>> defaultSort,
        bool defaultAscending = true)
    {
        if (sorts != null && sorts.Count > 0)
        {
            return source.ApplySorting(sorts);
        }

        return defaultAscending
            ? source.OrderBy(defaultSort)
            : source.OrderByDescending(defaultSort);
    }

    #endregion

    #region Pagination Extensions

    /// <summary>
    /// Apply pagination to query
    /// </summary>
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> source, int pageNumber, int pageSize)
    {
        return source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
    }

    /// <summary>
    /// Apply pagination from BaseQuery
    /// </summary>
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> source, BaseQuery query)
    {
        return source.ApplyPaging(query.PageNumber, query.PageSize);
    }

    /// <summary>
    /// Execute paginated query and return PagedResult
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .ApplyPaging(pageNumber, pageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<T>.Create(items, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Execute paginated query from BaseQuery and return PagedResult
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> source,
        BaseQuery query,
        CancellationToken cancellationToken = default)
    {
        return await source.ToPagedResultAsync(query.PageNumber, query.PageSize, cancellationToken);
    }

    /// <summary>
    /// Execute paginated query and return tuple (for backward compatibility)
    /// </summary>
    public static async Task<(IEnumerable<T> items, int totalCount)> ToPagedTupleAsync<T>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .ApplyPaging(pageNumber, pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <summary>
    /// Execute paginated query from BaseQuery and return tuple (for backward compatibility)
    /// </summary>
    public static async Task<(IEnumerable<T> items, int totalCount)> ToPagedTupleAsync<T>(
        this IQueryable<T> source,
        BaseQuery query,
        CancellationToken cancellationToken = default)
    {
        return await source.ToPagedTupleAsync(query.PageNumber, query.PageSize, cancellationToken);
    }

    #endregion
}
