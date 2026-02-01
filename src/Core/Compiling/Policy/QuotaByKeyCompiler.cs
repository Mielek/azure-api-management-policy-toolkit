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

public class QuotaByKeyCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.QuotaByKey);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.QuotaByKeyConfig>(
            node, context, "quota-by-key");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        XElement element = new("quota-by-key");

        element.Add(new XAttribute("counter-key", config.CounterKey.ToXmlValue()));

        bool isCallsAdded = config.Calls is not null;
        bool isBandwidthAdded = config.Bandwidth is not null;

        if (!isCallsAdded && !isBandwidthAdded)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.AtLeastOneOfTwoShouldBeDefined,
                node.GetLocation(),
                "quota-by-key",
                nameof(QuotaByKeyConfig.Calls),
                nameof(QuotaByKeyConfig.Bandwidth)
            ));
            return;
        }

        // Calls and Bandwidth are mutually optional but at least one is required
        element.AddOptionalAttribute("calls", config.Calls);
        element.AddOptionalAttribute("bandwidth", config.Bandwidth);

        element.Add(new XAttribute("renewal-period", config.RenewalPeriod.ToXmlValue()));
        element.AddOptionalAttribute("increment-condition", config.IncrementCondition);
        element.AddOptionalAttribute("increment-count", config.IncrementCount);
        element.AddOptionalAttribute("first-period-start", config.FirstPeriodStart);

        context.AddPolicy(element);
    }
}