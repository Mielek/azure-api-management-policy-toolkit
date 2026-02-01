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
        element.AddOptionalAttribute("retry-after-header-name", config.RetryAfterHeaderName);
        element.AddOptionalAttribute("retry-after-variable-name", config.RetryAfterVariableName);
        element.AddOptionalAttribute("remaining-calls-header-name", config.RemainingCallsHeaderName);
        element.AddOptionalAttribute("remaining-calls-variable-name", config.RemainingCallsVariableName);
        element.AddOptionalAttribute("total-calls-header-name", config.TotalCallsHeaderName);

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
        var isNameAdded = element.AddOptionalAttribute("name", entity.Name);
        var isIdAdded = element.AddOptionalAttribute("id", entity.Id);

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