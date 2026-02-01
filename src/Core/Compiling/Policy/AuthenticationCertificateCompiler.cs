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

public class AuthenticationCertificateCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.AuthenticationCertificate);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.CertificateAuthenticationConfig>(
            node, context, "authentication-certificate");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var certElement = new XElement("authentication-certificate");

        var thumbprintAdded = false;
        var certIdAdded = false;
        var bodyAdded = false;

        if (config.Thumbprint is { } thumbprint)
        {
            certElement.Add(new XAttribute("thumbprint", thumbprint.ToXmlValue()));
            thumbprintAdded = true;
        }

        if (config.CertificateId is { } certId)
        {
            certElement.Add(new XAttribute("certificate-id", certId.ToXmlValue()));
            certIdAdded = true;
        }

        if (config.Body is { } body)
        {
            certElement.Add(new XAttribute("body", body.ToXmlValue()));
            bodyAdded = true;
        }

        var count = new[] { thumbprintAdded, certIdAdded, bodyAdded }.Count(x => x);
        if (count != 1)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.OnlyOneOfTreeShouldBeDefined,
                node.ArgumentList.GetLocation(),
                "authentication-certificate",
                nameof(CertificateAuthenticationConfig.Thumbprint),
                nameof(CertificateAuthenticationConfig.CertificateId),
                nameof(CertificateAuthenticationConfig.Body)
            ));
            return;
        }

        if (config.Password is { } password)
        {
            certElement.Add(new XAttribute("password", password.ToXmlValue()));
        }

        context.AddPolicy(certElement);
    }

    public static void HandleCertificateAuthentication(
        IDocumentCompilationContext context,
        XElement element,
        IReadOnlyDictionary<string, InitializerValue> values,
        SyntaxNode node)
    {
        XElement certElement = new("authentication-certificate");
        certElement.AddAttribute(values, nameof(CertificateAuthenticationConfig.Password), "password");

        if (new[]
            {
                certElement.AddAttribute(values, nameof(CertificateAuthenticationConfig.Thumbprint), "thumbprint"),
                certElement.AddAttribute(values, nameof(CertificateAuthenticationConfig.CertificateId),
                    "certificate-id"),
                certElement.AddAttribute(values, nameof(CertificateAuthenticationConfig.Body), "body")
            }.Count(b => b) != 1)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.OnlyOneOfTreeShouldBeDefined,
                node.GetLocation(),
                $"{element.Name}.authentication-certificate",
                nameof(CertificateAuthenticationConfig.Thumbprint),
                nameof(CertificateAuthenticationConfig.CertificateId),
                nameof(CertificateAuthenticationConfig.Body)
            ));
            return;
        }

        element.Add(certElement);
    }
}