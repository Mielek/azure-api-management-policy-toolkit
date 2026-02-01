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

public class JsonToXmlCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.JsonToXml);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.JsonToXmlConfig>(
            node, context, "json-to-xml");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("json-to-xml");

        element.AddAttribute("apply", config.Apply);
        element.AddOptionalAttribute("consider-accept-header", config.ConsiderAcceptHeader);
        element.AddOptionalAttribute("parse-date", config.ParseDate);
        element.AddOptionalAttribute("namespace-separator", config.NamespaceSeparator);
        element.AddOptionalAttribute("namespace-prefix", config.NamespacePrefix);
        element.AddOptionalAttribute("attribute-block-name", config.AttributeBlockName);

        context.AddPolicy(element);
    }
}