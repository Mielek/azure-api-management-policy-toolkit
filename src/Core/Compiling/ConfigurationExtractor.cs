// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling;

/// <summary>
/// Extracts typed configuration objects from policy method invocations.
/// Validates argument count, type, and structure.
/// </summary>
public static class ConfigurationExtractor
{
    /// <summary>
    /// Extracts configuration from a policy invocation that expects a single configuration argument.
    /// </summary>
    /// <typeparam name="TConfig">The expected configuration type.</typeparam>
    /// <param name="node">The invocation expression to extract from.</param>
    /// <param name="context">The compilation context for semantic analysis.</param>
    /// <param name="policyName">The policy name for error messages.</param>
    /// <returns>A result containing the extracted named values or diagnostics.</returns>
    public static Result<IReadOnlyDictionary<string, InitializerValue>> Extract<TConfig>(
        InvocationExpressionSyntax node,
        ICompilationContext context,
        string policyName)
    {
        if (node.ArgumentList.Arguments.Count != 1)
        {
            return Result<IReadOnlyDictionary<string, InitializerValue>>.Failure(
                Diagnostic.Create(
                    CompilationErrors.ArgumentCountMissMatchForPolicy,
                    node.ArgumentList.GetLocation(),
                    policyName));
        }

        return ExtractFromExpression<TConfig>(
            node.ArgumentList.Arguments[0].Expression,
            context,
            policyName);
    }

    /// <summary>
    /// Extracts configuration from an expression that should be an object creation of the specified type.
    /// </summary>
    /// <typeparam name="TConfig">The expected configuration type.</typeparam>
    /// <param name="expression">The expression to extract from.</param>
    /// <param name="context">The compilation context for semantic analysis.</param>
    /// <param name="policyName">The policy name for error messages.</param>
    /// <returns>A result containing the extracted named values or diagnostics.</returns>
    public static Result<IReadOnlyDictionary<string, InitializerValue>> ExtractFromExpression<TConfig>(
        ExpressionSyntax expression,
        ICompilationContext context,
        string policyName)
    {
        if (expression is not ObjectCreationExpressionSyntax config)
        {
            return Result<IReadOnlyDictionary<string, InitializerValue>>.Failure(
                Diagnostic.Create(
                    CompilationErrors.PolicyArgumentIsNotAnObjectCreation,
                    expression.GetLocation(),
                    policyName,
                    typeof(TConfig).Name));
        }

        var initializerResult = ExpressionProcessor.ProcessToInitializerValue(config, context);
        
        if (initializerResult.IsFailure)
        {
            return Result<IReadOnlyDictionary<string, InitializerValue>>.Failure(initializerResult.Diagnostics);
        }

        var initializer = initializerResult.Value;
        
        if (!initializer.TryGetValues<TConfig>(out var result))
        {
            return Result<IReadOnlyDictionary<string, InitializerValue>>.Failure(
                Diagnostic.Create(
                    CompilationErrors.PolicyArgumentIsNotOfRequiredType,
                    expression.GetLocation(),
                    policyName,
                    typeof(TConfig).Name));
        }

        return Result<IReadOnlyDictionary<string, InitializerValue>>.Success(result);
    }
}
