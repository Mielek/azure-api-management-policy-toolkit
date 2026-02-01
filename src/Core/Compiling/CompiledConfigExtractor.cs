// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling;

/// <summary>
/// Provides extraction methods that delegate to generated extractors via IExtractableConfig.
/// </summary>
public static class CompiledConfigExtractor
{
    /// <summary>
    /// Extracts a compiled config from a policy invocation.
    /// </summary>
    public static Result<TCompiledConfig> Extract<TCompiledConfig>(
        InvocationExpressionSyntax node,
        ICompilationContext context,
        string policyName)
        where TCompiledConfig : class, IExtractableConfig<TCompiledConfig>
    {
        return TCompiledConfig.Extract(node, context, policyName);
    }

    /// <summary>
    /// Extracts a compiled config from an expression.
    /// </summary>
    public static Result<TCompiledConfig> ExtractFromExpression<TCompiledConfig>(
        ExpressionSyntax expression,
        ICompilationContext context,
        string policyName)
        where TCompiledConfig : class, IExtractableConfig<TCompiledConfig>
    {
        return TCompiledConfig.ExtractFromExpression(expression, context, policyName);
    }
}
