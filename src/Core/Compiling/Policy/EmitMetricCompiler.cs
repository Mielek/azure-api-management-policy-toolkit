// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class EmitMetricCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.EmitMetric);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.EmitMetricConfig>(
            node, context, "emit-metric");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("emit-metric");

        element.Add(new XAttribute("name", config.Name));

        if (config.Value is { } metricValue)
        {
            element.Add(new XAttribute("value", metricValue.ToXmlValue()));
        }

        if (config.Namespace is { } ns)
        {
            element.Add(new XAttribute("namespace", ns));
        }

        foreach (var dimConfig in config.Dimensions)
        {
            var dimensionElement = new XElement("dimension");
            dimensionElement.Add(new XAttribute("name", dimConfig.Name));
            if (dimConfig.Value is { } value)
            {
                dimensionElement.Add(new XAttribute("value", value.ToXmlValue()));
            }
            element.Add(dimensionElement);
        }

        context.AddPolicy(element);
    }
}