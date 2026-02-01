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

        var count = new[]
        {
            certElement.TryAddAttribute("thumbprint", config.Thumbprint),
            certElement.TryAddAttribute("certificate-id", config.CertificateId),
            certElement.TryAddAttribute("body", config.Body)
        }.Count(x => x);

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

        certElement.TryAddAttribute("password", config.Password);

        context.AddPolicy(certElement);
    }

    public static void HandleCertificateAuthentication(
        XElement element,
        Configs.CertificateAuthenticationConfig config)
    {
        XElement certElement = new("authentication-certificate");

        var count = new[]
        {
            certElement.TryAddAttribute("thumbprint", config.Thumbprint),
            certElement.TryAddAttribute("certificate-id", config.CertificateId),
            certElement.TryAddAttribute("body", config.Body)
        }.Count(x => x);

        if (count != 1)
        {
            // Note: validation should be done at extraction time with CompiledConfigExtractor
            // This method assumes the config is already valid
            return;
        }

        certElement.TryAddAttribute("password", config.Password);

        element.Add(certElement);
    }
}