// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class ValidateAzureAdTokenCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.ValidateAzureAdToken);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<LocalValidateAzureAdTokenCompiledConfig>(
            node, context, "validate-azure-ad-token");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        XElement element = new("validate-azure-ad-token");

        element.Add(new XAttribute("tenant-id", config.TenantId.ToXmlValue()));

        if (config.HeaderName is { } headerName)
        {
            element.Add(new XAttribute("header-name", headerName.ToXmlValue()));
        }

        if (config.QueryParameterName is { } queryParam)
        {
            element.Add(new XAttribute("query-parameter-name", queryParam.ToXmlValue()));
        }

        if (config.TokenValue is { } tokenValue)
        {
            element.Add(new XAttribute("token-value", tokenValue.ToXmlValue()));
        }

        if (config.FailedValidationHttpCode is { } failedHttpCode)
        {
            element.Add(new XAttribute("failed-validation-httpcode", failedHttpCode.ToXmlValue()));
        }

        if (config.FailedValidationErrorMessage is { } failedMsg)
        {
            element.Add(new XAttribute("failed-validation-error-message", failedMsg.ToXmlValue()));
        }

        if (config.OutputTokenVariableName is { } outputVar)
        {
            element.Add(new XAttribute("output-token-variable-name", outputVar.ToXmlValue()));
        }

        if (config.BackendApplicationIds is { } backendIds)
        {
            GenericCompiler.HandleListFromInitializer(element, backendIds, "backend-application-ids", "application-id");
        }

        if (config.ClientApplicationIds is { } clientIds)
        {
            GenericCompiler.HandleListFromInitializer(element, clientIds, "client-application-ids", "application-id");
        }

        if (config.Audiences is { } audiences)
        {
            GenericCompiler.HandleListFromInitializer(element, audiences, "audiences", "audience");
        }

        if (config.RequiredClaims is { } requiredClaims)
        {
            element.Add(ClaimsConfigCompiler.HandleRequiredClaims(context, requiredClaims));
        }

        if (config.DecryptionKeys is { } decryptionKeys)
        {
            element.Add(HandleDecryptionKeys(context, decryptionKeys));
        }

        context.AddPolicy(element);
    }

    private static XElement HandleDecryptionKeys(IDocumentCompilationContext context, InitializerValue decryptionKeys)
    {
        XElement listElement = new("decryption-keys");
        foreach (InitializerValue initializer in decryptionKeys.UnnamedValues ?? [])
        {
            if (!initializer.TryGetValues<DecryptionKey>(
                    out IReadOnlyDictionary<string, InitializerValue>? decryptionKey))
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.PolicyArgumentIsNotAnObjectCreation,
                    initializer.Node.GetLocation(),
                    "validate-azure-ad-token.decryption-keys.key",
                    nameof(DecryptionKey)
                ));
                continue;
            }

            XElement decryptionElement = new("key");
            if (!decryptionElement.AddAttribute(decryptionKey, nameof(DecryptionKey.CertificateId),
                    "certificate-id"))
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.RequiredParameterNotDefined,
                    initializer.Node.GetLocation(),
                    "validate-azure-ad-token.decryption-keys.key",
                    nameof(DecryptionKey.CertificateId)
                ));
            }

            listElement.Add(decryptionElement);
        }

        return listElement;
    }

    private sealed class LocalValidateAzureAdTokenCompiledConfig
    {
        public required ExpressionValue<string> TenantId { get; init; }
        public ExpressionValue<string>? HeaderName { get; init; }
        public ExpressionValue<string>? QueryParameterName { get; init; }
        public ExpressionValue<string>? TokenValue { get; init; }
        public ExpressionValue<int>? FailedValidationHttpCode { get; init; }
        public ExpressionValue<string>? FailedValidationErrorMessage { get; init; }
        public ExpressionValue<string>? OutputTokenVariableName { get; init; }
        public InitializerValue? BackendApplicationIds { get; init; }
        public InitializerValue? ClientApplicationIds { get; init; }
        public InitializerValue? Audiences { get; init; }
        public InitializerValue? RequiredClaims { get; init; }
        public InitializerValue? DecryptionKeys { get; init; }
    }
}