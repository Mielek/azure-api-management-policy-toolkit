// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Reflection;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling;

/// <summary>
/// Extracts strongly-typed compiled config objects from policy method invocations.
/// Auto-discovers properties using reflection on the compiled config type with [ConfigProperty] attributes.
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
                
                // Get property metadata from attribute or use property name directly
                var configAttr = prop.GetCustomAttribute<ConfigPropertyAttribute>();
                var sourcePropertyName = configAttr?.SourcePropertyName ?? prop.Name;
                var xmlName = configAttr?.XmlName ?? ToKebabCase(prop.Name);
                
                // Check if property is required (has 'required' keyword in C# 11+)
                var isRequired = prop.GetCustomAttribute<System.Runtime.CompilerServices.RequiredMemberAttribute>() != null;
                
                // Determine the property type characteristics
                var propType = prop.PropertyType;
                var (isCollection, collectionElementType, innerType) = AnalyzePropertyType(propType);
                
                properties.Add(new PropertyMetadata(
                    prop,
                    sourcePropertyName,
                    xmlName,
                    isRequired,
                    isCollection,
                    innerType,
                    collectionElementType));
            }
            
            return new ConfigTypeMetadata(type, properties);
        });
    }

    private static (bool IsCollection, Type? CollectionElementType, Type InnerType) AnalyzePropertyType(Type propType)
    {
        // Check for nullable
        var underlyingType = Nullable.GetUnderlyingType(propType);
        var effectiveType = underlyingType ?? propType;
        
        // Check for IReadOnlyList<ExpressionValue<T>>
        if (effectiveType.IsGenericType)
        {
            var genericDef = effectiveType.GetGenericTypeDefinition();
            
            if (genericDef == typeof(IReadOnlyList<>))
            {
                var elementType = effectiveType.GetGenericArguments()[0];
                if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(ExpressionValue<>))
                {
                    var innerType = elementType.GetGenericArguments()[0];
                    return (true, innerType, innerType);
                }
            }
            else if (genericDef == typeof(ExpressionValue<>))
            {
                var innerType = effectiveType.GetGenericArguments()[0];
                return (false, null, innerType);
            }
        }
        
        return (false, null, effectiveType);
    }

    private static Result<object?> ExtractPropertyValue(
        ExpressionSyntax expression,
        ICompilationContext context,
        PropertyMetadata propMeta,
        string policyName)
    {
        // Handle InitializerValue directly (for legacy compatibility with manual processing)
        if (propMeta.InnerType == typeof(InitializerValue))
        {
            var result = ExpressionProcessor.ProcessToInitializerValue(expression, context);
            if (result.IsFailure)
            {
                return Result<object?>.Failure(result.Diagnostics.ToList());
            }
            return Result<object?>.Success(result.Value);
        }
        
        // Handle collections: IReadOnlyList<ExpressionValue<T>>
        if (propMeta.IsCollection && propMeta.CollectionElementType != null)
        {
            return ExtractCollectionValue(expression, context, propMeta);
        }
        
        // Handle nested compiled configs (complex types that are classes with ConfigProperty attributes)
        if (IsNestedCompiledConfig(propMeta.InnerType))
        {
            return ExtractNestedConfig(expression, context, propMeta, policyName);
        }
        
        // Handle ExpressionValue<T> or Nullable<ExpressionValue<T>>
        return ExtractExpressionValue(expression, context, propMeta.InnerType);
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

        var method = typeof(ExpressionProcessor)
            .GetMethod(nameof(ExpressionProcessor.ProcessToExpressionValue))!
            .MakeGenericMethod(elementType);

        // Create a list dynamically
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

    private static bool IsNestedCompiledConfig(Type type)
    {
        // A nested compiled config is a class that has the [SourceConfigType] attribute
        // or any property with [ConfigProperty] attribute
        return type.GetCustomAttribute<SourceConfigTypeAttribute>() != null
            || type.GetProperties().Any(p => p.GetCustomAttribute<ConfigPropertyAttribute>() != null);
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
        Type? CollectionElementType);
}
