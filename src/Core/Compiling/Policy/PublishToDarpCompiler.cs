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

public class PublishToDarpCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.PublishToDarp);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.PublishToDarpConfig>(
            node, context, "publish-to-darp");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("publish-to-darp");

        element.Add(new XAttribute("topic", config.Topic.ToXmlValue()));
        element.Value = config.Content.ToXmlValue();
        element.AddOptionalAttribute("pub-sub-name", config.PubSubName);
        element.AddOptionalAttribute("ignore-error", config.IgnoreError);
        element.AddOptionalAttribute("response-variable-name", config.ResponseVariableName);
        element.AddOptionalAttribute("timeout", config.Timeout);
        element.AddOptionalAttribute("template", config.Template);
        element.AddOptionalAttribute("content-type", config.ContentType);

        context.AddPolicy(element);
    }
}