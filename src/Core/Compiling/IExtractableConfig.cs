// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling;

/// <summary>
/// Interface for compiled config types that support strongly-typed extraction.
/// Implemented by generated compiled config classes.
/// </summary>
public interface IExtractableConfig<TSelf> where TSelf : IExtractableConfig<TSelf>
{
    /// <summary>
    /// Extracts the config from an invocation expression.
    /// </summary>
    static abstract Result<TSelf> Extract(
        InvocationExpressionSyntax node,
        ICompilationContext context,
        string policyName);

    /// <summary>
    /// Extracts the config from an object creation expression.
    /// </summary>
    static abstract Result<TSelf> ExtractFromExpression(
        ExpressionSyntax expression,
        ICompilationContext context,
        string policyName);
}
