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

public class ValidateJwtCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.ValidateJwt);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.ValidateJwtConfig>(
            node, context, "validate-jwt");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("validate-jwt");

        var headerNameAdded = false;
        var queryParamAdded = false;
        var tokenValueAdded = false;

        if (config.HeaderName is { } headerName)
        {
            element.Add(new XAttribute("header-name", headerName.ToXmlValue()));
            headerNameAdded = true;
        }

        if (config.QueryParameterName is { } queryParam)
        {
            element.Add(new XAttribute("query-parameter-name", queryParam.ToXmlValue()));
            queryParamAdded = true;
        }

        if (config.TokenValue is { } tokenValue)
        {
            element.Add(new XAttribute("token-value", tokenValue.ToXmlValue()));
            tokenValueAdded = true;
        }

        if (new[] { headerNameAdded, queryParamAdded, tokenValueAdded }.Count(b => b) != 1)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.OnlyOneOfTreeShouldBeDefined,
                node.ArgumentList.GetLocation(),
                "validate-jwt",
                nameof(ValidateJwtConfig.HeaderName),
                nameof(ValidateJwtConfig.QueryParameterName),
                nameof(ValidateJwtConfig.TokenValue)
            ));
            return;
        }

        element.TryAddAttribute("failed-validation-httpcode", config.FailedValidationHttpCode);
        element.TryAddAttribute("failed-validation-error-message", config.FailedValidationErrorMessage);
        element.TryAddAttribute("require-expiration-time", config.RequireExpirationTime);
        element.TryAddAttribute("require-scheme", config.RequireScheme);
        element.TryAddAttribute("require-signed-tokens", config.RequireSignedTokens);
        element.TryAddAttribute("clock-skew", config.ClockSkew);
        element.TryAddAttribute("output-token-variable-name", config.OutputTokenVariableName);

        if (config.OpenIdConfigs is { } openIdConfigs)
        {
            HandleOpenIdConfigs(element, openIdConfigs);
        }

        if (config.IssuerSigningKeys is { } issuerSigningKeys)
        {
            HandleKeys(element, issuerSigningKeys, "issuer-signing-keys");
        }

        if (config.DescriptionKeys is { } descriptionKeys)
        {
            HandleKeys(element, descriptionKeys, "decryption-keys");
        }

        if (config.Audiences is { } audiences)
        {
            HandleExpressionList(element, audiences, "audiences", "audience");
        }

        if (config.Issuers is { } issuers)
        {
            HandleExpressionList(element, issuers, "issuers", "issuer");
        }

        if (config.RequiredClaims is { } requiredClaims)
        {
            element.Add(ClaimsConfigCompiler.HandleRequiredClaims(requiredClaims));
        }

        context.AddPolicy(element);
    }

    private static void HandleOpenIdConfigs(XElement element, IReadOnlyList<CompiledConfigs.OpenIdConfig> openIdConfigs)
    {
        foreach (var openIdConfig in openIdConfigs)
        {
            var openIdElement = new XElement("openid-config");
            openIdElement.Add(new XAttribute("url", openIdConfig.Url));
            element.Add(openIdElement);
        }
    }

    private static void HandleKeys(XElement element, IReadOnlyList<CompiledConfigs.KeyConfig> keys, string listName)
    {
        var listElement = new XElement(listName);
        foreach (var key in keys)
        {
            var keyElement = new XElement("key");
            keyElement.TryAddAttribute("id", key.Id);

            switch (key)
            {
                case CompiledConfigs.Base64KeyConfig base64Key:
                    keyElement.Value = base64Key.Value;
                    break;
                case CompiledConfigs.CertificateKeyConfig certKey:
                    keyElement.Add(new XAttribute("certificate-id", certKey.CertificateId));
                    break;
                case CompiledConfigs.AsymmetricKeyConfig asymKey:
                    keyElement.Add(new XAttribute("n", asymKey.Modulus));
                    keyElement.Add(new XAttribute("e", asymKey.Exponent));
                    break;
            }

            listElement.Add(keyElement);
        }

        element.Add(listElement);
    }

    private static void HandleExpressionList(
        XElement element,
        IReadOnlyList<ExpressionValue<string>> values,
        string listName,
        string elementName)
    {
        var listElement = new XElement(listName);
        foreach (var value in values)
        {
            listElement.Add(new XElement(elementName, value.ToXmlValue()));
        }

        element.Add(listElement);
    }
}