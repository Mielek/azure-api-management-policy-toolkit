// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Generators;

/// <summary>
/// Source generator that creates compiled config classes from record/class types
/// in the Authoring namespace of referenced assemblies.
/// </summary>
[Generator]
public class PolicyConfigGenerator : ISourceGenerator
{
    private const string AuthoringNamespace = "Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring";
    private const string ExpressionValueTypeName = "Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring.ExpressionValue`1";

    public void Initialize(GeneratorInitializationContext context)
    {
        // No syntax receiver needed - we scan referenced assemblies
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var compilation = context.Compilation;
        
        // Check if Authoring assembly is referenced by looking for a known type
        var expressionValueType = compilation.GetTypeByMetadataName(ExpressionValueTypeName);
        if (expressionValueType is null)
        {
            // Authoring assembly not referenced
            return;
        }

        var configAnalyzer = new ConfigAnalyzer(compilation);
        var configEmitter = new CompiledConfigEmitter();
        var unionEmitter = new UnionTypeEmitter();

        var configsToGenerate = new List<ConfigInfo>();
        var interfaceImplementations = new Dictionary<INamedTypeSymbol, List<ConfigInfo>>(SymbolEqualityComparer.Default);

        // Find all config types in the Authoring namespace
        var configTypes = FindAuthoringConfigTypes(compilation);

        foreach (var typeSymbol in configTypes)
        {
            var configInfo = configAnalyzer.Analyze(typeSymbol);
            if (configInfo is not null)
            {
                configsToGenerate.Add(configInfo);

                // Track interface implementations for union generation
                foreach (var iface in configInfo.ImplementedInterfaces)
                {
                    if (!interfaceImplementations.TryGetValue(iface, out var implementations))
                    {
                        implementations = new List<ConfigInfo>();
                        interfaceImplementations[iface] = implementations;
                    }
                    implementations.Add(configInfo);
                }
            }
        }

        // Second pass: determine which types have derived classes
        var typesWithDerived = new HashSet<string>();
        foreach (var config in configsToGenerate)
        {
            if (config.BaseTypeName is not null)
            {
                typesWithDerived.Add(config.BaseTypeName);
            }
        }

        // Update configs with HasDerivedClasses flag
        for (int i = 0; i < configsToGenerate.Count; i++)
        {
            var config = configsToGenerate[i];
            if (typesWithDerived.Contains(config.ClassName))
            {
                configsToGenerate[i] = config.WithHasDerivedClasses(true);
            }
        }

        // Generate compiled config classes
        foreach (var config in configsToGenerate)
        {
            var source = configEmitter.Emit(config);
            context.AddSource($"Configs.{config.ClassName}.g.cs", SourceText.From(source, Encoding.UTF8));
        }

        // Generate union types for interfaces with multiple implementations
        foreach (var kvp in interfaceImplementations)
        {
            var interfaceSymbol = kvp.Key;
            var implementations = kvp.Value;
            if (implementations.Count > 1)
            {
                var source = unionEmitter.Emit(interfaceSymbol, implementations);
                context.AddSource($"{interfaceSymbol.Name}Union.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> FindAuthoringConfigTypes(Compilation compilation)
    {
        var results = new List<INamedTypeSymbol>();

        // Only search in referenced assemblies, not the current compilation.
        // This prevents duplicate generation when a project references another project
        // that already has compiled configs generated.
        foreach (var reference in compilation.References)
        {
            var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
            if (assemblySymbol is null)
            {
                continue;
            }

            // Scan the global namespace recursively
            var globalNamespace = assemblySymbol.GlobalNamespace;
            FindConfigTypesInNamespace(globalNamespace, results);
        }

        return results;
    }

    private static void FindConfigTypesInNamespace(INamespaceSymbol namespaceSymbol, List<INamedTypeSymbol> results)
    {
        var ns = namespaceSymbol.ToDisplayString();
        
        // Only process namespaces under Authoring
        if (ns.StartsWith(AuthoringNamespace, StringComparison.Ordinal))
        {
            foreach (var type in namespaceSymbol.GetTypeMembers())
            {
                if (IsConfigType(type))
                {
                    results.Add(type);
                }

                // Check nested types
                foreach (var nestedType in type.GetTypeMembers())
                {
                    if (IsConfigType(nestedType))
                    {
                        results.Add(nestedType);
                    }
                }
            }
        }

        // Recurse into child namespaces
        foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            FindConfigTypesInNamespace(childNamespace, results);
        }
    }

    /// <summary>
    /// Determines if a type is a config type that should have a compiled config generated.
    /// A config type is a public class or record in the Authoring namespace.
    /// </summary>
    private static bool IsConfigType(INamedTypeSymbol type)
    {
        // Must be a public class or record
        if (type.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }

        if (type.TypeKind != TypeKind.Class)
        {
            return false;
        }

        // Skip static classes
        if (type.IsStatic)
        {
            return false;
        }

        // Skip interfaces, enums, delegates
        if (type.TypeKind == TypeKind.Interface || 
            type.TypeKind == TypeKind.Enum || 
            type.TypeKind == TypeKind.Delegate)
        {
            return false;
        }

        // Must be in the Authoring namespace
        var ns = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        if (!ns.StartsWith(AuthoringNamespace, StringComparison.Ordinal))
        {
            return false;
        }

        // Skip attribute classes
        if (type.Name.EndsWith("Attribute", StringComparison.Ordinal))
        {
            return false;
        }

        // Skip special types like ExpressionValue, Expression, etc.
        var name = type.Name;
        if (name == "Expression" || 
            name.StartsWith("ExpressionValue", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }
}