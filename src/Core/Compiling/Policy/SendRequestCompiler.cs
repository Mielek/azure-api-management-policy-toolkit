// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class SendRequestCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.SendRequest);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.SendRequestConfig>(
            node, context, "send-request");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("send-request");

        element.Add(new XAttribute("response-variable-name", config.ResponseVariableName));

        element.TryAddAttribute("mode", config.Mode);
        element.TryAddAttribute("timeout", config.Timeout);
        element.TryAddAttribute("ignore-error", config.IgnoreError);

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