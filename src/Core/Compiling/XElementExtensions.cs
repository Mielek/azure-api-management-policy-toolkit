// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling;

/// <summary>
/// Extension methods for XElement manipulation during policy compilation.
/// </summary>
public static class XElementExtensions
{
    /// <summary>
    /// Adds an attribute to the element if the specified key exists in the values dictionary.
    /// </summary>
    /// <param name="element">The element to add the attribute to.</param>
    /// <param name="values">The dictionary of initializer values.</param>
    /// <param name="key">The key to look up in the dictionary.</param>
    /// <param name="attributeName">The name of the attribute to add.</param>
    /// <returns>True if the attribute was added, false otherwise.</returns>
    public static bool AddAttribute(
        this XElement element,
        IReadOnlyDictionary<string, InitializerValue> values,
        string key,
        string attributeName)
    {
        if (values.TryGetValue(key, out var value) && value.Value is not null)
        {
            element.Add(new XAttribute(attributeName, value.Value));
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

    /// <summary>
    /// Adds an attribute to the element if the value is not null or empty.
    /// </summary>
    /// <param name="element">The element to add the attribute to.</param>
    /// <param name="attributeName">The name of the attribute to add.</param>
    /// <param name="value">The value of the attribute (may be null).</param>
    /// <returns>True if the attribute was added, false otherwise.</returns>
    public static bool AddAttributeIfNotEmpty(this XElement element, string attributeName, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            element.Add(new XAttribute(attributeName, value));
            return true;
        }

        return false;
    }
}
