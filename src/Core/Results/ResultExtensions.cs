// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling;
using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Results;

/// <summary>
/// Extension methods for working with Result types.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Collects an enumerable of results into a single result containing all values.
    /// Accumulates all diagnostics from all results.
    /// </summary>
    public static Result<IReadOnlyList<T>> Collect<T>(this IEnumerable<Result<T>> results)
    {
        var resultList = results.ToList();
        var allDiagnostics = resultList.SelectMany(r => r.Diagnostics).ToList();
        var allSuccessful = resultList.All(r => r.IsSuccess);

        if (allSuccessful)
        {
            var values = resultList.Select(r => r.Value).ToList();
            return allDiagnostics.Count == 0
                ? Result<IReadOnlyList<T>>.Success(values)
                : Result<IReadOnlyList<T>>.Success(values);
        }

        return Result<IReadOnlyList<T>>.Failure(allDiagnostics);
    }

    /// <summary>
    /// Reports all diagnostics from the result to the compilation context.
    /// Bridges the functional Result world to the side-effect world at policy compiler boundaries.
    /// </summary>
    public static void ReportAll<T>(this Result<T> result, IDocumentCompilationContext context)
    {
        foreach (var diagnostic in result.Diagnostics)
        {
            context.Report(diagnostic);
        }
    }

    /// <summary>
    /// Gets the value if successful, otherwise returns the default value.
    /// </summary>
    public static T GetValueOrDefault<T>(this Result<T> result, T defaultValue)
    {
        return result.IsSuccess ? result.Value : defaultValue;
    }

    /// <summary>
    /// Gets the value if successful, otherwise returns the result of the factory function.
    /// </summary>
    public static T GetValueOrElse<T>(this Result<T> result, Func<T> defaultFactory)
    {
        return result.IsSuccess ? result.Value : defaultFactory();
    }

    /// <summary>
    /// Converts a successful result to a nullable, returning null for failures.
    /// </summary>
    public static T? ToNullable<T>(this Result<T> result) where T : class
    {
        return result.IsSuccess ? result.Value : null;
    }

    /// <summary>
    /// Converts a successful result to a nullable value type, returning null for failures.
    /// </summary>
    public static T? ToNullableValue<T>(this Result<T> result) where T : struct
    {
        return result.IsSuccess ? result.Value : null;
    }

    /// <summary>
    /// Filters a result based on a predicate. If the predicate returns false,
    /// the result becomes a failure with the provided diagnostic.
    /// </summary>
    public static Result<T> Where<T>(
        this Result<T> result,
        Func<T, bool> predicate,
        Func<Diagnostic> diagnosticFactory)
    {
        if (result.IsFailure)
            return result;

        return predicate(result.Value)
            ? result
            : Result<T>.Failure(diagnosticFactory());
    }

    /// <summary>
    /// Adds additional diagnostics to a result without changing its success/failure state.
    /// </summary>
    public static Result<T> WithDiagnostics<T>(this Result<T> result, params Diagnostic[] additionalDiagnostics)
    {
        if (additionalDiagnostics.Length == 0)
            return result;

        var allDiagnostics = result.Diagnostics.Concat(additionalDiagnostics).ToArray();
        return result.IsSuccess
            ? Result<T>.Success(result.Value)
            : Result<T>.Failure(allDiagnostics);
    }
}
