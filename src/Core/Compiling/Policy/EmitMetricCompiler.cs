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

        element.AddAttribute("name", config.Name);
        element.AddOptionalAttribute("value", config.Value);
        element.AddOptionalAttribute("namespace", config.Namespace);

        foreach (var dimConfig in config.Dimensions)
        {
            var dimensionElement = new XElement("dimension");
            dimensionElement.AddAttribute("name", dimConfig.Name);
            dimensionElement.AddOptionalAttribute("value", dimConfig.Value);
            element.Add(dimensionElement);
        }

        context.AddPolicy(element);
    }
}