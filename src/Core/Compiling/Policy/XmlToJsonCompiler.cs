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

public class XmlToJsonCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.XmlToJson);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.XmlToJsonConfig>(
            node, context, "xml-to-json");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("xml-to-json");

        element.Add(new XAttribute("kind", config.Kind.ToXmlValue()));
        element.Add(new XAttribute("apply", config.Apply.ToXmlValue()));

        if (config.ConsiderAcceptHeader is { } considerAcceptHeader)
        {
            element.Add(new XAttribute("consider-accept-header", considerAcceptHeader.ToXmlValue()));
        }

        if (config.AlwaysArrayChildElements is { } alwaysArrayChildElements)
        {
            element.Add(new XAttribute("always-array-child-elements", alwaysArrayChildElements.ToXmlValue()));
        }

        context.AddPolicy(element);
    }
}