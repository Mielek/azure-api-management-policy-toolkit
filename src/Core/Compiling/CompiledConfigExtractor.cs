// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Reflection;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling;

/// <summary>
/// Extracts strongly-typed compiled config objects from policy method invocations.
/// Auto-discovers properties using reflection on the compiled config type.
/// </summary>
public static class CompiledConfigExtractor
{
    // Cache for property metadata per compiled config type
    private static readonly ConcurrentDictionary<Type, ConfigTypeMetadata> MetadataCache = new();

    /// <summary>
    /// Extracts a compiled config from a policy invocation that expects a single configuration argument.
    /// Properties are auto-discovered from the compiled config type using [ConfigProperty] attributes.
    /// </summary>
    /// <typeparam name="TCompiledConfig">The compiled config type to create.</typeparam>
    /// <param name="node">The invocation expression to extract from.</param>
    /// <param name="context">The compilation context for semantic analysis.</param>
    /// <param name="policyName">The policy name for error messages.</param>
    /// <returns>A result containing the compiled config or diagnostics.</returns>
    public static Result<TCompiledConfig> Extract<TCompiledConfig>(
        InvocationExpressionSyntax node,
        ICompilationContext context,
        string policyName)
        where TCompiledConfig : class
    {
        if (node.ArgumentList.Arguments.Count != 1)
        {
            return Result<TCompiledConfig>.Failure(
                Diagnostic.Create(
                    CompilationErrors.ArgumentCountMissMatchForPolicy,
                    node.ArgumentList.GetLocation(),
                    policyName));
        }

        return ExtractFromExpression<TCompiledConfig>(
            node.ArgumentList.Arguments[0].Expression,
            context,
            policyName);
    }

    /// <summary>
    /// Extracts a compiled config from an expression that should be an object creation.
    /// Properties are auto-discovered from the compiled config type using [ConfigProperty] attributes.
    /// </summary>
    public static Result<TCompiledConfig> ExtractFromExpression<TCompiledConfig>(
        ExpressionSyntax expression,
        ICompilationContext context,
        string policyName)
        where TCompiledConfig : class
    {
        if (expression is not ObjectCreationExpressionSyntax objectCreation)
        {
            return Result<TCompiledConfig>.Failure(
                Diagnostic.Create(
                    CompilationErrors.PolicyArgumentIsNotAnObjectCreation,
                    expression.GetLocation(),
                    policyName,
                    typeof(TCompiledConfig).Name));
        }

        var metadata = GetOrCreateMetadata<TCompiledConfig>();
        var diagnostics = new List<Diagnostic>();
        
        // Extract properties from the syntax
        var syntaxProperties = ExtractSyntaxProperties(objectCreation);
        
        // Create the compiled config instance using Activator (supports required members)
        var config = (TCompiledConfig)Activator.CreateInstance(typeof(TCompiledConfig), nonPublic: true)!;
        
        // Process each property in the compiled config type
        foreach (var propMeta in metadata.Properties)
        {
            if (!syntaxProperties.TryGetValue(propMeta.SourcePropertyName, out var syntaxExpression))
            {
                // Property not provided in source
                if (propMeta.IsRequired)
                {
                    diagnostics.Add(Diagnostic.Create(
                        CompilationErrors.RequiredParameterNotDefined,
                        objectCreation.GetLocation(),
                        policyName,
                        propMeta.SourcePropertyName));
                }
                continue;
            }

            // Extract the value based on the property type
            var extractResult = ExtractPropertyValue(syntaxExpression, context, propMeta, policyName);
            if (extractResult.IsFailure)
            {
                diagnostics.AddRange(extractResult.Diagnostics);
                continue;
            }

            // Set the property value
            if (extractResult.Value != null)
            {
                propMeta.PropertyInfo.SetValue(config, extractResult.Value);
            }
        }

        if (diagnostics.Count > 0)
        {
            return Result<TCompiledConfig>.Failure(diagnostics);
        }

        return config;
    }

