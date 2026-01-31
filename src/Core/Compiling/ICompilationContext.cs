// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling;

/// <summary>
/// Minimal interface providing access to Roslyn Compilation for semantic analysis.
/// Used by processors that need symbol resolution (method bodies, constant field values).
/// </summary>
public interface ICompilationContext
{
    /// <summary>
    /// Gets the Roslyn Compilation for semantic model access.
    /// </summary>
    Compilation Compilation { get; }
}
