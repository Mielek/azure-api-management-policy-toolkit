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
        var element = new XElement("proxy");

        element.Add(new XAttribute("url", config.Url.ToXmlValue()));

        if (config.Username is { } username)
        {
            element.Add(new XAttribute("username", username.ToXmlValue()));
        }

        if (config.Password is { } password)
        {
            element.Add(new XAttribute("password", password.ToXmlValue()));
        }

        context.AddPolicy(element);
    }

    public static void HandleProxy(IDocumentCompilationContext context, XElement element, InitializerValue value)
    {
        if (!value.TryGetValues<ProxyConfig>(out IReadOnlyDictionary<string, InitializerValue>? values))
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.PolicyArgumentIsNotOfRequiredType,
                value.Node.GetLocation(),
                $"{element.Name}.proxy",
                nameof(ProxyConfig)
            ));
            return;
        }

        XElement? proxyElement = HandleProxy(context, value.Node, values);
        if (proxyElement is not null)
        {
            element.Add(proxyElement);
        }
    }

    private static XElement? HandleProxy(IDocumentCompilationContext context, SyntaxNode node,
        IReadOnlyDictionary<string, InitializerValue> values)
    {
        XElement element = new("proxy");
        if (!element.AddAttribute(values, nameof(ProxyConfig.Url), "url"))
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.RequiredParameterNotDefined,
                node.GetLocation(),
                "proxy",
                nameof(ProxyConfig.Url)
            ));
            return null;
        }

        element.AddAttribute(values, nameof(ProxyConfig.Username), "username");
        element.AddAttribute(values, nameof(ProxyConfig.Password), "password");

        return element;
    }
}