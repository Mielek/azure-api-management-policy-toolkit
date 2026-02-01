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

public class IpFilterCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.IpFilter);

    private sealed class LocalIpFilterCompiledConfig
    {
        public required ExpressionValue<string> Action { get; init; }
        public InitializerValue? Addresses { get; init; }
        public InitializerValue? AddressRanges { get; init; }
    }

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<LocalIpFilterCompiledConfig>(
            node, context, "ip-filter");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("ip-filter");

        element.Add(new XAttribute("action", config.Action.ToXmlValue()));

        bool atLeastOneAddress = false;
        if (config.Addresses is { } addresses)
        {
            foreach (var address in addresses.UnnamedValues ?? [])
            {
                if (address.Value is not null)
                {
                    element.Add(new XElement("address", address.Value));
                    atLeastOneAddress = true;
                }
            }
        }

        bool atLeastOneRange = false;
        if (config.AddressRanges is { } addressRanges)
        {
            foreach (var range in addressRanges.UnnamedValues ?? [])
            {
                if (range.Node is not ExpressionSyntax rangeExpression)
                {
                    context.Report(Diagnostic.Create(
                        CompilationErrors.PolicyArgumentIsNotOfRequiredType,
                        range.Node.GetLocation(),
                        "ip-filter.address-range",
                        nameof(AddressRange)
                    ));
                    continue;
                }

                var rangeConfigResult = CompiledConfigExtractor.ExtractFromExpression<CompiledConfigs.AddressRange>(
                    rangeExpression, context, "ip-filter.address-range");

                if (!rangeConfigResult.IsSuccess)
                {
                    rangeConfigResult.ReportAll(context);
                    continue;
                }

                var rangeConfig = rangeConfigResult.Value;
                var rangeElement = new XElement("address-range");
                rangeElement.Add(new XAttribute("from", rangeConfig.From.ToXmlValue()));
                rangeElement.Add(new XAttribute("to", rangeConfig.To.ToXmlValue()));
                element.Add(rangeElement);
                atLeastOneRange = true;
            }
        }

        if (!atLeastOneAddress && !atLeastOneRange)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.AtLeastOneOfTwoShouldBeDefined,
                node.GetLocation(),
                "ip-filter",
                nameof(IpFilterConfig.Addresses),
                nameof(IpFilterConfig.AddressRanges)
            ));
            return;
        }

        context.AddPolicy(element);
    }
}