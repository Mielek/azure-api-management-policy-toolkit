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

        element.AddAttribute("name", config.Name);
        element.AddOptionalAttribute("operation", config.Operation);
        element.AddOptionalAttribute("ignore-error", config.IgnoreError);
        element.AddOptionalAttribute("response-variable-name", config.ResponseVariableName);
        element.AddOptionalAttribute("timeout", config.Timeout);
        element.AddOptionalAttribute("template", config.Template);
        element.AddOptionalAttribute("content-type", config.ContentType);

        if (config.MetaData is { } metaData)
        {
            var metadataElement = new XElement("metadata");
            foreach (var item in metaData)
            {
                var itemElement = new XElement("item");
                itemElement.AddAttribute("key", item.Key);
                itemElement.Value = item.Value.ToXmlValue();
                metadataElement.Add(itemElement);
            }
            element.Add(metadataElement);
        }

        element.AddOptionalElement("data", config.Data);

        context.AddPolicy(element);
    }
}