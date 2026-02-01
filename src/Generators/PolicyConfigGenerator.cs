// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Generators;

/// <summary>
/// Incremental source generator that creates compiled config classes with strongly-typed
/// Extract() methods from record/class types in the Authoring namespace.
/// </summary>
[Generator]
public class PolicyConfigGenerator : IIncrementalGenerator
{
    private const string AuthoringNamespace = "Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring";
    private const string ExpressionValueTypeName = "Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring.ExpressionValue`1";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Create a pipeline that finds all config types in referenced assemblies
        var configTypes = context.CompilationProvider
            .SelectMany((compilation, ct) =>
            {
                var expressionValueType = compilation.GetTypeByMetadataName(ExpressionValueTypeName);
                if (expressionValueType is null)
                {
                    return ImmutableArray<INamedTypeSymbol>.Empty;
                }
                return FindAuthoringConfigTypes(compilation).ToImmutableArray();
            });

        // Combine all config types into a single collection for processing
        var allConfigs = configTypes.Collect();

        // Register the source output
        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(allConfigs),
            (ctx, source) => Execute(ctx, source.Left, source.Right));
    }

    private void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<INamedTypeSymbol> configTypes)
    {
        if (configTypes.IsEmpty)
        {
            return;
        }

        var configAnalyzer = new ConfigAnalyzer(compilation);
        var configEmitter = new CompiledConfigEmitter();
        var extractorEmitter = new ConfigExtractorEmitter();
        var unionEmitter = new UnionTypeEmitter();

        var configsToGenerate = new List<ConfigInfo>();
        var interfaceImplementations = new Dictionary<INamedTypeSymbol, List<ConfigInfo>>(SymbolEqualityComparer.Default);

        foreach (var typeSymbol in configTypes)
        {
            var configInfo = configAnalyzer.Analyze(typeSymbol);
            if (configInfo is not null)
            {
                configsToGenerate.Add(configInfo);

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

        // Determine which types have derived classes
        var typesWithDerived = new HashSet<string>();
        foreach (var config in configsToGenerate)
        {
            if (config.BaseTypeName is not null)
            {
                typesWithDerived.Add(config.BaseTypeName);
            }
        }

        // Update configs with HasDerivedClasses flag and build lookup
        var configLookup = new Dictionary<string, ConfigInfo>();
        for (int i = 0; i < configsToGenerate.Count; i++)
        {
            var config = configsToGenerate[i];
            if (typesWithDerived.Contains(config.ClassName))
            {
                config = config.WithHasDerivedClasses(true);
                configsToGenerate[i] = config;
            }
            configLookup[config.ClassName] = config;
        }

        // Build interface implementations lookup for extractor generation
        var interfaceImplLookup = interfaceImplementations
            .ToDictionary(
                kvp => kvp.Key.Name,
                kvp => kvp.Value,
                StringComparer.Ordinal);

        // Generate compiled config classes with Extract() methods
        foreach (var config in configsToGenerate)
        {
            var source = configEmitter.Emit(config);
            var extractorSource = extractorEmitter.Emit(config, configLookup, interfaceImplLookup);
            context.AddSource($"Configs.{config.ClassName}.g.cs", SourceText.From(source, Encoding.UTF8));
            context.AddSource($"Extractors.{config.ClassName}.g.cs", SourceText.From(extractorSource, Encoding.UTF8));
        }

        // Generate union types with ExtractUnion() methods
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

        foreach (var reference in compilation.References)
        {
            var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
            if (assemblySymbol is null)
            {
                continue;
            }

            FindConfigTypesInNamespace(assemblySymbol.GlobalNamespace, results);
        }

        return results;
    }

    private static void FindConfigTypesInNamespace(INamespaceSymbol namespaceSymbol, List<INamedTypeSymbol> results)
    {
        var ns = namespaceSymbol.ToDisplayString();

        if (ns.StartsWith(AuthoringNamespace, StringComparison.Ordinal))
        {
            foreach (var type in namespaceSymbol.GetTypeMembers())
            {
                if (IsConfigType(type))
                {
                    results.Add(type);
                }

                foreach (var nestedType in type.GetTypeMembers())
                {
                    if (IsConfigType(nestedType))
                    {
                        results.Add(nestedType);
                    }
                }
            }
        }

        foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            FindConfigTypesInNamespace(childNamespace, results);
        }
    }

    private static bool IsConfigType(INamedTypeSymbol type)
    {
        if (type.DeclaredAccessibility != Accessibility.Public)
            return false;

        if (type.TypeKind != TypeKind.Class)
            return false;

        if (type.IsStatic)
            return false;

        var ns = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        if (!ns.StartsWith(AuthoringNamespace, StringComparison.Ordinal))
            return false;

        if (type.Name.EndsWith("Attribute", StringComparison.Ordinal))
            return false;

        var name = type.Name;
        if (name == "Expression" || name.StartsWith("ExpressionValue", StringComparison.Ordinal))
            return false;

        return true;
    }
}