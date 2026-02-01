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

public class CacheLookupValueCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.CacheLookupValue);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.CacheLookupValueConfig>(
            node, context, "cache-lookup-value");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("cache-lookup-value");

        element.AddAttribute("key", config.Key);
        element.AddAttribute("variable-name", config.VariableName);
        element.AddOptionalAttribute("caching-type", config.CachingType);
        element.AddOptionalAttribute("default-value", config.DefaultValue);

        context.AddPolicy(element);
    }
}