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

public class RateLimitByKeyCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.RateLimitByKey);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.RateLimitByKeyConfig>(
            node, context, "rate-limit-by-key");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("rate-limit-by-key");

        element.Add(new XAttribute("calls", config.Calls.ToXmlValue()));
        element.Add(new XAttribute("renewal-period", config.RenewalPeriod.ToXmlValue()));
        element.Add(new XAttribute("counter-key", config.CounterKey.ToXmlValue()));
        element.AddOptionalAttribute("increment-condition", config.IncrementCondition);
        element.AddOptionalAttribute("increment-count", config.IncrementCount);
        element.AddOptionalAttribute("retry-after-header-name", config.RetryAfterHeaderName);
        element.AddOptionalAttribute("retry-after-variable-name", config.RetryAfterVariableName);
        element.AddOptionalAttribute("remaining-calls-header-name", config.RemainingCallsHeaderName);
        element.AddOptionalAttribute("remaining-calls-variable-name", config.RemainingCallsVariableName);
        element.AddOptionalAttribute("total-calls-header-name", config.TotalCallsHeaderName);

        context.AddPolicy(element);
    }
}