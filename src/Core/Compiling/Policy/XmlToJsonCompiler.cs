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
        element.AddOptionalAttribute("consider-accept-header", config.ConsiderAcceptHeader);
        element.AddOptionalAttribute("always-array-child-elements", config.AlwaysArrayChildElements);

        context.AddPolicy(element);
    }
}