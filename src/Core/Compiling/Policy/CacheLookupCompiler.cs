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

        element.AddAttribute("vary-by-developer", config.VaryByDeveloper);
        element.AddAttribute("vary-by-developer-groups", config.VaryByDeveloperGroups);
        element.AddOptionalAttribute("caching-type", config.CachingType);
        element.AddOptionalAttribute("downstream-caching-type", config.DownstreamCachingType);
        element.AddOptionalAttribute("must-revalidate", config.MustRevalidate);
        element.AddOptionalAttribute("allow-private-response-caching", config.AllowPrivateResponseCaching);

        if (config.VaryByHeaders is { } varyByHeaders)
        {
            foreach (var header in varyByHeaders)
            {
                element.AddElement("vary-by-header", header);
            }
        }

        if (config.VaryByQueryParameters is { } varyByQueryParameters)
        {
            foreach (var queryParam in varyByQueryParameters)
            {
                element.AddElement("vary-by-query-parameter", queryParam);
            }
        }

        context.AddPolicy(element);
    }
}