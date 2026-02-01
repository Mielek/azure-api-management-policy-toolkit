// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class ValidateClientCertificateCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.ValidateClientCertificate);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<LocalValidateClientCertificateCompiledConfig>(
            node, context, "validate-client-certificate");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        XElement element = new("validate-client-certificate");

        if (config.ValidateRevocation is { } validateRevocation)
        {
            element.Add(new XAttribute("validate-revocation", validateRevocation.ToXmlValue()));
        }

        if (config.ValidateTrust is { } validateTrust)
        {
            element.Add(new XAttribute("validate-trust", validateTrust.ToXmlValue()));
        }

        if (config.ValidateNotBefore is { } validateNotBefore)
        {
            element.Add(new XAttribute("validate-not-before", validateNotBefore.ToXmlValue()));
        }

        if (config.ValidateNotAfter is { } validateNotAfter)
        {
            element.Add(new XAttribute("validate-not-after", validateNotAfter.ToXmlValue()));
        }

        if (config.IgnoreError is { } ignoreError)
        {
            element.Add(new XAttribute("ignore-error", ignoreError.ToXmlValue()));
        }

        if (config.Identities is { } identitiesValue)
        {
            XElement identities = HandleIdentities(context, identitiesValue);
            element.Add(identities);
        }

        context.AddPolicy(element);
    }

    private static XElement HandleIdentities(IDocumentCompilationContext context, InitializerValue identitiesValue)
    {
        XElement identities = new("identities");
        foreach (InitializerValue identityValue in identitiesValue.UnnamedValues ?? [])
        {
            if (!identityValue.TryGetValues<CertificateIdentity>(
                    out IReadOnlyDictionary<string, InitializerValue>? certValues))
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.PolicyArgumentIsNotOfRequiredType,
                    identityValue.Node.GetLocation(),
                    "identity",
                    nameof(CertificateIdentity)
                ));
                continue;
            }

            XElement identity = new("identity");
            identity.AddAttribute(certValues, nameof(CertificateIdentity.Thumbprint), "thumbprint");
            identity.AddAttribute(certValues, nameof(CertificateIdentity.SerialNumber), "serial-number");
            identity.AddAttribute(certValues, nameof(CertificateIdentity.CommonName), "common-name");
            identity.AddAttribute(certValues, nameof(CertificateIdentity.Subject), "subject");
            identity.AddAttribute(certValues, nameof(CertificateIdentity.DnsName), "dns-name");
            identity.AddAttribute(certValues, nameof(CertificateIdentity.IssuerSubject), "issuer-subject");
            identity.AddAttribute(certValues, nameof(CertificateIdentity.IssuerThumbprint), "issuer-thumbprint");
            identity.AddAttribute(certValues, nameof(CertificateIdentity.IssuerCertificateId), "issuer-certificate-id");
            identities.Add(identity);
        }

        return identities;
    }

    private sealed class LocalValidateClientCertificateCompiledConfig
    {
        public ExpressionValue<bool>? ValidateRevocation { get; init; }
        public ExpressionValue<bool>? ValidateTrust { get; init; }
        public ExpressionValue<bool>? ValidateNotBefore { get; init; }
        public ExpressionValue<bool>? ValidateNotAfter { get; init; }
        public ExpressionValue<bool>? IgnoreError { get; init; }
        public InitializerValue? Identities { get; init; }
    }
}