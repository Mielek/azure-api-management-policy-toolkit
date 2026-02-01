// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class SendOneWayRequestCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.SendOneWayRequest);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<LocalSendOneWayRequestCompiledConfig>(
            node, context, "send-one-way-request");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        XElement element = new("send-one-way-request");

        if (config.Mode is { } mode)
        {
            element.Add(new XAttribute("mode", mode.ToXmlValue()));
        }

        if (config.Timeout is { } timeout)
        {
            element.Add(new XAttribute("timeout", timeout.ToXmlValue()));
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
        IReadOnlyDictionary<string, InitializerValue>? values = authentication.NamedValues;
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

    private sealed class LocalSendOneWayRequestCompiledConfig
    {
        public ExpressionValue<string>? Mode { get; init; }
        public ExpressionValue<int>? Timeout { get; init; }
        public ExpressionValue<string>? Url { get; init; }
        public ExpressionValue<string>? Method { get; init; }
        public InitializerValue? Headers { get; init; }
        public InitializerValue? Body { get; init; }
        public InitializerValue? Authentication { get; init; }
        public InitializerValue? Proxy { get; init; }
    }
}