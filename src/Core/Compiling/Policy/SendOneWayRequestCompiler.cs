// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class SendOneWayRequestCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.SendOneWayRequest);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.SendOneWayRequestConfig>(
            node, context, "send-one-way-request");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        XElement element = new("send-one-way-request");

        element.TryAddAttribute("mode", config.Mode);
        element.TryAddAttribute("timeout", config.Timeout);

        if (config.Url is { } url)
        {
            element.Add(new XElement("set-url", url.ToXmlValue()));
        }

        if (config.Method is { } method)
        {
            element.Add(new XElement("set-method", method));
        }

        if (config.Headers is { } headers)
        {
            BaseSetHeaderCompiler.HandleHeaders(element, headers);
        }

        if (config.Body is { } body)
        {
            SetBodyCompiler.HandleBody(element, body);
        }

        if (config.Authentication is { } authentication)
        {
            HandleAuthentication(element, authentication);
        }

        if (config.Proxy is { } proxy)
        {
            element.Add(ProxyCompiler.HandleProxy(proxy));
        }

        context.AddPolicy(element);
    }

    private void HandleAuthentication(XElement element, CompiledConfigs.AuthenticationConfigUnion authentication)
    {
        _ = authentication.Match(
            basic =>
            {
                AuthenticationBasicCompiler.HandleBasicAuthentication(element, basic);
                return 0;
            },
            certificate =>
            {
                AuthenticationCertificateCompiler.HandleCertificateAuthentication(element, certificate);
                return 0;
            },
            managedIdentity =>
            {
                AuthenticationManagedIdentityCompiler.HandleManagedIdentityAuthentication(element, managedIdentity);
                return 0;
            }
        );
    }
}