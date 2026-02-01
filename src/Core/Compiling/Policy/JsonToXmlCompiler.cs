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

        element.Add(new XAttribute("apply", config.Apply.ToXmlValue()));

        if (config.ConsiderAcceptHeader is { } considerAcceptHeader)
        {
            element.Add(new XAttribute("consider-accept-header", considerAcceptHeader.ToXmlValue()));
        }

        if (config.ParseDate is { } parseDate)
        {
            element.Add(new XAttribute("parse-date", parseDate.ToXmlValue()));
        }

        if (config.NamespaceSeparator is { } namespaceSeparator)
        {
            element.Add(new XAttribute("namespace-separator", namespaceSeparator.ToXmlValue()));
        }

        if (config.NamespacePrefix is { } namespacePrefix)
        {
            element.Add(new XAttribute("namespace-prefix", namespacePrefix.ToXmlValue()));
        }

        if (config.AttributeBlockName is { } attributeBlockName)
        {
            element.Add(new XAttribute("attribute-block-name", attributeBlockName.ToXmlValue()));
        }

        context.AddPolicy(element);
    }
}