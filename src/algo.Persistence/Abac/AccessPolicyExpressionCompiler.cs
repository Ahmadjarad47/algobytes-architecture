using System.Linq.Expressions;
using System.Reflection;
using algo.Application.Common.AccessPolicy;

namespace algo.Persistence.Abac;

public sealed class AccessPolicyExpressionCompiler
{
    public Expression<Func<TEntity, bool>> Compile<TEntity>(
        AccessPolicyConditionAst ast,
        AccessPolicyEntityMetadata metadata)
        where TEntity : class
    {
        if (metadata.EntityType != typeof(TEntity))
        {
            throw new InvalidOperationException(
                $"Metadata entity type {metadata.EntityType.Name} does not match {typeof(TEntity).Name}.");
        }

        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var body = BuildExpression(ast, metadata, parameter);
        return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
    }

    private static Expression BuildExpression(
        AccessPolicyConditionAst ast,
        AccessPolicyEntityMetadata metadata,
        ParameterExpression parameter) =>
        ast switch
        {
            AccessPolicyAllAst all => all.All.Select(n => BuildExpression(n, metadata, parameter))
                .Aggregate((Expression?)null, (acc, next) => acc is null ? next : Expression.AndAlso(acc, next))
                ?? Expression.Constant(true),
            AccessPolicyAnyAst any => any.Any.Select(n => BuildExpression(n, metadata, parameter))
                .Aggregate((Expression?)null, (acc, next) => acc is null ? next : Expression.OrElse(acc, next))
                ?? Expression.Constant(false),
            AccessPolicyFieldAst field => BuildFieldExpression(field, metadata, parameter),
            _ => throw new InvalidOperationException("Unknown condition node."),
        };

