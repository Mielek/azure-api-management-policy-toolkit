// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;

/// <summary>
/// Specifies the XML attribute or element name to use when serializing a property.
/// Used in conjunction with <see cref="GenerateCompiledConfigAttribute"/> to customize
/// the XML output for specific properties.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class XmlNameAttribute : Attribute
{
    /// <summary>
    /// Gets the XML name for the property.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlNameAttribute"/> class.
    /// </summary>
    /// <param name="name">The XML attribute or element name to use.</param>
    public XmlNameAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
