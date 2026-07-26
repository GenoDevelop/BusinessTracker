using System.Linq.Expressions;
using System.Reflection;
using GenoDev.BusinessTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public static class QueryableSearchExtensions
{
    public static IQueryable<T> ApplyNumericFilter<T>(this IQueryable<T> query,
        Expression<Func<T, double>> selector, NumericOperator? op, double? value)
    {
        if (op == null || value == null)
            return query;

        return op switch
        {
            NumericOperator.Equal => query.Where(Expression.Lambda<Func<T, bool>>(Expression.Equal(selector.Body, Expression.Constant(value.Value)), selector.Parameters)),
            NumericOperator.NotEqual => query.Where(Expression.Lambda<Func<T, bool>>(Expression.NotEqual(selector.Body, Expression.Constant(value.Value)), selector.Parameters)),
            NumericOperator.LessThan => query.Where(Expression.Lambda<Func<T, bool>>(Expression.LessThan(selector.Body, Expression.Constant(value.Value)), selector.Parameters)),
            NumericOperator.LessThanOrEqual => query.Where(Expression.Lambda<Func<T, bool>>(Expression.LessThanOrEqual(selector.Body, Expression.Constant(value.Value)), selector.Parameters)),
            NumericOperator.GreaterThan => query.Where(Expression.Lambda<Func<T, bool>>(Expression.GreaterThan(selector.Body, Expression.Constant(value.Value)), selector.Parameters)),
            NumericOperator.GreaterThanOrEqual => query.Where(Expression.Lambda<Func<T, bool>>(Expression.GreaterThanOrEqual(selector.Body, Expression.Constant(value.Value)), selector.Parameters)),
            _ => query
        };
    }
    
    public static IQueryable<T> WhereContainsAll<T>(
        this IQueryable<T> query,
        Expression<Func<T, string?>> propertySelector,
        string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return query;

        var terms = searchText
            .Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (terms.Length == 0)
            return query;

        var parameter = propertySelector.Parameters[0];
        var property = propertySelector.Body;

        Expression? predicate = null;

        foreach (var term in terms)
        {
            var pattern = $"%{EscapeLikePattern(term)}%";

            var ilikeExpression = Expression.Call(
                typeof(NpgsqlDbFunctionsExtensions),
                nameof(NpgsqlDbFunctionsExtensions.ILike),
                Type.EmptyTypes,
                Expression.Property(
                    expression: null,
                    typeof(EF),
                    nameof(EF.Functions)),
                property,
                Expression.Constant(pattern),
                Expression.Constant("\\"));

            predicate = predicate is null
                ? ilikeExpression
                : Expression.AndAlso(predicate, ilikeExpression);
        }

        var lambda = Expression.Lambda<Func<T, bool>>(
            predicate!,
            parameter);

        return query.Where(lambda);
    }
    
    public static IQueryable<T> WhereContainsAllInAny<T>(
        this IQueryable<T> query,
        string? searchText,
        params Expression<Func<T, string?>>[] columns)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(columns);

        if (string.IsNullOrWhiteSpace(searchText))
            return query;

        if (columns.Length == 0)
            throw new ArgumentException(
                "Należy przekazać przynajmniej jedną kolumnę.",
                nameof(columns));

        var terms = searchText
            .Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (terms.Length == 0)
            return query;

        var entityParameter = Expression.Parameter(typeof(T), "x");

        // AND pomiędzy poszczególnymi słowami.
        Expression allTermsExpression = Expression.Constant(true);

        foreach (var term in terms)
        {
            var pattern = $"%{EscapeLikePattern(term)}%";

            // Dzięki przechwyceniu wartości EF utworzy parametr SQL,
            // zamiast wstawiać wzorzec bezpośrednio do zapytania.
            Expression<Func<string>> patternExpression = () => pattern;

            // OR pomiędzy kolumnami dla konkretnego słowa.
            Expression anyColumnExpression = Expression.Constant(false);

            foreach (var columnSelector in columns)
            {
                var columnExpression = new ReplaceParameterVisitor(
                        columnSelector.Parameters[0],
                        entityParameter)
                    .Visit(columnSelector.Body)
                    ?? throw new InvalidOperationException(
                        "Nie udało się zbudować wyrażenia kolumny.");

                var columnIsNotNull = Expression.NotEqual(
                    columnExpression,
                    Expression.Constant(null, typeof(string)));

                var ilikeExpression = Expression.Call(
                    ILikeWithEscapeMethod,
                    Expression.Property(
                        expression: null,
                        typeof(EF),
                        nameof(EF.Functions)),
                    columnExpression,
                    patternExpression.Body,
                    Expression.Constant("\\"));

                var columnMatches = Expression.AndAlso(
                    columnIsNotNull,
                    ilikeExpression);

                anyColumnExpression = Expression.OrElse(
                    anyColumnExpression,
                    columnMatches);
            }

            allTermsExpression = Expression.AndAlso(
                allTermsExpression,
                anyColumnExpression);
        }

        var predicate = Expression.Lambda<Func<T, bool>>(
            allTermsExpression,
            entityParameter);

        return query.Where(predicate);
    }

    private static string EscapeLikePattern(string value)
    {
        // W PostgreSQL specjalnymi znakami LIKE są % i _.
        // Najpierw escapujemy sam znak escape.
        return value
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
    }

    private static readonly MethodInfo ILikeWithEscapeMethod =
        typeof(NpgsqlDbFunctionsExtensions).GetMethod(
            nameof(NpgsqlDbFunctionsExtensions.ILike),
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types:
            [
                typeof(DbFunctions),
                typeof(string),
                typeof(string),
                typeof(string)
            ],
            modifiers: null)
        ?? throw new InvalidOperationException("Nie znaleziono metody NpgsqlDbFunctionsExtensions.ILike.");

    private sealed class ReplaceParameterVisitor(
        ParameterExpression source,
        ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(
            ParameterExpression node)
        {
            return node == source
                ? target
                : base.VisitParameter(node);
        }
    }
}