    private static Expression BuildFieldExpression(
        AccessPolicyFieldAst field,
        AccessPolicyEntityMetadata metadata,
        ParameterExpression parameter)
    {
        if (!metadata.Fields.TryGetValue(field.Field, out var fieldMeta))
        {
            throw new InvalidOperationException($"Unknown field '{field.Field}'.");
        }

        var property = metadata.EntityType.GetProperty(
            fieldMeta.PropertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
            ?? throw new InvalidOperationException($"Property '{fieldMeta.PropertyName}' not found.");

        var left = Expression.Property(parameter, property);
        var op = field.Operator;

        return op switch
        {
            "isNull" => Expression.Equal(left, Expression.Constant(null, left.Type)),
            "notNull" => Expression.NotEqual(left, Expression.Constant(null, left.Type)),
            _ => BuildValueOperator(left, property.PropertyType, op, field.Value),
        };
    }

    private static Expression BuildValueOperator(
        Expression left,
        Type propertyType,
        string op,
        object? value)
    {
        var targetNonNullable = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var convertedValue = CoerceValue(value, targetNonNullable);
        var right = Expression.Constant(convertedValue, propertyType);

        switch (op)
        {
            case "eq":
                return Expression.Equal(left, right);
            case "neq":
                return Expression.NotEqual(left, right);
            case "gt":
                return Expression.GreaterThan(left, right);
            case "gte":
                return Expression.GreaterThanOrEqual(left, right);
            case "lt":
                return Expression.LessThan(left, right);
            case "lte":
                return Expression.LessThanOrEqual(left, right);
            case "contains":
                return BuildStringContains(left, convertedValue as string);
            case "startsWith":
                return BuildStringStartsWith(left, convertedValue as string);
            case "endsWith":
                return BuildStringEndsWith(left, convertedValue as string);
            case "in":
                return BuildIn(left, propertyType, value, notIn: false);
            case "nin":
                return BuildIn(left, propertyType, value, notIn: true);
            default:
                throw new NotSupportedException($"Operator '{op}' is not supported for compilation.");
        }
    }

    private static Expression BuildStringContains(Expression left, string? substring)
    {
        if (substring is null)
        {
            throw new InvalidOperationException("'contains' requires a non-null string value.");
        }

        var notNull = Expression.NotEqual(left, Expression.Constant(null, left.Type));
        var contains = Expression.Call(
            left,
            typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!,
            Expression.Constant(substring));
        return Expression.AndAlso(notNull, contains);
    }

    private static Expression BuildStringStartsWith(Expression left, string? prefix)
    {
        if (prefix is null)
        {
            throw new InvalidOperationException("'startsWith' requires a non-null string value.");
        }

        var notNull = Expression.NotEqual(left, Expression.Constant(null, left.Type));
        var call = Expression.Call(
            left,
            typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!,
            Expression.Constant(prefix));
        return Expression.AndAlso(notNull, call);
    }

    private static Expression BuildStringEndsWith(Expression left, string? suffix)
    {
        if (suffix is null)
        {
            throw new InvalidOperationException("'endsWith' requires a non-null string value.");
        }

        var notNull = Expression.NotEqual(left, Expression.Constant(null, left.Type));
        var call = Expression.Call(
            left,
            typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) })!,
            Expression.Constant(suffix));
        return Expression.AndAlso(notNull, call);
    }

    private static Expression BuildIn(Expression left, Type propertyType, object? value, bool notIn)
    {
        if (value is not List<object?> list)
        {
            throw new InvalidOperationException("'in' / 'nin' requires an array value.");
        }

        var elementType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var array = Array.CreateInstance(elementType, list.Count);
        for (var i = 0; i < list.Count; i++)
        {
            var coerced = CoerceValue(list[i], elementType);
            array.SetValue(coerced, i);
        }

        var constant = Expression.Constant(array, array.GetType());
        var containsMethod = array.GetType().GetMethod("Contains", new[] { elementType })
            ?? throw new InvalidOperationException("Could not resolve Contains method for array.");

        var containsCall = Expression.Call(constant, containsMethod, left);
        return notIn ? Expression.Not(containsCall) : containsCall;
    }

    private static object? CoerceValue(object? value, Type targetNonNullable)
    {
        if (value is null)
        {
            if (targetNonNullable.IsValueType)
            {
                throw new InvalidOperationException("Cannot coerce null to non-nullable value type.");
            }

            return null;
        }

        if (targetNonNullable == typeof(string))
        {
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        if (targetNonNullable == typeof(bool) && value is bool b)
        {
            return b;
        }

        if (targetNonNullable == typeof(DateTimeOffset))
        {
            return value switch
            {
                DateTimeOffset dto => dto,
                string s => DateTimeOffset.Parse(s, System.Globalization.CultureInfo.InvariantCulture),
                _ => throw new InvalidOperationException("Unsupported value for DateTimeOffset."),
            };
        }

        if (targetNonNullable == typeof(Guid))
        {
            return value switch
            {
                Guid g => g,
                string s => Guid.Parse(s),
                _ => throw new InvalidOperationException("Unsupported value for Guid."),
            };
        }

        if (targetNonNullable.IsEnum)
        {
            return value switch
            {
                string s => Enum.Parse(targetNonNullable, s, ignoreCase: true),
                long enumLong => Enum.ToObject(targetNonNullable, checked((int)enumLong)),
                int i => Enum.ToObject(targetNonNullable, i),
                _ => throw new InvalidOperationException("Unsupported value for enum."),
            };
        }

        if (targetNonNullable == typeof(long) && value is long l)
        {
            return l;
        }

        if (targetNonNullable == typeof(int) && value is long li)
        {
            return checked((int)li);
        }

        if (targetNonNullable == typeof(int) && value is int ii)
        {
            return ii;
        }

        if (targetNonNullable == typeof(double) && value is double d)
        {
            return d;
        }

        if (targetNonNullable == typeof(double) && value is long ld)
        {
            return (double)ld;
        }

        try
        {
            return Convert.ChangeType(value, targetNonNullable, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot coerce value to {targetNonNullable.Name}.", ex);
        }
    }
}
