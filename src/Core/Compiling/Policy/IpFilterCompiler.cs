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

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.IpFilterConfig>(
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
            foreach (var address in addresses)
            {
                element.Add(new XElement("address", address.ToXmlValue()));
                atLeastOneAddress = true;
            }
        }

        bool atLeastOneRange = false;
        if (config.AddressRanges is { } addressRanges)
        {
            foreach (var rangeConfig in addressRanges)
            {
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