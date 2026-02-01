// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class QuotaCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.Quota);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<LocalQuotaCompiledConfig>(
            node, context, "quota");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("quota");

        var isCallsAdded = false;
        var isBandwidthAdded = false;

        if (config.Calls is { } calls)
        {
            element.Add(new XAttribute("calls", calls.ToXmlValue()));
            isCallsAdded = true;
        }

        if (config.Bandwidth is { } bandwidth)
        {
            element.Add(new XAttribute("bandwidth", bandwidth.ToXmlValue()));
            isBandwidthAdded = true;
        }

        if (!isCallsAdded && !isBandwidthAdded)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.AtLeastOneOfTwoShouldBeDefined,
                node.GetLocation(),
                "quota",
                nameof(QuotaConfig.Calls),
                nameof(QuotaConfig.Bandwidth)
            ));
            return;
        }

        element.Add(new XAttribute("renewal-period", config.RenewalPeriod.ToXmlValue()));

        if (config.Apis is { } apis)
        {
            foreach (var api in apis.UnnamedValues!)
            {
                if (api.Type != nameof(ApiQuota))
                {
                    context.Report(Diagnostic.Create(
                        CompilationErrors.PolicyArgumentIsNotOfRequiredType,
                        api.Node.GetLocation(),
                        "quota.api",
                        nameof(ApiQuota)
                    ));
                    continue;
                }

                if (!Handle(context, "api", api, out var apiElement))
                {
                    continue;
                }

                element.Add(apiElement);

                if (api.NamedValues!.TryGetValue(nameof(ApiQuota.Operations), out var operations))
                {
                    foreach (var operation in operations.UnnamedValues!)
                    {
                        if (operation.Type != nameof(OperationQuota))
                        {
                            context.Report(Diagnostic.Create(
                                CompilationErrors.PolicyArgumentIsNotOfRequiredType,
                                operation.Node.GetLocation(),
                                "quota.api.operation",
                                nameof(OperationQuota)
                            ));
                            continue;
                        }

                        if (Handle(context, "operation", operation, out var operationElement))
                        {
                            apiElement.Add(operationElement);
                        }
                    }
                }
            }
        }

        context.AddPolicy(element);
    }

    private bool Handle(IDocumentCompilationContext context, string name, InitializerValue value, out XElement element)
    {
        element = new XElement(name);
        var values = value.NamedValues!;

        var isNameAdded = element.AddAttribute(values, nameof(EntityQuotaConfig.Name), "name");
        var isIdAdded = element.AddAttribute(values, nameof(EntityQuotaConfig.Id), "id");

        if (!isNameAdded && !isIdAdded)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.AtLeastOneOfTwoShouldBeDefined,
                value.Node.GetLocation(),
                name,
                nameof(EntityQuotaConfig.Name),
                nameof(EntityQuotaConfig.Id)
            ));
            return false;
        }

        var isCallsAdded = element.AddAttribute(values, nameof(EntityQuotaConfig.Calls), "calls");
        var isBandwidthAdded = element.AddAttribute(values, nameof(EntityQuotaConfig.Bandwidth), "bandwidth");

        if (!isCallsAdded && !isBandwidthAdded)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.AtLeastOneOfTwoShouldBeDefined,
                value.Node.GetLocation(),
                name,
                nameof(EntityQuotaConfig.Calls),
                nameof(EntityQuotaConfig.Bandwidth)
            ));
            return false;
        }

        return true;
    }

    private sealed class LocalQuotaCompiledConfig
    {
        public ExpressionValue<int>? Calls { get; init; }
        public ExpressionValue<int>? Bandwidth { get; init; }
        public required ExpressionValue<int> RenewalPeriod { get; init; }
        public InitializerValue? Apis { get; init; }
    }
}