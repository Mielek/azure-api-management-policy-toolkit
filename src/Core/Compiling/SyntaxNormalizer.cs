// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling;

/// <summary>
/// Utility for normalizing syntax nodes by removing trivia and whitespace.
/// </summary>
public static class SyntaxNormalizer
{
    /// <summary>
    /// Removes all trivia from a syntax node and normalizes whitespace.
    /// </summary>
    public static T Normalize<T>(T node) where T : SyntaxNode
    {
        var unformatted = (T)new TriviaRemoverRewriter().Visit(node);
        return unformatted.NormalizeWhitespace("", "");
    }
}
