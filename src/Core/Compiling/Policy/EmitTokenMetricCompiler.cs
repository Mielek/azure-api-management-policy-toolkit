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

public class LlmEmitTokenMetricCompiler()
    : BaseEmitTokenMetricCompiler("llm-emit-token-metric", nameof(IInboundContext.LlmEmitTokenMetric));

public class AzureOpenAiEmitTokenMetricCompiler()
    : BaseEmitTokenMetricCompiler("azure-openai-emit-token-metric", nameof(IInboundContext.AzureOpenAiEmitTokenMetric));

public abstract class BaseEmitTokenMetricCompiler : IMethodPolicyHandler
{
    private readonly string _policyName;
    public string MethodName { get; }

    private sealed class LocalEmitTokenMetricCompiledConfig
    {
        public ExpressionValue<string>? Namespace { get; init; }
        public required InitializerValue Dimensions { get; init; }
    }

    protected BaseEmitTokenMetricCompiler(string policyName, string methodName)
    {
        this._policyName = policyName;
        MethodName = methodName;
    }

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<LocalEmitTokenMetricCompiledConfig>(
            node, context, _policyName);

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement(_policyName);

        if (config.Namespace is { } ns)
        {
            element.Add(new XAttribute("namespace", ns.ToXmlValue()));
        }

        var dimensions = config.Dimensions.UnnamedValues ?? [];
        if (dimensions.Count == 0)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.RequiredParameterIsEmpty,
                config.Dimensions.Node.GetLocation(),
                _policyName,
                nameof(EmitTokenMetricConfig.Dimensions)
            ));
            return;
        }

        foreach (var dimension in dimensions)
        {
            if (dimension.Node is not ExpressionSyntax dimensionExpression)
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.PolicyArgumentIsNotOfRequiredType,
                    dimension.Node.GetLocation(),
                    $"{_policyName}.dimension",
                    nameof(MetricDimensionConfig)
                ));
                continue;
            }

            var dimConfigResult = CompiledConfigExtractor.ExtractFromExpression<CompiledConfigs.MetricDimensionConfig>(
                dimensionExpression, context, $"{_policyName}.dimension");

            if (!dimConfigResult.IsSuccess)
            {
                dimConfigResult.ReportAll(context);
                continue;
            }

            var dimConfig = dimConfigResult.Value;
            var dimensionElement = new XElement("dimension");
            dimensionElement.Add(new XAttribute("name", dimConfig.Name.ToXmlValue()));
            if (dimConfig.Value is { } value)
            {
                dimensionElement.Add(new XAttribute("value", value.ToXmlValue()));
            }
            element.Add(dimensionElement);
        }

        context.AddPolicy(element);
    }
}