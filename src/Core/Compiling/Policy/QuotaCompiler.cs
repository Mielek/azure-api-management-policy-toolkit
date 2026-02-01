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

public class QuotaCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.Quota);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.QuotaConfig>(
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
            element.Add(new XAttribute("calls", calls));
            isCallsAdded = true;
        }

        if (config.Bandwidth is { } bandwidth)
        {
            element.Add(new XAttribute("bandwidth", bandwidth));
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

        element.Add(new XAttribute("renewal-period", config.RenewalPeriod));

        if (config.Apis is { } apis)
        {
            foreach (var api in apis)
            {
                var apiElement = new XElement("api");
                
                if (!HandleEntityQuota(context, node, "api", api, apiElement))
                {
                    continue;
                }

                element.Add(apiElement);

                if (api.Operations is { } operations)
                {
                    foreach (var operation in operations)
                    {
                        var operationElement = new XElement("operation");
                        
                        if (HandleEntityQuota(context, node, "operation", operation, operationElement))
                        {
                            apiElement.Add(operationElement);
                        }
                    }
                }
            }
        }

        context.AddPolicy(element);
    }

    private bool HandleEntityQuota(
        IDocumentCompilationContext context, 
        InvocationExpressionSyntax node,
        string elementName, 
        CompiledConfigs.EntityQuotaConfig entity, 
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
                nameof(EntityQuotaConfig.Name),
                nameof(EntityQuotaConfig.Id)
            ));
            return false;
        }

        var isCallsAdded = false;
        var isBandwidthAdded = false;

        if (entity.Calls is { } calls)
        {
            element.Add(new XAttribute("calls", calls));
            isCallsAdded = true;
        }

        if (entity.Bandwidth is { } bandwidth)
        {
            element.Add(new XAttribute("bandwidth", bandwidth));
            isBandwidthAdded = true;
        }

        if (!isCallsAdded && !isBandwidthAdded)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.AtLeastOneOfTwoShouldBeDefined,
                node.GetLocation(),
                elementName,
                nameof(EntityQuotaConfig.Calls),
                nameof(EntityQuotaConfig.Bandwidth)
            ));
            return false;
        }

        return true;
    }
}