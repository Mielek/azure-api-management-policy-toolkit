// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class ValidateClientCertificateCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.ValidateClientCertificate);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.ValidateClientCertificateConfig>(
            node, context, "validate-client-certificate");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        XElement element = new("validate-client-certificate");

        element.AddOptionalAttribute("validate-revocation", config.ValidateRevocation);
        element.AddOptionalAttribute("validate-trust", config.ValidateTrust);
        element.AddOptionalAttribute("validate-not-before", config.ValidateNotBefore);
        element.AddOptionalAttribute("validate-not-after", config.ValidateNotAfter);
        element.AddOptionalAttribute("ignore-error", config.IgnoreError);

        if (config.Identities is { } identities)
        {
            element.Add(HandleIdentities(identities));
        }

        context.AddPolicy(element);
    }

    private static XElement HandleIdentities(IReadOnlyList<CompiledConfigs.CertificateIdentity> identities)
    {
        XElement identitiesElement = new("identities");
        foreach (var identity in identities)
        {
            XElement identityElement = new("identity");
            identityElement.AddOptionalAttribute("thumbprint", identity.Thumbprint);
            identityElement.AddOptionalAttribute("serial-number", identity.SerialNumber);
            identityElement.AddOptionalAttribute("common-name", identity.CommonName);
            identityElement.AddOptionalAttribute("subject", identity.Subject);
            identityElement.AddOptionalAttribute("dns-name", identity.DnsName);
            identityElement.AddOptionalAttribute("issuer-subject", identity.IssuerSubject);
            identityElement.AddOptionalAttribute("issuer-thumbprint", identity.IssuerThumbprint);
            identityElement.AddOptionalAttribute("issuer-certificate-id", identity.IssuerCertificateId);
            identitiesElement.Add(identityElement);
        }

        return identitiesElement;
    }
}