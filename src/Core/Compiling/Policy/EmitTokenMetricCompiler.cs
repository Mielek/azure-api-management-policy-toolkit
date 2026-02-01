// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
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

    protected BaseEmitTokenMetricCompiler(string policyName, string methodName)
    {
        this._policyName = policyName;
        MethodName = methodName;
    }

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.EmitTokenMetricConfig>(
            node, context, _policyName);

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement(_policyName);
        element.AddOptionalAttribute("namespace", config.Namespace);

        foreach (var dimConfig in config.Dimensions)
        {
            var dimensionElement = new XElement("dimension");
            dimensionElement.Add(new XAttribute("name", dimConfig.Name));
            dimensionElement.AddOptionalAttribute("value", dimConfig.Value);
            element.Add(dimensionElement);
        }

        context.AddPolicy(element);
    }
}