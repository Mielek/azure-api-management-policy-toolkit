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

        element.AddAttribute("response-variable-name", config.ResponseVariableName);

        element.AddOptionalAttribute("mode", config.Mode);
        element.AddOptionalAttribute("timeout", config.Timeout);
        element.AddOptionalAttribute("ignore-error", config.IgnoreError);

        element.AddOptionalElement("set-url", config.Url);
        element.AddOptionalElement("set-method", config.Method);

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