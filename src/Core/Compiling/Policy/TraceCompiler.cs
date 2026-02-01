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

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.TraceConfig>(
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
            foreach (var data in metadata)
            {
                var metadataElement = new XElement("metadata");
                metadataElement.Add(new XAttribute("name", data.Name.ToXmlValue()));
                metadataElement.Add(new XAttribute("value", data.Value.ToXmlValue()));
                element.Add(metadataElement);
            }
        }

        context.AddPolicy(element);
    }
}