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

public class CacheStoreValueCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.CacheStoreValue);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.CacheStoreValueConfig>(
            node, context, "cache-store-value");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("cache-store-value");

        element.AddAttribute("key", config.Key);
        element.AddAttribute("value", config.Value);
        element.AddAttribute("duration", config.Duration);
        element.AddOptionalAttribute("caching-type", config.CachingType);

        context.AddPolicy(element);
    }
}