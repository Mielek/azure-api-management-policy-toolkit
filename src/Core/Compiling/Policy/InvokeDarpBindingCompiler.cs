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

public class InvokeDarpBindingCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.InvokeDarpBinding);

    private sealed class LocalInvokeDarpBindingCompiledConfig
    {
        public required ExpressionValue<string> Name { get; init; }
        public ExpressionValue<string>? Operation { get; init; }
        public ExpressionValue<bool>? IgnoreError { get; init; }
        public ExpressionValue<string>? ResponseVariableName { get; init; }
        public ExpressionValue<int>? Timeout { get; init; }
        public ExpressionValue<string>? Template { get; init; }
        public ExpressionValue<string>? ContentType { get; init; }
        public InitializerValue? MetaData { get; init; }
        public ExpressionValue<string>? Data { get; init; }
    }

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<LocalInvokeDarpBindingCompiledConfig>(
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
            element.Add(new XAttribute("operation", operation.ToXmlValue()));
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

        if (config.MetaData is { } metaData)
        {
            HandleMetaData(context, metaData, element);
        }

        if (config.Data is { } data)
        {
            element.Add(new XElement("data", data.ToXmlValue()));
        }

        context.AddPolicy(element);
    }

    private static void HandleMetaData(IDocumentCompilationContext context, InitializerValue metaDataValue,
        XElement parentElement)
    {
        if (metaDataValue.UnnamedValues is null || metaDataValue.UnnamedValues.Count == 0)
        {
            return;
        }

        var element = new XElement("metadata");

        foreach (var item in metaDataValue.UnnamedValues)
        {
            if (item.Node is not ExpressionSyntax itemExpression)
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.PolicyArgumentIsNotOfRequiredType,
                    item.Node.GetLocation(),
                    "invoke-darp-binding.metadata",
                    nameof(DarpMetaData)
                ));
                continue;
            }

            var itemConfigResult = CompiledConfigExtractor.ExtractFromExpression<CompiledConfigs.DarpMetaData>(
                itemExpression, context, "invoke-darp-binding.metadata.item");

            if (!itemConfigResult.IsSuccess)
            {
                itemConfigResult.ReportAll(context);
                continue;
            }

            var itemConfig = itemConfigResult.Value;
            var metaDataElement = new XElement("item");
            metaDataElement.Add(new XAttribute("key", itemConfig.Key.ToXmlValue()));
            metaDataElement.Value = itemConfig.Value.ToXmlValue();
            element.Add(metaDataElement);
        }

        parentElement.Add(element);
    }
}