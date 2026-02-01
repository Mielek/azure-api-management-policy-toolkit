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
    private const string ExpressionAllowedAttributeName = "Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring.ExpressionAllowedAttribute";
    private const string AuthoringNamespace = "Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring";
    
    private readonly Compilation _compilation;
    private readonly INamedTypeSymbol? _expressionAllowedAttribute;

    public ConfigAnalyzer(Compilation compilation)
    {
        _compilation = compilation;
        _expressionAllowedAttribute = compilation.GetTypeByMetadataName(ExpressionAllowedAttributeName);
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

        // Get base type if it's one of our config types
        string? baseTypeName = null;
        if (classSymbol.BaseType is not null && IsAuthoringConfigType(classSymbol.BaseType))
        {
            baseTypeName = classSymbol.BaseType.Name;
        }

        return new ConfigInfo(
            classSymbol.ContainingNamespace.ToDisplayString(),
            classSymbol.Name,
            classSymbol.IsAbstract,
            HasDerivedClasses: false, // Will be updated in a second pass
            baseTypeName,
            properties.ToImmutableArray(),
            implementedInterfaces.ToImmutableArray()
        );
    }

    private PropertyInfo? AnalyzeProperty(IPropertySymbol property)
    {
        var type = property.Type;
        var isRequired = IsRequired(property);
        var isNullable = IsNullableType(type);
        var isExpressionAllowed = HasExpressionAllowedAttribute(property);

        // Get the underlying type if it's nullable
        var underlyingType = GetUnderlyingType(type);

        var isCollection = IsCollectionType(type);
        var collectionElementType = GetCollectionElementType(type);
        var collectionElementIsCompiledConfig = isCollection && IsAuthoringConfigType(GetCollectionElementTypeSymbol(type));
        
        // Check if non-collection type is a nested compiled config
        var isNestedCompiledConfig = !isCollection && IsAuthoringConfigType(underlyingType);

        return new PropertyInfo(
            property.Name,
            ToKebabCase(property.Name),
            GetTypeFullName(underlyingType),
            isRequired,
            isNullable,
            IsEnumType(underlyingType),
            IsInterfaceType(underlyingType),
            isCollection,
            collectionElementType,
            collectionElementIsCompiledConfig,
            isExpressionAllowed,
            isNestedCompiledConfig
        );
    }

    private bool HasExpressionAllowedAttribute(IPropertySymbol property)
    {
        if (_expressionAllowedAttribute is null)
        {
            return false;
        }

        return property.GetAttributes().Any(attr =>
            SymbolEqualityComparer.Default.Equals(attr.AttributeClass, _expressionAllowedAttribute));
    }

    /// <summary>
    /// Checks if a type is a config class that should have a compiled config generated.
    /// Uses namespace convention: any record/class in the Authoring namespace.
    /// </summary>
    private static bool IsAuthoringConfigType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null)
        {
            return false;
        }

        // Must be a class or record in the Authoring namespace
        if (typeSymbol is not INamedTypeSymbol namedType)
        {
            return false;
        }

        if (namedType.TypeKind != TypeKind.Class)
        {
            return false;
        }

        var ns = namedType.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        return ns.StartsWith("Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring", StringComparison.Ordinal);
    }

    private static ITypeSymbol? GetCollectionElementTypeSymbol(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol arrayType)
        {
            return arrayType.ElementType;
        }

        if (type is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1)
        {
            return namedType.TypeArguments[0];
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
        if (type is IArrayTypeSymbol arrayType)
        {
            // byte[] is treated as a scalar (binary blob), not a collection
            if (arrayType.ElementType.SpecialType == SpecialType.System_Byte)
            {
                return false;
            }
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
    bool IsAbstract,
    bool HasDerivedClasses,
    string? BaseTypeName,
    ImmutableArray<PropertyInfo> Properties,
    ImmutableArray<INamedTypeSymbol> ImplementedInterfaces
)
{
    /// <summary>
    /// Creates a new ConfigInfo with the HasDerivedClasses flag set.
    /// </summary>
    public ConfigInfo WithHasDerivedClasses(bool hasDerivedClasses) =>
        this with { HasDerivedClasses = hasDerivedClasses };
}

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
    string? CollectionElementType,
    bool CollectionElementIsCompiledConfig = false,
    bool IsExpressionAllowed = false,
    bool IsNestedCompiledConfig = false
);
