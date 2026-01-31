// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Generators;

/// <summary>
/// Analyzes config classes to extract information needed for code generation.
/// </summary>
internal class ConfigAnalyzer
{
    private const string XmlNameAttributeName = "Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring.XmlNameAttribute";
    private readonly Compilation _compilation;
    private readonly INamedTypeSymbol? _xmlNameAttribute;

    public ConfigAnalyzer(Compilation compilation)
    {
        _compilation = compilation;
        _xmlNameAttribute = compilation.GetTypeByMetadataName(XmlNameAttributeName);
    }

    public ConfigInfo? Analyze(INamedTypeSymbol classSymbol)
    {
        var properties = new List<PropertyInfo>();
        var implementedInterfaces = new List<INamedTypeSymbol>();

        // Get all public properties with init or set accessors
        foreach (var member in classSymbol.GetMembers())
        {
            if (member is IPropertySymbol property &&
                property.DeclaredAccessibility == Accessibility.Public &&
                !property.IsStatic &&
                !property.IsIndexer &&
                (property.SetMethod is not null || HasInitAccessor(property)))
            {
                var propInfo = AnalyzeProperty(property);
                if (propInfo is not null)
                {
                    properties.Add(propInfo);
                }
            }
        }

        // Collect implemented interfaces that might need union generation
        foreach (var iface in classSymbol.AllInterfaces)
        {
            // Only track interfaces from the same assembly or Authoring assembly
            if (IsRelevantInterface(iface))
            {
                implementedInterfaces.Add(iface);
            }
        }

        return new ConfigInfo(
            classSymbol.ContainingNamespace.ToDisplayString(),
            classSymbol.Name,
            properties.ToImmutableArray(),
            implementedInterfaces.ToImmutableArray()
        );
    }

    private PropertyInfo? AnalyzeProperty(IPropertySymbol property)
    {
        var type = property.Type;
        var xmlName = GetXmlName(property);
        var isRequired = IsRequired(property);
        var isNullable = IsNullableType(type);

        // Get the underlying type if it's nullable
        var underlyingType = GetUnderlyingType(type);

        return new PropertyInfo(
            property.Name,
            xmlName ?? ToKebabCase(property.Name),
            GetTypeFullName(underlyingType),
            isRequired,
            isNullable,
            IsEnumType(underlyingType),
            IsInterfaceType(underlyingType),
            IsCollectionType(type),
            GetCollectionElementType(type)
        );
    }

    private string? GetXmlName(IPropertySymbol property)
    {
        if (_xmlNameAttribute is null)
        {
            return null;
        }

        var attr = property.GetAttributes().FirstOrDefault(a =>
            SymbolEqualityComparer.Default.Equals(a.AttributeClass, _xmlNameAttribute));

        if (attr is not null && attr.ConstructorArguments.Length > 0)
        {
            return attr.ConstructorArguments[0].Value?.ToString();
        }

        return null;
    }

    private static bool HasInitAccessor(IPropertySymbol property)
    {
        return property.SetMethod?.IsInitOnly == true;
    }

    private static bool IsRequired(IPropertySymbol property)
    {
        return property.IsRequired;
    }

    private static bool IsNullableType(ITypeSymbol type)
    {
        if (type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return true;
        }

        if (type is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return true;
        }

        return false;
    }

    private static ITypeSymbol GetUnderlyingType(ITypeSymbol type)
    {
        // Handle Nullable<T>
        if (type is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return namedType.TypeArguments[0];
        }

        return type;
    }

    private static string GetTypeFullName(ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static bool IsEnumType(ITypeSymbol type)
    {
        return type.TypeKind == TypeKind.Enum;
    }

    private static bool IsInterfaceType(ITypeSymbol type)
    {
        return type.TypeKind == TypeKind.Interface;
    }

    private static bool IsCollectionType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol)
        {
            return true;
        }

        if (type is INamedTypeSymbol namedType)
        {
            var fullName = namedType.OriginalDefinition.ToDisplayString();
            return fullName.StartsWith("System.Collections.Generic.IList<", StringComparison.Ordinal) ||
                   fullName.StartsWith("System.Collections.Generic.List<", StringComparison.Ordinal) ||
                   fullName.StartsWith("System.Collections.Generic.IEnumerable<", StringComparison.Ordinal) ||
                   fullName.StartsWith("System.Collections.Generic.ICollection<", StringComparison.Ordinal);
        }

        return false;
    }

    private static string? GetCollectionElementType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol arrayType)
        {
            return GetTypeFullName(arrayType.ElementType);
        }

        if (type is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1)
        {
            return GetTypeFullName(namedType.TypeArguments[0]);
        }

        return null;
    }

    private bool IsRelevantInterface(INamedTypeSymbol iface)
    {
        // Track interfaces that are in the Authoring namespace (likely config interfaces)
        var ns = iface.ContainingNamespace.ToDisplayString();
        return ns.StartsWith("Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring", StringComparison.Ordinal);
    }

    private static string ToKebabCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var result = new System.Text.StringBuilder();
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

/// <summary>
/// Information about a config class to generate.
/// </summary>
internal record ConfigInfo(
    string Namespace,
    string ClassName,
    ImmutableArray<PropertyInfo> Properties,
    ImmutableArray<INamedTypeSymbol> ImplementedInterfaces
);

/// <summary>
/// Information about a property to generate.
/// </summary>
internal record PropertyInfo(
    string Name,
    string XmlName,
    string TypeFullName,
    bool IsRequired,
    bool IsNullable,
    bool IsEnum,
    bool IsInterface,
    bool IsCollection,
    string? CollectionElementType
);
