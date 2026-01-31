// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling;

/// <summary>
/// Represents a value extracted from an initializer expression during compilation.
/// Can hold scalar values, arrays, or named object properties.
/// </summary>
public record InitializerValue
{
    /// <summary>
    /// Gets the optional name of this value (used in named assignments).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the scalar string value (for literals and resolved expressions).
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// Gets the type name of the initializer (e.g., "CertificateAuthenticationConfig").
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Gets the collection of unnamed values (for arrays and collections).
    /// </summary>
    public IReadOnlyList<InitializerValue>? UnnamedValues { get; init; }

    /// <summary>
    /// Gets the dictionary of named values (for object initializers).
    /// </summary>
    public IReadOnlyDictionary<string, InitializerValue>? NamedValues { get; init; }

    /// <summary>
    /// Gets the source syntax node for error location reporting.
    /// </summary>
    public required SyntaxNode Node { get; init; }

    /// <summary>
    /// Creates an InitializerValue for a scalar string value.
    /// </summary>
    public static InitializerValue ForScalar(string value, SyntaxNode node) =>
        new() { Value = value, Node = node };

    /// <summary>
    /// Creates an InitializerValue for an array of values.
    /// </summary>
    public static InitializerValue ForArray(IReadOnlyList<InitializerValue> values, SyntaxNode node, string? type = null) =>
        new() { UnnamedValues = values, Type = type, Node = node };

    /// <summary>
    /// Creates an InitializerValue for an object with named properties.
    /// </summary>
    public static InitializerValue ForObject(
        IReadOnlyDictionary<string, InitializerValue> namedValues,
        string typeName,
        SyntaxNode node) =>
        new() { NamedValues = namedValues, Type = typeName, Node = node };

    /// <summary>
    /// Attempts to get the named values if this is an object of the specified type.
    /// </summary>
    public bool TryGetValues<T>([NotNullWhen(true)] out IReadOnlyDictionary<string, InitializerValue>? namedValues)
    {
        if (Type == typeof(T).Name && NamedValues is not null)
        {
            namedValues = NamedValues;
            return true;
        }

        namedValues = null;
        return false;
    }
}
