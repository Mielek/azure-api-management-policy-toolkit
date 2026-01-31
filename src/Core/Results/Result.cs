// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Results;

/// <summary>
/// Represents the result of an operation that can either succeed with a value
/// or fail with one or more diagnostics.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly IReadOnlyList<Diagnostic> _diagnostics;

    private Result(T? value, IReadOnlyList<Diagnostic> diagnostics, bool isSuccess)
    {
        _value = value;
        _diagnostics = diagnostics;
        IsSuccess = isSuccess;
    }

    /// <summary>
    /// Gets whether this result represents a successful operation.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets whether this result represents a failed operation.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the success value. Only valid when <see cref="IsSuccess"/> is true.
    /// </summary>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access Value on a failed Result.");

    /// <summary>
    /// Gets the diagnostics associated with this result.
    /// Empty for successful results, contains error information for failed results.
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    public static Result<T> Success(T value) =>
        new(value, Array.Empty<Diagnostic>(), isSuccess: true);

    /// <summary>
    /// Creates a failed result with the specified diagnostics.
    /// </summary>
    public static Result<T> Failure(params Diagnostic[] diagnostics) =>
        new(default, diagnostics, isSuccess: false);

    /// <summary>
    /// Creates a failed result with the specified diagnostics.
    /// </summary>
    public static Result<T> Failure(IEnumerable<Diagnostic> diagnostics) =>
        new(default, diagnostics.ToArray(), isSuccess: false);

    /// <summary>
    /// Transforms the success value using the specified function.
    /// If this result is a failure, returns a new failure with the same diagnostics.
    /// </summary>
    public Result<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        return IsSuccess
            ? Result<TResult>.Success(mapper(_value!))
            : Result<TResult>.Failure(_diagnostics);
    }

    /// <summary>
    /// Chains another operation that returns a Result.
    /// If this result is a failure, returns a new failure with the same diagnostics.
    /// </summary>
    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> binder)
    {
        return IsSuccess
            ? binder(_value!)
            : Result<TResult>.Failure(_diagnostics);
    }

    /// <summary>
    /// Pattern matches on the result, calling the appropriate function based on success or failure.
    /// </summary>
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<IReadOnlyList<Diagnostic>, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(_value!) : onFailure(_diagnostics);
    }

    /// <summary>
    /// Pattern matches on the result, executing the appropriate action based on success or failure.
    /// </summary>
    public void Match(
        Action<T> onSuccess,
        Action<IReadOnlyList<Diagnostic>> onFailure)
    {
        if (IsSuccess)
            onSuccess(_value!);
        else
            onFailure(_diagnostics);
    }

    /// <summary>
    /// Implicit conversion from a value to a successful Result.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);
}

/// <summary>
/// Provides static methods for working with Result types.
/// </summary>
public static class Result
{
    /// <summary>
    /// Combines multiple results, accumulating all diagnostics.
    /// Returns success only if all input results are successful.
    /// </summary>
    public static Result<(T1, T2)> Combine<T1, T2>(Result<T1> r1, Result<T2> r2)
    {
        var allDiagnostics = r1.Diagnostics.Concat(r2.Diagnostics).ToArray();

        if (r1.IsSuccess && r2.IsSuccess)
        {
            return allDiagnostics.Length == 0
                ? Result<(T1, T2)>.Success((r1.Value, r2.Value))
                : new CombinedResult<(T1, T2)>((r1.Value, r2.Value), allDiagnostics, isSuccess: true);
        }

        return Result<(T1, T2)>.Failure(allDiagnostics);
    }

    /// <summary>
    /// Combines multiple results, accumulating all diagnostics.
    /// Returns success only if all input results are successful.
    /// </summary>
    public static Result<(T1, T2, T3)> Combine<T1, T2, T3>(Result<T1> r1, Result<T2> r2, Result<T3> r3)
    {
        var allDiagnostics = r1.Diagnostics.Concat(r2.Diagnostics).Concat(r3.Diagnostics).ToArray();

        if (r1.IsSuccess && r2.IsSuccess && r3.IsSuccess)
        {
            return allDiagnostics.Length == 0
                ? Result<(T1, T2, T3)>.Success((r1.Value, r2.Value, r3.Value))
                : new CombinedResult<(T1, T2, T3)>((r1.Value, r2.Value, r3.Value), allDiagnostics, isSuccess: true);
        }

        return Result<(T1, T2, T3)>.Failure(allDiagnostics);
    }

    /// <summary>
    /// Combines multiple results, accumulating all diagnostics.
    /// Returns success only if all input results are successful.
    /// </summary>
    public static Result<(T1, T2, T3, T4)> Combine<T1, T2, T3, T4>(
        Result<T1> r1, Result<T2> r2, Result<T3> r3, Result<T4> r4)
    {
        var allDiagnostics = r1.Diagnostics
            .Concat(r2.Diagnostics)
            .Concat(r3.Diagnostics)
            .Concat(r4.Diagnostics)
            .ToArray();

        if (r1.IsSuccess && r2.IsSuccess && r3.IsSuccess && r4.IsSuccess)
        {
            return allDiagnostics.Length == 0
                ? Result<(T1, T2, T3, T4)>.Success((r1.Value, r2.Value, r3.Value, r4.Value))
                : new CombinedResult<(T1, T2, T3, T4)>(
                    (r1.Value, r2.Value, r3.Value, r4.Value), allDiagnostics, isSuccess: true);
        }

        return Result<(T1, T2, T3, T4)>.Failure(allDiagnostics);
    }

    /// <summary>
    /// Combines an array of results of the same type, accumulating all diagnostics.
    /// Returns success only if all input results are successful.
    /// </summary>
    public static Result<IReadOnlyList<T>> Combine<T>(params Result<T>[] results)
    {
        var allDiagnostics = results.SelectMany(r => r.Diagnostics).ToArray();
        var allSuccessful = results.All(r => r.IsSuccess);

        if (allSuccessful)
        {
            var values = results.Select(r => r.Value).ToArray();
            return allDiagnostics.Length == 0
                ? Result<IReadOnlyList<T>>.Success(values)
                : new CombinedResult<IReadOnlyList<T>>(values, allDiagnostics, isSuccess: true);
        }

        return Result<IReadOnlyList<T>>.Failure(allDiagnostics);
    }
}

/// <summary>
/// Internal struct for combined results that may have diagnostics even on success.
/// </summary>
internal readonly struct CombinedResult<T>
{
    private readonly T _value;
    private readonly IReadOnlyList<Diagnostic> _diagnostics;
    private readonly bool _isSuccess;

    public CombinedResult(T value, IReadOnlyList<Diagnostic> diagnostics, bool isSuccess)
    {
        _value = value;
        _diagnostics = diagnostics;
        _isSuccess = isSuccess;
    }

    public static implicit operator Result<T>(CombinedResult<T> combined)
    {
        return combined._isSuccess
            ? Result<T>.Success(combined._value)
            : Result<T>.Failure(combined._diagnostics);
    }
}
