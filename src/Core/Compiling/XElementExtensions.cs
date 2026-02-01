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
    #region AddAttribute (Required)

    /// <summary>
    /// Adds an attribute to the element with the specified name and value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="element">The element to add the attribute to.</param>
    /// <param name="attributeName">The name of the attribute to add.</param>
    /// <param name="value">The expression value.</param>
    public static void AddAttribute<T>(
        this XElement element,
        string attributeName,
        ExpressionValue<T> value)
    {
        element.Add(new XAttribute(attributeName, value.ToXmlValue()));
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
    /// Adds an attribute to the element with the specified name and value.
    /// </summary>
    /// <param name="element">The element to add the attribute to.</param>
    /// <param name="attributeName">The name of the attribute to add.</param>
    /// <param name="value">The value of the attribute.</param>
    public static void AddAttribute(this XElement element, string attributeName, int value)
    {
        element.Add(new XAttribute(attributeName, value));
    }

    public static void AddAttribute(this XElement element, string attributeName, uint value)
    {
        element.Add(new XAttribute(attributeName, value));
    }

    /// <summary>
    /// Adds an attribute to the element with the specified name and value.
    /// </summary>
    /// <param name="element">The element to add the attribute to.</param>
    /// <param name="attributeName">The name of the attribute to add.</param>
    /// <param name="value">The value of the attribute.</param>
    public static void AddAttribute(this XElement element, string attributeName, bool value)
    {
        element.Add(new XAttribute(attributeName, value.ToString().ToLowerInvariant()));
    }

    #endregion

    #region AddOptionalAttribute

    /// <summary>
    /// Adds an attribute to the element if the value is not null.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="element">The element to add the attribute to.</param>
    /// <param name="attributeName">The name of the attribute to add.</param>
    /// <param name="value">The expression value (may be null).</param>
    /// <returns>True if the attribute was added, false otherwise.</returns>
    public static bool AddOptionalAttribute<T>(
        this XElement element,
        string attributeName,
        ExpressionValue<T>? value)
    {
        if (value is { } notNullValue)
        {
            element.AddAttribute(attributeName, notNullValue);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Adds an attribute to the element if the value is not null.
    /// Uses <see cref="object.ToString"/> for conversion.
    /// </summary>
    /// <param name="element">The element to add the attribute to.</param>
    /// <param name="attributeName">The name of the attribute to add.</param>
    /// <param name="value">The value (may be null).</param>
    /// <returns>True if the attribute was added, false otherwise.</returns>
    public static bool AddOptionalAttribute(
        this XElement element,
        string attributeName,
        object? value)
    {
        if (value is not null)
        {
            element.AddAttribute(attributeName, value.ToString()!);
            return true;
        }

        return false;
    }

    #endregion

    #region AddElement (Required)

    /// <summary>
    /// Adds a child element with the specified name and value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="element">The parent element to add the child to.</param>
    /// <param name="elementName">The name of the child element.</param>
    /// <param name="value">The expression value for the element content.</param>
    /// <returns>The newly created child element.</returns>
    public static XElement AddElement<T>(
        this XElement element,
        string elementName,
        ExpressionValue<T> value)
    {
        var child = new XElement(elementName, value.ToXmlValue());
        element.Add(child);
        return child;
    }

    /// <summary>
    /// Adds a child element with the specified name and value.
    /// </summary>
    /// <param name="element">The parent element to add the child to.</param>
    /// <param name="elementName">The name of the child element.</param>
    /// <param name="value">The value for the element content.</param>
    /// <returns>The newly created child element.</returns>
    public static XElement AddElement(
        this XElement element,
        string elementName,
        string value)
    {
        var child = new XElement(elementName, value);
        element.Add(child);
        return child;
    }

    /// <summary>
    /// Adds an empty child element with the specified name.
    /// </summary>
    /// <param name="element">The parent element to add the child to.</param>
    /// <param name="elementName">The name of the child element.</param>
    /// <returns>The newly created child element.</returns>
    public static XElement AddElement(
        this XElement element,
        string elementName)
    {
        var child = new XElement(elementName);
        element.Add(child);
        return child;
    }

    public static XElement AddElement(
        this XElement element,
        string elementName,
        object[] values)
    {
        var child = new XElement(elementName, values);
        element.Add(child);
        return child;
    }

    #endregion

    #region AddOptionalElement

    /// <summary>
    /// Adds a child element if the value is not null.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="element">The parent element to add the child to.</param>
    /// <param name="elementName">The name of the child element.</param>
    /// <param name="value">The expression value (may be null).</param>
    /// <returns>The newly created child element if added, null otherwise.</returns>
    public static XElement? AddOptionalElement<T>(
        this XElement element,
        string elementName,
        ExpressionValue<T>? value)
    {
        if (value is { } notNullValue)
        {
            var child = new XElement(elementName, notNullValue.ToXmlValue());
            element.Add(child);
            return child;
        }

        return null;
    }

    /// <summary>
    /// Adds a child element if the value is not null.
    /// </summary>
    /// <param name="element">The parent element to add the child to.</param>
    /// <param name="elementName">The name of the child element.</param>
    /// <param name="value">The value (may be null).</param>
    /// <returns>The newly created child element if added, null otherwise.</returns>
    public static XElement? AddOptionalElement(
        this XElement element,
        string elementName,
        string? value)
    {
        if (value is not null)
        {
            var child = new XElement(elementName, value);
            element.Add(child);
            return child;
        }

        return null;
    }

    #endregion
}
