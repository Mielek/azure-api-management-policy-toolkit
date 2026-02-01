// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling;

/// <summary>
/// Extension methods for XElement manipulation during policy compilation.
/// </summary>
public static class XElementExtensions
{
    /// <summary>
    /// Tries to add an attribute to the element if the value is not null.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="element">The element to add the attribute to.</param>
    /// <param name="attributeName">The name of the attribute to add.</param>
    /// <param name="value">The expression value (may be null).</param>
    /// <returns>True if the attribute was added, false otherwise.</returns>
    public static bool TryAddAttribute<T>(
        this XElement element,
        string attributeName,
        ExpressionValue<T>? value)
    {
        if (value is { } notNullValue)
        {
            element.Add(new XAttribute(attributeName, notNullValue.ToXmlValue()));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to add an attribute to the element if the value is not null.
    /// Uses <see cref="object.ToString"/> for conversion.
    /// </summary>
    /// <param name="element">The element to add the attribute to.</param>
    /// <param name="attributeName">The name of the attribute to add.</param>
    /// <param name="value">The value (may be null).</param>
    /// <returns>True if the attribute was added, false otherwise.</returns>
    public static bool TryAddAttribute(
        this XElement element,
        string attributeName,
        object? value)
    {
        if (value is not null)
        {
            element.Add(new XAttribute(attributeName, value.ToString()!));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Adds an attribute to the element with the specified name and value.
    /// </summary>
    /// <param name="element">The element to add the attribute to.</param>
    /// <param name="attributeName">The name of the attribute to add.</param>
    /// <param name="value">The value of the attribute.</param>
    public static void AddAttribute(this XElement element, string attributeName, string value)
    {
        element.Add(new XAttribute(attributeName, value));
    }
}
