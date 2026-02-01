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

        if (config.PubSubName is { } pubSubName)
        {
            element.Add(new XAttribute("pub-sub-name", pubSubName.ToXmlValue()));
        }

        if (config.IgnoreError is { } ignoreError)
        {
            element.Add(new XAttribute("ignore-error", ignoreError.ToXmlValue()));
        }

        if (config.ResponseVariableName is { } responseVariableName)
        {
            element.Add(new XAttribute("response-variable-name", responseVariableName.ToXmlValue()));
        }

        if (config.Timeout is { } timeout)
        {
            element.Add(new XAttribute("timeout", timeout.ToXmlValue()));
        }

        if (config.Template is { } template)
        {
            element.Add(new XAttribute("template", template.ToXmlValue()));
        }

        if (config.ContentType is { } contentType)
        {
            element.Add(new XAttribute("content-type", contentType.ToXmlValue()));
        }

        context.AddPolicy(element);
    }
}