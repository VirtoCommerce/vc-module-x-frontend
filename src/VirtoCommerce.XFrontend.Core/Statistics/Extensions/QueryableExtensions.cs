using System;
using System.Linq;
using System.Linq.Expressions;

namespace VirtoCommerce.XFrontend.Core.Statistics.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> WhereBetween<T>(this IQueryable<T> query, Expression<Func<T, DateTime>> date, DateTime? from, DateTime? to)
    {
        if (from != null)
        {
            query = query.Where(Compare(date, Expression.GreaterThanOrEqual, from.Value));
        }

        if (to != null)
        {
            query = query.Where(Compare(date, Expression.LessThanOrEqual, to.Value));
        }

        return query;
    }

    private static Expression<Func<T, bool>> Compare<T>(Expression<Func<T, DateTime>> date, Func<Expression, Expression, BinaryExpression> comparison, DateTime bound)
    {
        // Read through a closure, not inlined as a constant, so EF sends the bound as a query parameter.
        Expression<Func<DateTime>> value = () => bound;

        return Expression.Lambda<Func<T, bool>>(comparison(date.Body, value.Body), date.Parameters);
    }
}
