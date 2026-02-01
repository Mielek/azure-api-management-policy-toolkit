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
        element.Add(new XAttribute("url", config.Url.ToXmlValue()));

        if (config.Username is { } username)
        {
            element.Add(new XAttribute("username", username.ToXmlValue()));
        }

        if (config.Password is { } password)
        {
            element.Add(new XAttribute("password", password.ToXmlValue()));
        }

        return element;
    }
}