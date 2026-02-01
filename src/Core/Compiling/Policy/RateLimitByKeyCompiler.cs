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

        if (config.IncrementCondition is { } incrementCondition)
        {
            element.Add(new XAttribute("increment-condition", incrementCondition.ToXmlValue()));
        }

        if (config.IncrementCount is { } incrementCount)
        {
            element.Add(new XAttribute("increment-count", incrementCount.ToXmlValue()));
        }

        if (config.RetryAfterHeaderName is { } retryAfterHeaderName)
        {
            element.Add(new XAttribute("retry-after-header-name", retryAfterHeaderName.ToXmlValue()));
        }

        if (config.RetryAfterVariableName is { } retryAfterVariableName)
        {
            element.Add(new XAttribute("retry-after-variable-name", retryAfterVariableName.ToXmlValue()));
        }

        if (config.RemainingCallsHeaderName is { } remainingCallsHeaderName)
        {
            element.Add(new XAttribute("remaining-calls-header-name", remainingCallsHeaderName.ToXmlValue()));
        }

        if (config.RemainingCallsVariableName is { } remainingCallsVariableName)
        {
            element.Add(new XAttribute("remaining-calls-variable-name", remainingCallsVariableName.ToXmlValue()));
        }

        if (config.TotalCallsHeaderName is { } totalCallsHeaderName)
        {
            element.Add(new XAttribute("total-calls-header-name", totalCallsHeaderName.ToXmlValue()));
        }

        context.AddPolicy(element);
    }
}