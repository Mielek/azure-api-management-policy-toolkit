// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class InvokeDarpBindingCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.InvokeDarpBinding);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.InvokeDarpBindingConfig>(
            node, context, "invoke-darp-binding");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("invoke-darp-binding");

        element.Add(new XAttribute("name", config.Name.ToXmlValue()));

        if (config.Operation is { } operation)
        {
            element.Add(new XAttribute("operation", operation));
        }

        if (config.IgnoreError is { } ignoreError)
        {
            element.Add(new XAttribute("ignore-error", ignoreError.ToXmlValue()));
        }

        if (config.ResponseVariableName is { } responseVariableName)
        {
            element.Add(new XAttribute("response-variable-name", responseVariableName));
        }

        if (config.Timeout is { } timeout)
        {
            element.Add(new XAttribute("timeout", timeout.ToXmlValue()));
        }

        if (config.Template is { } template)
        {
            element.Add(new XAttribute("template", template));
        }

        if (config.ContentType is { } contentType)
        {
            element.Add(new XAttribute("content-type", contentType));
        }

        if (config.MetaData is { } metaData)
        {
            var metadataElement = new XElement("metadata");
            foreach (var item in metaData)
            {
                var itemElement = new XElement("item");
                itemElement.Add(new XAttribute("key", item.Key));
                itemElement.Value = item.Value.ToXmlValue();
                metadataElement.Add(itemElement);
            }
            element.Add(metadataElement);
        }

        if (config.Data is { } data)
        {
            element.Add(new XElement("data", data.ToXmlValue()));
        }

        context.AddPolicy(element);
    }
}