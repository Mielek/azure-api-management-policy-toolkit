// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling;

/// <summary>
/// Extension methods for converting values to their XML string representations.
/// These complement the ExpressionValue{T}.ToXmlValue() method for plain (non-expression) values.
/// </summary>
public static class XmlValueExtensions
{
    /// <summary>
    /// Converts a string value to its XML representation.
    /// </summary>
    public static string ToXmlValue(this string? value) => value ?? string.Empty;

    /// <summary>
    /// Converts a boolean value to its XML representation (lowercase).
    /// </summary>
    public static string ToXmlValue(this bool value) => value ? "true" : "false";

    /// <summary>
    /// Converts a nullable boolean value to its XML representation (lowercase).
    /// </summary>
    public static string ToXmlValue(this bool? value) => value switch
    {
        true => "true",
        false => "false",
        null => string.Empty
    };

    /// <summary>
    /// Converts an integer value to its XML representation.
    /// </summary>
    public static string ToXmlValue(this int value) => value.ToString();

    /// <summary>
    /// Converts a nullable integer value to its XML representation.
    /// </summary>
    public static string ToXmlValue(this int? value) => value?.ToString() ?? string.Empty;

    /// <summary>
    /// Converts an unsigned integer value to its XML representation.
    /// </summary>
    public static string ToXmlValue(this uint value) => value.ToString();

    /// <summary>
    /// Converts a nullable unsigned integer value to its XML representation.
    /// </summary>
    public static string ToXmlValue(this uint? value) => value?.ToString() ?? string.Empty;

    /// <summary>
    /// Converts a long value to its XML representation.
    /// </summary>
    public static string ToXmlValue(this long value) => value.ToString();

    /// <summary>
    /// Converts a nullable long value to its XML representation.
    /// </summary>
    public static string ToXmlValue(this long? value) => value?.ToString() ?? string.Empty;

    /// <summary>
    /// Converts a double value to its XML representation.
    /// </summary>
    public static string ToXmlValue(this double value) => value.ToString();

    /// <summary>
    /// Converts a nullable double value to its XML representation.
    /// </summary>
    public static string ToXmlValue(this double? value) => value?.ToString() ?? string.Empty;

    /// <summary>
    /// Converts an enum value to its XML representation (kebab-case).
    /// </summary>
    public static string ToXmlValue<TEnum>(this TEnum value) where TEnum : struct, Enum
        => ToKebabCase(value.ToString());

    private static string ToKebabCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var result = new StringBuilder();
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    result.Append('-');
                }
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }
}
