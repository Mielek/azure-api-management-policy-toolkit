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

public class EmitMetricCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.EmitMetric);

    private sealed class LocalEmitMetricCompiledConfig
    {
        public required ExpressionValue<string> Name { get; init; }
        public ExpressionValue<double>? Value { get; init; }
        public ExpressionValue<string>? Namespace { get; init; }
        public required InitializerValue Dimensions { get; init; }
    }

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<LocalEmitMetricCompiledConfig>(
            node, context, "emit-metric");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("emit-metric");

        element.Add(new XAttribute("name", config.Name.ToXmlValue()));

        if (config.Value is { } metricValue)
        {
            element.Add(new XAttribute("value", metricValue.ToXmlValue()));
        }

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
                "emit-metric",
                nameof(EmitMetricConfig.Dimensions)
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
                    "emit-metric.dimension",
                    nameof(MetricDimensionConfig)
                ));
                continue;
            }

            var dimConfigResult = CompiledConfigExtractor.ExtractFromExpression<CompiledConfigs.MetricDimensionConfig>(
                dimensionExpression, context, "emit-metric.dimension");

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