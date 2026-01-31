// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Generators;

/// <summary>
/// Source generator that creates compiled config classes from classes marked with [GenerateCompiledConfig].
/// </summary>
[Generator]
public class PolicyConfigGenerator : ISourceGenerator
{
    private const string GenerateCompiledConfigAttributeName = "Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring.GenerateCompiledConfigAttribute";

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new ConfigSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not ConfigSyntaxReceiver receiver)
        {
            return;
        }

        var compilation = context.Compilation;
        var attributeSymbol = compilation.GetTypeByMetadataName(GenerateCompiledConfigAttributeName);
        if (attributeSymbol is null)
        {
            // Attribute not found - probably not referenced
            return;
        }

        var configAnalyzer = new ConfigAnalyzer(compilation);
        var configEmitter = new CompiledConfigEmitter();
        var unionEmitter = new UnionTypeEmitter();

        var configsToGenerate = new List<ConfigInfo>();
        var interfaceImplementations = new Dictionary<INamedTypeSymbol, List<ConfigInfo>>(SymbolEqualityComparer.Default);

        // Analyze all candidate types (classes and records)
        foreach (var typeDeclaration in receiver.CandidateTypes)
        {
            var model = compilation.GetSemanticModel(typeDeclaration.SyntaxTree);
            var typeSymbol = model.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;

            if (typeSymbol is null)
            {
                continue;
            }

            // Check if the type has the [GenerateCompiledConfig] attribute
            if (!HasGenerateCompiledConfigAttribute(typeSymbol, attributeSymbol))
            {
                continue;
            }

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

        // Generate compiled config classes
        foreach (var config in configsToGenerate)
        {
            var source = configEmitter.Emit(config);
            context.AddSource($"{config.ClassName}CompiledConfig.g.cs", SourceText.From(source, Encoding.UTF8));
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

    private static bool HasGenerateCompiledConfigAttribute(INamedTypeSymbol classSymbol, INamedTypeSymbol attributeSymbol)
    {
        return classSymbol.GetAttributes().Any(attr =>
            SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeSymbol));
    }
}

/// <summary>
/// Syntax receiver that collects candidate classes and records for generation.
/// </summary>
internal class ConfigSyntaxReceiver : ISyntaxReceiver
{
    public List<TypeDeclarationSyntax> CandidateTypes { get; } = new List<TypeDeclarationSyntax>();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        // Look for class and record declarations with attributes
        if (syntaxNode is TypeDeclarationSyntax typeDeclaration &&
            (syntaxNode is ClassDeclarationSyntax || syntaxNode is RecordDeclarationSyntax) &&
            typeDeclaration.AttributeLists.Count > 0)
        {
            CandidateTypes.Add(typeDeclaration);
        }
    }
}
