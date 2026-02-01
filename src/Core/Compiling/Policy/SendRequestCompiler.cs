// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class SendRequestCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.SendRequest);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<LocalSendRequestCompiledConfig>(
            node, context, "send-request");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("send-request");

        element.Add(new XAttribute("response-variable-name", config.ResponseVariableName.ToXmlValue()));

        if (config.Mode is { } mode)
        {
            element.Add(new XAttribute("mode", mode.ToXmlValue()));
        }

        if (config.Timeout is { } timeout)
        {
            element.Add(new XAttribute("timeout", timeout.ToXmlValue()));
        }

        if (config.IgnoreError is { } ignoreError)
        {
            element.Add(new XAttribute("ignore-error", ignoreError.ToXmlValue()));
        }

        if (config.Url is { } url)
        {
            element.Add(new XElement("set-url", url.ToXmlValue()));
        }

        if (config.Method is { } method)
        {
            element.Add(new XElement("set-method", method.ToXmlValue()));
        }

        if (config.Headers is { } headers)
        {
            BaseSetHeaderCompiler.HandleHeaders(context, element, headers);
        }

        if (config.Body is { } body)
        {
            SetBodyCompiler.HandleBody(context, element, body);
        }

        if (config.Authentication is { } authentication)
        {
            HandleAuthentication(context, element, authentication);
        }

        if (config.Proxy is { } proxy)
        {
            ProxyCompiler.HandleProxy(context, element, proxy);
        }

        context.AddPolicy(element);
    }

    private void HandleAuthentication(IDocumentCompilationContext context, XElement element,
        InitializerValue authentication)
    {
        var values = authentication.NamedValues;
        if (values is null)
        {
            return;
        }

        switch (authentication.Type)
        {
            case nameof(CertificateAuthenticationConfig):
                AuthenticationCertificateCompiler.HandleCertificateAuthentication(context, element, values,
                    authentication.Node);
                break;
            case nameof(BasicAuthenticationConfig):
                AuthenticationBasicCompiler.HandleBasicAuthentication(context, element, values, authentication.Node);
                break;
            case nameof(ManagedIdentityAuthenticationConfig):
                AuthenticationManagedIdentityCompiler.HandleManagedIdentityAuthentication(context, element, values,
                    authentication.Node);
                break;
            default:
                context.Report(Diagnostic.Create(
                    CompilationErrors.NotSupportedType,
                    authentication.Node.GetLocation(),
                    $"{element.Name}",
                    authentication.Type
                ));
                break;
        }
    }

    private sealed class LocalSendRequestCompiledConfig
    {
        public required ExpressionValue<string> ResponseVariableName { get; init; }
        public ExpressionValue<string>? Mode { get; init; }
        public ExpressionValue<int>? Timeout { get; init; }
        public ExpressionValue<bool>? IgnoreError { get; init; }
        public ExpressionValue<string>? Url { get; init; }
        public ExpressionValue<string>? Method { get; init; }
        public InitializerValue? Headers { get; init; }
        public InitializerValue? Body { get; init; }
        public InitializerValue? Authentication { get; init; }
        public InitializerValue? Proxy { get; init; }
    }
}