    /// <summary>
    /// Extracts compiled configs from an array/collection of config objects.
    /// </summary>
    public static Result<IReadOnlyList<TCompiledConfig>> ExtractArray<TCompiledConfig>(
        ExpressionSyntax expression,
        ICompilationContext context,
        string policyName)
        where TCompiledConfig : class
    {
        var items = new List<TCompiledConfig>();
        var diagnostics = new List<Diagnostic>();

        IEnumerable<ExpressionSyntax> expressions = expression switch
        {
            ArrayCreationExpressionSyntax array => array.Initializer?.Expressions ?? [],
            ImplicitArrayCreationExpressionSyntax implicitArray => implicitArray.Initializer.Expressions,
            CollectionExpressionSyntax collection => collection.Elements.OfType<ExpressionElementSyntax>().Select(e => e.Expression),
            _ => []
        };

        foreach (var itemExpression in expressions)
        {
            var result = ExtractFromExpression<TCompiledConfig>(itemExpression, context, policyName);
            if (result.IsSuccess)
            {
                items.Add(result.Value);
            }
            else
            {
                diagnostics.AddRange(result.Diagnostics);
            }
        }

        if (diagnostics.Count > 0)
        {
            return Result<IReadOnlyList<TCompiledConfig>>.Failure(diagnostics);
        }

        return items;
    }

    private static Dictionary<string, ExpressionSyntax> ExtractSyntaxProperties(ObjectCreationExpressionSyntax objectCreation)
    {
        var properties = new Dictionary<string, ExpressionSyntax>(StringComparer.Ordinal);
        
        foreach (var expression in objectCreation.Initializer?.Expressions ?? [])
        {
            if (expression is AssignmentExpressionSyntax assignment)
            {
                var name = assignment.Left.ToString();
                properties[name] = assignment.Right;
            }
        }
        
        return properties;
    }

