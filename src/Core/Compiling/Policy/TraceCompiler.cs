// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class TraceCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.Trace);

    private sealed class LocalTraceCompiledConfig
    {
        public required ExpressionValue<string> Source { get; init; }
        public required ExpressionValue<string> Message { get; init; }
        public ExpressionValue<string>? Severity { get; init; }
        public InitializerValue? Metadata { get; init; }
    }

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<LocalTraceCompiledConfig>(
            node, context, "trace");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("trace");

        element.Add(new XAttribute("source", config.Source.ToXmlValue()));
        element.Add(new XElement("message", config.Message.ToXmlValue()));

        if (config.Severity is { } severity)
        {
            element.Add(new XAttribute("severity", severity.ToXmlValue()));
        }

        if (config.Metadata is { } metadata)
        {
            HandleMetadata(context, metadata, element);
        }

        context.AddPolicy(element);
    }

    private static void HandleMetadata(IDocumentCompilationContext context, InitializerValue metadataValue,
        XElement parentElement)
    {
        foreach (var dataValue in metadataValue.UnnamedValues ?? [])
        {
            if (dataValue.Node is not ExpressionSyntax dataExpression)
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.PolicyArgumentIsNotOfRequiredType,
                    dataValue.Node.GetLocation(),
                    "trace.metadata",
                    nameof(TraceMetadata)
                ));
                continue;
            }

            var configResult = CompiledConfigExtractor.ExtractFromExpression<CompiledConfigs.TraceMetadata>(
                dataExpression, context, "trace.metadata");

            if (!configResult.IsSuccess)
            {
                configResult.ReportAll(context);
                continue;
            }

            var config = configResult.Value;
            var metadataElement = new XElement("metadata");
            metadataElement.Add(new XAttribute("name", config.Name.ToXmlValue()));
            metadataElement.Add(new XAttribute("value", config.Value.ToXmlValue()));
            parentElement.Add(metadataElement);
        }
    }
}