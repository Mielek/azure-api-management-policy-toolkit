// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class RateLimitCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.RateLimit);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<LocalRateLimitCompiledConfig>(
            node, context, "rate-limit");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("rate-limit");

        element.Add(new XAttribute("calls", config.Calls.ToXmlValue()));
        element.Add(new XAttribute("renewal-period", config.RenewalPeriod.ToXmlValue()));

        if (config.RetryAfterHeaderName is { } retryAfterHeader)
        {
            element.Add(new XAttribute("retry-after-header-name", retryAfterHeader.ToXmlValue()));
        }

        if (config.RetryAfterVariableName is { } retryAfterVar)
        {
            element.Add(new XAttribute("retry-after-variable-name", retryAfterVar.ToXmlValue()));
        }

        if (config.RemainingCallsHeaderName is { } remainingHeader)
        {
            element.Add(new XAttribute("remaining-calls-header-name", remainingHeader.ToXmlValue()));
        }

        if (config.RemainingCallsVariableName is { } remainingVar)
        {
            element.Add(new XAttribute("remaining-calls-variable-name", remainingVar.ToXmlValue()));
        }

        if (config.TotalCallsHeaderName is { } totalHeader)
        {
            element.Add(new XAttribute("total-calls-header-name", totalHeader.ToXmlValue()));
        }

        if (config.Apis is { } apis)
        {
            foreach (var api in apis.UnnamedValues!)
            {
                if (!Handle(context, "api", api, out var apiElement))
                {
                    continue;
                }

                element.Add(apiElement);

                if (api.NamedValues!.TryGetValue(nameof(ApiRateLimit.Operations), out var operations))
                {
                    foreach (var operation in operations.UnnamedValues!)
                    {
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

        var isNameAdded = element.AddAttribute(values, nameof(EntityLimitConfig.Name), "name");
        var isIdAdded = element.AddAttribute(values, nameof(EntityLimitConfig.Id), "id");

        if (!isNameAdded && !isIdAdded)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.AtLeastOneOfTwoShouldBeDefined,
                value.Node.GetLocation(),
                name,
                nameof(EntityLimitConfig.Name),
                nameof(EntityLimitConfig.Id)
            ));
            return false;
        }

        if (!element.AddAttribute(values, nameof(EntityLimitConfig.Calls), "calls"))
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.RequiredParameterNotDefined,
                value.Node.GetLocation(),
                name,
                nameof(EntityLimitConfig.Calls)
            ));
            return false;
        }

        if (!element.AddAttribute(values, nameof(EntityLimitConfig.RenewalPeriod), "renewal-period"))
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.RequiredParameterNotDefined,
                value.Node.GetLocation(),
                name,
                nameof(EntityLimitConfig.RenewalPeriod)
            ));
            return false;
        }

        return true;
    }

    private sealed class LocalRateLimitCompiledConfig
    {
        public required ExpressionValue<int> Calls { get; init; }
        public required ExpressionValue<int> RenewalPeriod { get; init; }
        public ExpressionValue<string>? RetryAfterHeaderName { get; init; }
        public ExpressionValue<string>? RetryAfterVariableName { get; init; }
        public ExpressionValue<string>? RemainingCallsHeaderName { get; init; }
        public ExpressionValue<string>? RemainingCallsVariableName { get; init; }
        public ExpressionValue<string>? TotalCallsHeaderName { get; init; }
        public InitializerValue? Apis { get; init; }
    }
}