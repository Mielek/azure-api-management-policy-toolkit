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

public class RateLimitCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.RateLimit);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.RateLimitConfig>(
            node, context, "rate-limit");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("rate-limit");

        element.Add(new XAttribute("calls", config.Calls));
        element.Add(new XAttribute("renewal-period", config.RenewalPeriod));

        if (config.RetryAfterHeaderName is { } retryAfterHeader)
        {
            element.Add(new XAttribute("retry-after-header-name", retryAfterHeader));
        }

        if (config.RetryAfterVariableName is { } retryAfterVar)
        {
            element.Add(new XAttribute("retry-after-variable-name", retryAfterVar));
        }

        if (config.RemainingCallsHeaderName is { } remainingHeader)
        {
            element.Add(new XAttribute("remaining-calls-header-name", remainingHeader));
        }

        if (config.RemainingCallsVariableName is { } remainingVar)
        {
            element.Add(new XAttribute("remaining-calls-variable-name", remainingVar));
        }

        if (config.TotalCallsHeaderName is { } totalHeader)
        {
            element.Add(new XAttribute("total-calls-header-name", totalHeader));
        }

        if (config.Apis is { } apis)
        {
            foreach (var api in apis)
            {
                var apiElement = new XElement("api");
                
                if (!HandleEntityLimit(context, node, "api", api, apiElement))
                {
                    continue;
                }

                element.Add(apiElement);

                if (api.Operations is { } operations)
                {
                    foreach (var operation in operations)
                    {
                        var operationElement = new XElement("operation");
                        
                        if (HandleEntityLimit(context, node, "operation", operation, operationElement))
                        {
                            apiElement.Add(operationElement);
                        }
                    }
                }
            }
        }

        context.AddPolicy(element);
    }

    private bool HandleEntityLimit(
        IDocumentCompilationContext context,
        InvocationExpressionSyntax node,
        string elementName,
        CompiledConfigs.EntityLimitConfig entity,
        XElement element)
    {
        var isNameAdded = false;
        var isIdAdded = false;

        if (entity.Name is { } name)
        {
            element.Add(new XAttribute("name", name));
            isNameAdded = true;
        }

        if (entity.Id is { } id)
        {
            element.Add(new XAttribute("id", id));
            isIdAdded = true;
        }

        if (!isNameAdded && !isIdAdded)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.AtLeastOneOfTwoShouldBeDefined,
                node.GetLocation(),
                elementName,
                nameof(EntityLimitConfig.Name),
                nameof(EntityLimitConfig.Id)
            ));
            return false;
        }

        element.Add(new XAttribute("calls", entity.Calls));
        element.Add(new XAttribute("renewal-period", entity.RenewalPeriod));

        return true;
    }
}