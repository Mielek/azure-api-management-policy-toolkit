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

public class CacheLookupCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.CacheLookup);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.CacheLookupConfig>(
            node, context, "cache-lookup");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("cache-lookup");

        element.Add(new XAttribute("vary-by-developer", config.VaryByDeveloper.ToXmlValue()));
        element.Add(new XAttribute("vary-by-developer-groups", config.VaryByDeveloperGroups.ToXmlValue()));
        element.AddOptionalAttribute("caching-type", config.CachingType);
        element.AddOptionalAttribute("downstream-caching-type", config.DownstreamCachingType);
        element.AddOptionalAttribute("must-revalidate", config.MustRevalidate);
        element.AddOptionalAttribute("allow-private-response-caching", config.AllowPrivateResponseCaching);

        if (config.VaryByHeaders is { } varyByHeaders)
        {
            foreach (var header in varyByHeaders)
            {
                element.Add(new XElement("vary-by-header", header.ToXmlValue()));
            }
        }

        if (config.VaryByQueryParameters is { } varyByQueryParameters)
        {
            foreach (var queryParam in varyByQueryParameters)
            {
                element.Add(new XElement("vary-by-query-parameter", queryParam.ToXmlValue()));
            }
        }

        context.AddPolicy(element);
    }
}