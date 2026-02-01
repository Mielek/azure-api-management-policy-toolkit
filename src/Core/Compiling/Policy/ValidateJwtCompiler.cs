// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class ValidateJwtCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.ValidateJwt);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<LocalValidateJwtCompiledConfig>(
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

        if (config.FailedValidationHttpCode is { } failedHttpCode)
        {
            element.Add(new XAttribute("failed-validation-httpcode", failedHttpCode.ToXmlValue()));
        }

        if (config.FailedValidationErrorMessage is { } failedMsg)
        {
            element.Add(new XAttribute("failed-validation-error-message", failedMsg.ToXmlValue()));
        }

        if (config.RequireExpirationTime is { } requireExp)
        {
            element.Add(new XAttribute("require-expiration-time", requireExp.ToXmlValue()));
        }

        if (config.RequireScheme is { } requireScheme)
        {
            element.Add(new XAttribute("require-scheme", requireScheme.ToXmlValue()));
        }

        if (config.RequireSignedTokens is { } requireSigned)
        {
            element.Add(new XAttribute("require-signed-tokens", requireSigned.ToXmlValue()));
        }

        if (config.ClockSkew is { } clockSkew)
        {
            element.Add(new XAttribute("clock-skew", clockSkew.ToXmlValue()));
        }

        if (config.OutputTokenVariableName is { } outputVar)
        {
            element.Add(new XAttribute("output-token-variable-name", outputVar.ToXmlValue()));
        }

        if (config.OpenIdConfigs is { } openIdConfigs)
        {
            var openIdElements = HandleOpenIdConfigs(context, openIdConfigs);
            element.Add(openIdElements);
        }

        if (config.IssuerSigningKeys is { } issuerSigningKeys)
        {
            HandleKeys(context, element, issuerSigningKeys, "issuer-signing-keys");
        }

        if (config.DescriptionKeys is { } descriptionKeys)
        {
            HandleKeys(context, element, descriptionKeys, "decryption-keys");
        }

        if (config.Audiences is { } audiences)
        {
            GenericCompiler.HandleListFromInitializer(element, audiences, "audiences", "audience");
        }

        if (config.Issuers is { } issuers)
        {
            GenericCompiler.HandleListFromInitializer(element, issuers, "issuers", "issuer");
        }

        if (config.RequiredClaims is { } requiredClaims)
        {
            XElement claimsElement = ClaimsConfigCompiler.HandleRequiredClaims(context, requiredClaims);
            element.Add(claimsElement);
        }

        context.AddPolicy(element);
    }

    private static object[] HandleOpenIdConfigs(IDocumentCompilationContext context, InitializerValue openIdConfigs)
    {
        var openIdElements = new List<object>();
        foreach (var openIdConfig in openIdConfigs.UnnamedValues ?? [])
        {
            if (!openIdConfig.TryGetValues<OpenIdConfig>(out var openIdConfigValues))
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.PolicyArgumentIsNotOfRequiredType,
                    openIdConfig.Node.GetLocation(),
                    "openid-config",
                    nameof(OpenIdConfig)
                ));
                continue;
            }

            var openIdElement = new XElement("openid-config");
            if (!openIdElement.AddAttribute(openIdConfigValues, nameof(OpenIdConfig.Url), "url"))
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.RequiredParameterNotDefined,
                    openIdConfig.Node.GetLocation(),
                    "openid-config",
                    nameof(OpenIdConfig.Url)
                ));
                continue;
            }

            openIdElements.Add(openIdElement);
        }

        return openIdElements.ToArray();
    }

    private static void HandleKeys(
        IDocumentCompilationContext context,
        XElement element,
        InitializerValue listInitializer,
        string listName)
    {
        var listElement = new XElement(listName);
        foreach (var initializer in listInitializer.UnnamedValues ?? [])
        {
            var keyValues = initializer.NamedValues;
            if (keyValues is null)
            {
                continue;
            }

            var keyElement = new XElement("key");
            keyElement.AddAttribute(keyValues, nameof(KeyConfig.Id), "id");
            switch (initializer.Type)
            {
                case nameof(Base64KeyConfig):
                    if (!keyValues.TryGetValue(nameof(Base64KeyConfig.Value), out var value))
                    {
                        context.Report(Diagnostic.Create(
                            CompilationErrors.RequiredParameterNotDefined,
                            initializer.Node.GetLocation(),
                            "key",
                            nameof(Base64KeyConfig.Value)
                        ));
                        continue;
                    }

                    keyElement.Value = value.Value!;
                    break;
                case nameof(CertificateKeyConfig):
                    if (!keyElement.AddAttribute(keyValues, nameof(CertificateKeyConfig.CertificateId),
                            "certificate-id"))
                    {
                        context.Report(Diagnostic.Create(
                            CompilationErrors.RequiredParameterNotDefined,
                            initializer.Node.GetLocation(),
                            "key",
                            nameof(CertificateKeyConfig.CertificateId)
                        ));
                        continue;
                    }

                    break;
                case nameof(AsymmetricKeyConfig):
                    if (!keyElement.AddAttribute(keyValues, nameof(AsymmetricKeyConfig.Modulus), "n"))
                    {
                        context.Report(Diagnostic.Create(
                            CompilationErrors.RequiredParameterNotDefined,
                            initializer.Node.GetLocation(),
                            "key",
                            nameof(AsymmetricKeyConfig.Modulus)
                        ));
                        continue;
                    }

                    if (!keyElement.AddAttribute(keyValues, nameof(AsymmetricKeyConfig.Exponent), "e"))
                    {
                        context.Report(Diagnostic.Create(
                            CompilationErrors.RequiredParameterNotDefined,
                            initializer.Node.GetLocation(),
                            "key",
                            nameof(AsymmetricKeyConfig.Exponent)
                        ));
                        continue;
                    }

                    break;
                default:
                    context.Report(Diagnostic.Create(
                        CompilationErrors.NotSupportedType,
                        initializer.Node.GetLocation(),
                        "key",
                        initializer.Type
                    ));
                    continue;
            }

            listElement.Add(keyElement);
        }

        element.Add(listElement);
    }

    private sealed class LocalValidateJwtCompiledConfig
    {
        public ExpressionValue<string>? HeaderName { get; init; }
        public ExpressionValue<string>? QueryParameterName { get; init; }
        public ExpressionValue<string>? TokenValue { get; init; }
        public ExpressionValue<int>? FailedValidationHttpCode { get; init; }
        public ExpressionValue<string>? FailedValidationErrorMessage { get; init; }
        public ExpressionValue<bool>? RequireExpirationTime { get; init; }
        public ExpressionValue<string>? RequireScheme { get; init; }
        public ExpressionValue<bool>? RequireSignedTokens { get; init; }
        public ExpressionValue<int>? ClockSkew { get; init; }
        public ExpressionValue<string>? OutputTokenVariableName { get; init; }
        public InitializerValue? OpenIdConfigs { get; init; }
        public InitializerValue? IssuerSigningKeys { get; init; }
        public InitializerValue? DescriptionKeys { get; init; }
        public InitializerValue? Audiences { get; init; }
        public InitializerValue? Issuers { get; init; }
        public InitializerValue? RequiredClaims { get; init; }
    }
}