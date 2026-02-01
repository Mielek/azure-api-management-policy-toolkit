// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class ValidateAzureAdTokenCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.ValidateAzureAdToken);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.ValidateAzureAdTokenConfig>(
            node, context, "validate-azure-ad-token");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        XElement element = new("validate-azure-ad-token");

        element.AddAttribute("tenant-id", config.TenantId);

        element.AddOptionalAttribute("header-name", config.HeaderName);
        element.AddOptionalAttribute("query-parameter-name", config.QueryParameterName);
        element.AddOptionalAttribute("token-value", config.TokenValue);
        element.AddOptionalAttribute("failed-validation-httpcode", config.FailedValidationHttpCode);
        element.AddOptionalAttribute("failed-validation-error-message", config.FailedValidationErrorMessage);
        element.AddOptionalAttribute("output-token-variable-name", config.OutputTokenVariableName);

        if (config.BackendApplicationIds is { } backendIds)
        {
            GenericCompiler.HandleList(element, backendIds, "backend-application-ids", "application-id");
        }

        if (config.ClientApplicationIds is { } clientIds)
        {
            GenericCompiler.HandleList(element, clientIds, "client-application-ids", "application-id");
        }

        if (config.Audiences is { } audiences)
        {
            GenericCompiler.HandleList(element, audiences, "audiences", "audience");
        }

        if (config.RequiredClaims is { } requiredClaims)
        {
            element.Add(ClaimsConfigCompiler.HandleRequiredClaims(requiredClaims));
        }

        if (config.DecryptionKeys is { } decryptionKeys)
        {
            element.Add(HandleDecryptionKeys(decryptionKeys));
        }

        context.AddPolicy(element);
    }

    private static XElement HandleDecryptionKeys(IReadOnlyList<CompiledConfigs.DecryptionKey> decryptionKeys)
    {
        XElement listElement = new("decryption-keys");
        foreach (var key in decryptionKeys)
        {
            XElement keyElement = new("key");
            keyElement.AddAttribute("certificate-id", key.CertificateId);
            listElement.Add(keyElement);
        }

        return listElement;
    }
}