    private static ConfigTypeMetadata GetOrCreateMetadata<TCompiledConfig>()
    {
        return MetadataCache.GetOrAdd(typeof(TCompiledConfig), type =>
        {
            var properties = new List<PropertyMetadata>();
            
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanWrite) continue;
                
                // Use property name directly as source property name
                var sourcePropertyName = prop.Name;
                var xmlName = ToKebabCase(prop.Name);
                
                // Check if property is required (has 'required' keyword in C# 11+)
                var isRequired = prop.GetCustomAttribute<System.Runtime.CompilerServices.RequiredMemberAttribute>() != null;
                
                // Determine the property type characteristics
                var propType = prop.PropertyType;
                var (isCollection, collectionElementType, innerType, isExpressionValue) = AnalyzePropertyType(propType);
                
                properties.Add(new PropertyMetadata(
                    prop,
                    sourcePropertyName,
                    xmlName,
                    isRequired,
                    isCollection,
                    innerType,
                    collectionElementType,
                    isExpressionValue));
            }
            
            return new ConfigTypeMetadata(type, properties);
        });
    }

    private static (bool IsCollection, Type? CollectionElementType, Type InnerType, bool IsExpressionValue) AnalyzePropertyType(Type propType)
    {
        // Check for nullable
        var underlyingType = Nullable.GetUnderlyingType(propType);
        var effectiveType = underlyingType ?? propType;
        
        // Check for IReadOnlyList<T>
        if (effectiveType.IsGenericType)
        {
            var genericDef = effectiveType.GetGenericTypeDefinition();
            
            if (genericDef == typeof(IReadOnlyList<>))
            {
                var elementType = effectiveType.GetGenericArguments()[0];
                
                // Check if the element type is ExpressionValue<T>
                if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(ExpressionValue<>))
                {
                    var innerElementType = elementType.GetGenericArguments()[0];
                    // Collection of expression values - IsExpressionValue should be true
                    return (true, innerElementType, innerElementType, true);
                }
                
                // Collection element is a plain type (compiled config or simple type)
                return (true, elementType, elementType, false);
            }
            else if (genericDef == typeof(ExpressionValue<>))
            {
                var innerType = effectiveType.GetGenericArguments()[0];
                return (false, null, innerType, true);
            }
        }
        
        // Not wrapped in ExpressionValue - use the type directly
        return (false, null, effectiveType, false);
    }

    private static Result<object?> ExtractPropertyValue(
        ExpressionSyntax expression,
        ICompilationContext context,
        PropertyMetadata propMeta,
        string policyName)
    {
        // Handle collections: IReadOnlyList<T>
        if (propMeta.IsCollection && propMeta.CollectionElementType != null)
        {
            return ExtractCollectionValue(expression, context, propMeta);
        }

        // Handle union types (abstract classes with nested case classes)
        if (IsUnionType(propMeta.InnerType))
        {
            return ExtractUnionValue(expression, context, propMeta, policyName);
        }
        
        // Handle nested compiled configs (complex types)
        if (IsNestedCompiledConfig(propMeta.InnerType))
        {
            return ExtractNestedConfig(expression, context, propMeta, policyName);
        }
        
        // Handle ExpressionValue<T> - property supports expressions
        if (propMeta.IsExpressionValue)
        {
            return ExtractExpressionValue(expression, context, propMeta.InnerType);
        }
        
        // Handle non-ExpressionValue types - extract constant value directly
        return ExtractConstantValue(expression, context, propMeta.InnerType);
    }

    private static Result<object?> ExtractConstantValue(
        ExpressionSyntax expression,
        ICompilationContext context,
        Type targetType)
    {
        // Use semantic model to get the constant value
        var semanticModel = context.Compilation.GetSemanticModel(expression.SyntaxTree);
        var constantValue = semanticModel.GetConstantValue(expression);
        
        if (constantValue.HasValue)
        {
            // Convert to target type if needed
            var value = constantValue.Value;
            if (value is not null && value.GetType() != targetType)
            {
                try
                {
                    value = Convert.ChangeType(value, targetType);
                }
                catch
                {
                    // Type conversion failed, use as-is
                }
            }
            return Result<object?>.Success(value);
        }
        
        // Try to extract from literal expression
        if (expression is LiteralExpressionSyntax literal)
        {
            var token = literal.Token;
            object? value = token.Value;
            return Result<object?>.Success(value);
        }
        
        // For non-constant expressions that aren't allowed for this property
        return Result<object?>.Failure(Diagnostic.Create(
            CompilationErrors.NotSupportedParameter,
            expression.GetLocation(),
            expression.ToString()));
    }

    private static Result<object?> ExtractExpressionValue(
        ExpressionSyntax expression,
        ICompilationContext context,
        Type innerType)
    {
        // Use reflection to call the generic ProcessToExpressionValue<T>
        var method = typeof(ExpressionProcessor)
            .GetMethod(nameof(ExpressionProcessor.ProcessToExpressionValue))!
            .MakeGenericMethod(innerType);
        
        var result = method.Invoke(null, [expression, context]);
        
        // The result is Result<ExpressionValue<T>>
        var resultType = result!.GetType();
        var isSuccess = (bool)resultType.GetProperty("IsSuccess")!.GetValue(result)!;
        
        if (!isSuccess)
        {
            var diagnostics = (IEnumerable<Diagnostic>)resultType.GetProperty("Diagnostics")!.GetValue(result)!;
            return Result<object?>.Failure(diagnostics.ToList());
        }
        
        var value = resultType.GetProperty("Value")!.GetValue(result);
        return Result<object?>.Success(value);
    }

    private static Result<object?> ExtractCollectionValue(
        ExpressionSyntax expression,
        ICompilationContext context,
        PropertyMetadata propMeta)
    {
        var elementType = propMeta.CollectionElementType!;
        var diagnostics = new List<Diagnostic>();

        IEnumerable<ExpressionSyntax> expressions = expression switch
        {
            ArrayCreationExpressionSyntax array => array.Initializer?.Expressions ?? [],
            ImplicitArrayCreationExpressionSyntax implicitArray => implicitArray.Initializer.Expressions,
            CollectionExpressionSyntax collection => collection.Elements.OfType<ExpressionElementSyntax>().Select(e => e.Expression),
            _ => [expression] // Single value treated as array of one
        };

        // Check if element type is a nested compiled config (complex object like AddressRange, TraceMetadata, etc.)
        if (IsNestedCompiledConfig(elementType))
        {
            return ExtractNestedConfigCollection(expressions, context, propMeta, elementType, diagnostics);
        }

        // If the collection elements support expressions (IsExpressionValue is true),
        // wrap each element in ExpressionValue<T>
        if (propMeta.IsExpressionValue)
        {
            var method = typeof(ExpressionProcessor)
                .GetMethod(nameof(ExpressionProcessor.ProcessToExpressionValue))!
                .MakeGenericMethod(elementType);

            // Create a list of ExpressionValue<T>
            var expressionValueType = typeof(ExpressionValue<>).MakeGenericType(elementType);
            var listType = typeof(List<>).MakeGenericType(expressionValueType);
            var list = Activator.CreateInstance(listType)!;
            var addMethod = listType.GetMethod("Add")!;

            foreach (var itemExpression in expressions)
            {
                var result = method.Invoke(null, [itemExpression, context]);
                var resultType = result!.GetType();
                var isSuccess = (bool)resultType.GetProperty("IsSuccess")!.GetValue(result)!;
                
                if (!isSuccess)
                {
                    var itemDiagnostics = (IEnumerable<Diagnostic>)resultType.GetProperty("Diagnostics")!.GetValue(result)!;
                    diagnostics.AddRange(itemDiagnostics);
                    continue;
                }
                
                var value = resultType.GetProperty("Value")!.GetValue(result);
                addMethod.Invoke(list, [value]);
            }

            if (diagnostics.Count > 0)
            {
                return Result<object?>.Failure(diagnostics);
            }
            
            return Result<object?>.Success(list);
        }
        else
        {
            // Collection of plain values - extract constant values directly
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = Activator.CreateInstance(listType)!;
            var addMethod = listType.GetMethod("Add")!;

            foreach (var itemExpression in expressions)
            {
                var result = ExtractConstantValue(itemExpression, context, elementType);
                if (result.IsFailure)
                {
                    diagnostics.AddRange(result.Diagnostics);
                    continue;
                }
                
                addMethod.Invoke(list, [result.Value]);
            }

            if (diagnostics.Count > 0)
            {
                return Result<object?>.Failure(diagnostics);
            }
            
            return Result<object?>.Success(list);
        }
    }

    private static Result<object?> ExtractNestedConfigCollection(
        IEnumerable<ExpressionSyntax> expressions,
        ICompilationContext context,
        PropertyMetadata propMeta,
        Type elementType,
        List<Diagnostic> diagnostics)
    {
        // For nested configs, we extract each item as a compiled config
        // The property type is IReadOnlyList<TNestedConfig>
        // We create List<TNestedConfig> directly (no ExpressionValue wrapper)
        
        // Get the generated compiled config type for this authoring type
        var compiledConfigType = GetCompiledConfigType(elementType);
        if (compiledConfigType == null)
        {
            // Fallback: try to use the element type directly if it's already a compiled config
            compiledConfigType = elementType;
        }
        
        var listType = typeof(List<>).MakeGenericType(compiledConfigType);
        var list = Activator.CreateInstance(listType)!;
        var addMethod = listType.GetMethod("Add")!;
        
        foreach (var itemExpression in expressions)
        {
            // For abstract base classes, we need to determine the actual type from the source
            var actualConfigType = compiledConfigType;
            if (compiledConfigType.IsAbstract && itemExpression is ObjectCreationExpressionSyntax objectCreation)
            {
                actualConfigType = GetConcreteTypeFromExpression(objectCreation, compiledConfigType);
                if (actualConfigType == null)
                {
                    diagnostics.Add(Diagnostic.Create(
                        CompilationErrors.NotSupportedType,
                        itemExpression.GetLocation(),
                        propMeta.SourcePropertyName,
                        objectCreation.Type.ToString()));
                    continue;
                }
            }
            
            var extractMethod = typeof(CompiledConfigExtractor)
                .GetMethod(nameof(ExtractFromExpression), BindingFlags.Public | BindingFlags.Static)!
                .MakeGenericMethod(actualConfigType);
            
            var result = extractMethod.Invoke(null, [itemExpression, context, propMeta.SourcePropertyName]);
            var resultType = result!.GetType();
            var isSuccess = (bool)resultType.GetProperty("IsSuccess")!.GetValue(result)!;
            
            if (!isSuccess)
            {
                var itemDiagnostics = (IEnumerable<Diagnostic>)resultType.GetProperty("Diagnostics")!.GetValue(result)!;
                diagnostics.AddRange(itemDiagnostics);
                continue;
            }
            
            var extractedConfig = resultType.GetProperty("Value")!.GetValue(result);
            addMethod.Invoke(list, [extractedConfig]);
        }
        
        if (diagnostics.Count > 0)
        {
            return Result<object?>.Failure(diagnostics);
        }
        
        return Result<object?>.Success(list);
    }

    /// <summary>
    /// Gets the concrete derived type from an ObjectCreationExpression when the target type is abstract.
    /// </summary>
    private static Type? GetConcreteTypeFromExpression(ObjectCreationExpressionSyntax objectCreation, Type abstractBaseType)
    {
        var typeName = objectCreation.Type.ToString();
        
        // Remove namespace prefix if present
        var lastDot = typeName.LastIndexOf('.');
        var simpleTypeName = lastDot >= 0 ? typeName.Substring(lastDot + 1) : typeName;
        
        // Remove "Config" suffix if present for matching
        var baseName = simpleTypeName;
        if (baseName.EndsWith("Config", StringComparison.Ordinal))
        {
            baseName = baseName.Substring(0, baseName.Length - 6);
        }
        
        // Find derived types in the same namespace
        var ns = abstractBaseType.Namespace ?? string.Empty;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // First try exact match with "Config" suffix
            var concreteType = assembly.GetType($"{ns}.{simpleTypeName}");
            if (concreteType != null && !concreteType.IsAbstract && abstractBaseType.IsAssignableFrom(concreteType))
            {
                return concreteType;
            }
            
            // Try adding "Config" suffix
            concreteType = assembly.GetType($"{ns}.{baseName}Config");
            if (concreteType != null && !concreteType.IsAbstract && abstractBaseType.IsAssignableFrom(concreteType))
            {
                return concreteType;
            }
        }
        
        return null;
    }

    private static Type? GetCompiledConfigType(Type authoringType)
    {
        // Look for a compiled config in the Compiling.Configs namespace with the same name
        var compiledConfigNamespace = "Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs";
        var configName = authoringType.Name;
        
        // Search in loaded assemblies
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var compiledType = assembly.GetType($"{compiledConfigNamespace}.{configName}");
            if (compiledType != null)
            {
                return compiledType;
            }
        }
        
        return null;
    }

    private static bool IsNestedCompiledConfig(Type type)
    {
        // Skip primitive and simple types
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type.IsEnum)
        {
            return false;
        }

        // Skip union types - they're handled separately
        if (IsUnionType(type))
        {
            return false;
        }
        
        // A nested compiled config is:
        // 1. A class in the Compiling.Configs namespace (generated compiled config)
        // 2. A class/record type from the Authoring namespace (authoring config)
        var ns = type.Namespace ?? string.Empty;
        if (ns.Contains("Compiling.Configs") || ns.Contains("Authoring"))
        {
            return type.IsClass;
        }
        
        return false;
    }

    /// <summary>
    /// Checks if a type is a union type (abstract class with nested case classes ending in "Union").
    /// </summary>
    private static bool IsUnionType(Type type)
    {
        if (!type.IsAbstract || !type.IsClass)
        {
            return false;
        }

        var ns = type.Namespace ?? string.Empty;
        if (!ns.Contains("Compiling.Configs"))
        {
            return false;
        }

        // Union types follow the naming convention "*Union"
        return type.Name.EndsWith("Union", StringComparison.Ordinal);
    }

    /// <summary>
    /// Extracts a union type value by identifying the concrete type and wrapping it in the appropriate case class.
    /// </summary>
    private static Result<object?> ExtractUnionValue(
        ExpressionSyntax expression,
        ICompilationContext context,
        PropertyMetadata propMeta,
        string policyName)
    {
        if (expression is not ObjectCreationExpressionSyntax objectCreation)
        {
            return Result<object?>.Failure(Diagnostic.Create(
                CompilationErrors.PolicyArgumentIsNotAnObjectCreation,
                expression.GetLocation(),
                policyName,
                propMeta.SourcePropertyName));
        }

        // Get the type name from the source code
        var typeName = objectCreation.Type.ToString();
        
        // Remove namespace prefix if present to get just the class name
        var lastDot = typeName.LastIndexOf('.');
        var simpleTypeName = lastDot >= 0 ? typeName.Substring(lastDot + 1) : typeName;

        // Find the corresponding case class in the union type
        var unionType = propMeta.InnerType;
        var caseClass = FindUnionCaseClass(unionType, simpleTypeName);
        
        if (caseClass is null)
        {
            return Result<object?>.Failure(Diagnostic.Create(
                CompilationErrors.NotSupportedType,
                expression.GetLocation(),
                policyName,
                simpleTypeName));
        }

        // Get the compiled config type for this case (the type of the Config property)
        var configProperty = caseClass.GetProperty("Config");
        if (configProperty is null)
        {
            return Result<object?>.Failure(Diagnostic.Create(
                CompilationErrors.NotSupportedType,
                expression.GetLocation(),
                policyName,
                simpleTypeName));
        }

        var compiledConfigType = configProperty.PropertyType;

        // Extract the concrete config
        var extractMethod = typeof(CompiledConfigExtractor)
            .GetMethod(nameof(ExtractFromExpression), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(compiledConfigType);

        var result = extractMethod.Invoke(null, [expression, context, $"{policyName}.{propMeta.SourcePropertyName}"]);
        var resultType = result!.GetType();
        var isSuccess = (bool)resultType.GetProperty("IsSuccess")!.GetValue(result)!;

        if (!isSuccess)
        {
            var diagnostics = (IEnumerable<Diagnostic>)resultType.GetProperty("Diagnostics")!.GetValue(result)!;
            return Result<object?>.Failure(diagnostics.ToList());
        }

        var configValue = resultType.GetProperty("Value")!.GetValue(result);

        // Create the case class instance wrapping the config
        var caseInstance = Activator.CreateInstance(caseClass, configValue);
        return Result<object?>.Success(caseInstance);
    }

    /// <summary>
    /// Finds the case class within a union type that matches the given source type name.
    /// </summary>
    private static Type? FindUnionCaseClass(Type unionType, string sourceTypeName)
    {
        // The source type name is like "BasicAuthenticationConfig"
        // The case class name is like "BasicAuthentication"
        // So we need to remove the "Config" suffix
        var caseName = sourceTypeName;
        if (caseName.EndsWith("Config", StringComparison.Ordinal))
        {
            caseName = caseName.Substring(0, caseName.Length - 6);
        }

        // Look for nested class with this name
        var nestedTypes = unionType.GetNestedTypes(BindingFlags.Public);
        foreach (var nestedType in nestedTypes)
        {
            if (nestedType.Name == caseName && nestedType.IsClass && !nestedType.IsAbstract)
            {
                return nestedType;
            }
        }

        return null;
    }

    private static Result<object?> ExtractNestedConfig(
        ExpressionSyntax expression,
        ICompilationContext context,
        PropertyMetadata propMeta,
        string policyName)
    {
        // Use reflection to call ExtractFromExpression<T>
        var method = typeof(CompiledConfigExtractor)
            .GetMethod(nameof(ExtractFromExpression), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(propMeta.InnerType);
        
        var result = method.Invoke(null, [expression, context, $"{policyName}.{propMeta.SourcePropertyName}"]);
        
        var resultType = result!.GetType();
        var isSuccess = (bool)resultType.GetProperty("IsSuccess")!.GetValue(result)!;
        
        if (!isSuccess)
        {
            var diagnostics = (IEnumerable<Diagnostic>)resultType.GetProperty("Diagnostics")!.GetValue(result)!;
            return Result<object?>.Failure(diagnostics.ToList());
        }
        
        var value = resultType.GetProperty("Value")!.GetValue(result);
        return Result<object?>.Success(value);
    }

    private static string ToKebabCase(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsUpper(c))
            {
                if (i > 0) result.Append('-');
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }

    private sealed record ConfigTypeMetadata(Type Type, List<PropertyMetadata> Properties);
    
    private sealed record PropertyMetadata(
        PropertyInfo PropertyInfo,
        string SourcePropertyName,
        string XmlName,
        bool IsRequired,
        bool IsCollection,
        Type InnerType,
        Type? CollectionElementType,
        bool IsExpressionValue);
}
