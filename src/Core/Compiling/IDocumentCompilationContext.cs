// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling;

/// <summary>
/// Compilation context for document processing, providing access to XML generation,
/// diagnostic reporting, and semantic analysis capabilities.
/// </summary>
public interface IDocumentCompilationContext : ICompilationContext
{
    void AddPolicy(XNode element);
    void Report(Diagnostic diagnostic);

    SyntaxNode SyntaxRoot { get; }
    IList<Diagnostic> Diagnostics { get; }

    XElement RootElement { get; }
    XElement CurrentElement { get; }
}