// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;

/// <summary>
/// Represents a value that can be either a compile-time constant or an APIM policy expression.
/// Used in generated compiled config classes to wrap property values.
/// </summary>
/// <typeparam name="T">The type of the constant value.</typeparam>
public readonly struct ExpressionValue<T> : IEquatable<ExpressionValue<T>>
{
    private readonly T? _constantValue;
    private readonly string? _expression;

    private ExpressionValue(T? constantValue, string? expression, bool isExpression)
    {
        _constantValue = constantValue;
        _expression = expression;
        IsExpression = isExpression;
    }

    /// <summary>
    /// Gets a value indicating whether this value is an APIM policy expression.
    /// </summary>
    public bool IsExpression { get; }

    /// <summary>
    /// Gets a value indicating whether this value is a compile-time constant.
    /// </summary>
    public bool IsConstant => !IsExpression;

    /// <summary>
    /// Gets the constant value. Only valid when <see cref="IsConstant"/> is true.
    /// </summary>
    public T ConstantValue => IsConstant
        ? _constantValue!
        : throw new InvalidOperationException("Cannot access ConstantValue when IsExpression is true.");

    /// <summary>
    /// Gets the expression string. Only valid when <see cref="IsExpression"/> is true.
    /// </summary>
    public string Expression => IsExpression
        ? _expression!
        : throw new InvalidOperationException("Cannot access Expression when IsConstant is true.");

    /// <summary>
    /// Creates an <see cref="ExpressionValue{T}"/> from a compile-time constant.
    /// </summary>
    public static ExpressionValue<T> FromConstant(T value) =>
        new(value, null, isExpression: false);

    /// <summary>
    /// Creates an <see cref="ExpressionValue{T}"/> from an APIM policy expression string.
    /// </summary>
    public static ExpressionValue<T> FromExpression(string expression) =>
        new(default, expression ?? throw new ArgumentNullException(nameof(expression)), isExpression: true);

    /// <summary>
    /// Tries to get the constant value.
    /// </summary>
    public bool TryGetConstant([MaybeNullWhen(false)] out T value)
    {
        if (IsConstant)
        {
            value = _constantValue!;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Tries to get the expression string.
    /// </summary>
    public bool TryGetExpression([MaybeNullWhen(false)] out string expression)
    {
        if (IsExpression)
        {
            expression = _expression!;
            return true;
        }

        expression = null;
        return false;
    }

    /// <summary>
    /// Converts this value to its XML representation.
    /// For expressions, returns the expression string (e.g., "@(context.Request.Headers...)").
    /// For constants, returns the string representation of the value.
    /// </summary>
    public string ToXmlValue()
    {
        if (IsExpression)
        {
            return _expression!;
        }

        return _constantValue switch
        {
            null => string.Empty,
            bool b => b ? "true" : "false",
            Enum e => ToKebabCase(e.ToString()),
            _ => _constantValue.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// Converts this value to its XML representation, or null if the value is null.
    /// </summary>
    public string? ToXmlValueOrNull()
    {
        if (IsExpression)
        {
            return _expression;
        }

        return _constantValue switch
        {
            null => null,
            bool b => b ? "true" : "false",
            Enum e => ToKebabCase(e.ToString()),
            _ => _constantValue.ToString()
        };
    }

    private static string ToKebabCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var result = new System.Text.StringBuilder();
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    result.Append('-');
                }
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }

    /// <summary>
    /// Pattern matches on the value, executing the appropriate function.
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onConstant, Func<string, TResult> onExpression) =>
        IsExpression ? onExpression(_expression!) : onConstant(_constantValue!);

    /// <summary>
    /// Maps the constant value using the specified function.
    /// If this is an expression, returns an expression value with the same expression.
    /// </summary>
    public ExpressionValue<TResult> Map<TResult>(Func<T, TResult> mapper) =>
        IsExpression
            ? ExpressionValue<TResult>.FromExpression(_expression!)
            : ExpressionValue<TResult>.FromConstant(mapper(_constantValue!));

    public bool Equals(ExpressionValue<T> other) =>
        IsExpression == other.IsExpression &&
        EqualityComparer<T?>.Default.Equals(_constantValue, other._constantValue) &&
        _expression == other._expression;

    public override bool Equals(object? obj) =>
        obj is ExpressionValue<T> other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(IsExpression, _constantValue, _expression);

    public static bool operator ==(ExpressionValue<T> left, ExpressionValue<T> right) =>
        left.Equals(right);

    public static bool operator !=(ExpressionValue<T> left, ExpressionValue<T> right) =>
        !left.Equals(right);

    public override string ToString() =>
        IsExpression ? $"Expression({_expression})" : $"Constant({_constantValue})";

    /// <summary>
    /// Implicit conversion from a constant value to an <see cref="ExpressionValue{T}"/>.
    /// </summary>
    public static implicit operator ExpressionValue<T>(T value) => FromConstant(value);
}

/// <summary>
/// Extension methods for nullable <see cref="ExpressionValue{T}"/>.
/// </summary>
public static class ExpressionValueExtensions
{
    /// <summary>
    /// Converts a nullable <see cref="ExpressionValue{T}"/> to its XML representation, or null if the value is null.
    /// </summary>
    public static string? ToXmlValueOrNull<T>(this ExpressionValue<T>? value) =>
        value?.ToXmlValueOrNull();

    /// <summary>
    /// Creates an <see cref="ExpressionValue{T}"/> from a nullable constant, returning null if the constant is null.
    /// </summary>
    public static ExpressionValue<T>? FromNullableConstant<T>(T? value) where T : class =>
        value is null ? null : ExpressionValue<T>.FromConstant(value);

    /// <summary>
    /// Creates an <see cref="ExpressionValue{T}"/> from a nullable value type constant, returning null if the constant is null.
    /// </summary>
    public static ExpressionValue<T>? FromNullableConstant<T>(T? value) where T : struct =>
        value.HasValue ? ExpressionValue<T>.FromConstant(value.Value) : null;
}
