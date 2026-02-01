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

public class AuthenticationManagedIdentityCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.AuthenticationManagedIdentity);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.ManagedIdentityAuthenticationConfig>(
            node, context, "authentication-managed-identity");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("authentication-managed-identity");

        element.AddAttribute("resource", config.Resource);
        element.AddOptionalAttribute("client-id", config.ClientId);
        element.AddOptionalAttribute("output-token-variable-name", config.OutputTokenVariableName);
        element.AddOptionalAttribute("ignore-error", config.IgnoreError);

        context.AddPolicy(element);
    }

    public static void HandleManagedIdentityAuthentication(
        XElement element,
        Configs.ManagedIdentityAuthenticationConfig config)
    {
        XElement certElement = new("authentication-managed-identity");
        certElement.AddAttribute("resource", config.Resource);
        certElement.AddOptionalAttribute("client-id", config.ClientId);
        certElement.AddOptionalAttribute("output-token-variable-name", config.OutputTokenVariableName);
        certElement.AddOptionalAttribute("ignore-error", config.IgnoreError);
        element.Add(certElement);
    }
}