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

        element.TryAddAttribute("validate-revocation", config.ValidateRevocation);
        element.TryAddAttribute("validate-trust", config.ValidateTrust);
        element.TryAddAttribute("validate-not-before", config.ValidateNotBefore);
        element.TryAddAttribute("validate-not-after", config.ValidateNotAfter);
        element.TryAddAttribute("ignore-error", config.IgnoreError);

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
            identityElement.TryAddAttribute("thumbprint", identity.Thumbprint);
            identityElement.TryAddAttribute("serial-number", identity.SerialNumber);
            identityElement.TryAddAttribute("common-name", identity.CommonName);
            identityElement.TryAddAttribute("subject", identity.Subject);
            identityElement.TryAddAttribute("dns-name", identity.DnsName);
            identityElement.TryAddAttribute("issuer-subject", identity.IssuerSubject);
            identityElement.TryAddAttribute("issuer-thumbprint", identity.IssuerThumbprint);
            identityElement.TryAddAttribute("issuer-certificate-id", identity.IssuerCertificateId);
            identitiesElement.Add(identityElement);
        }

        return identitiesElement;
    }
}