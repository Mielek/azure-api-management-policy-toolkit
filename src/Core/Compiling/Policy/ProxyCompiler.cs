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

public class ProxyCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.Proxy);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.ProxyConfig>(
            node, context, "proxy");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = HandleProxy(config);
        context.AddPolicy(element);
    }

    public static XElement HandleProxy(CompiledConfigs.ProxyConfig config)
    {
        XElement element = new("proxy");
        element.AddAttribute("url", config.Url);
        element.AddOptionalAttribute("username", config.Username);
        element.AddOptionalAttribute("password", config.Password);

        return element;
    }
}