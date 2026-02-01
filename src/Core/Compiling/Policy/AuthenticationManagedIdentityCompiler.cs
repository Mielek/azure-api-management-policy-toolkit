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

        element.Add(new XAttribute("resource", config.Resource.ToXmlValue()));

        if (config.ClientId is { } clientId)
        {
            element.Add(new XAttribute("client-id", clientId.ToXmlValue()));
        }

        if (config.OutputTokenVariableName is { } outputTokenVariableName)
        {
            element.Add(new XAttribute("output-token-variable-name", outputTokenVariableName.ToXmlValue()));
        }

        if (config.IgnoreError is { } ignoreError)
        {
            element.Add(new XAttribute("ignore-error", ignoreError.ToXmlValue()));
        }

        context.AddPolicy(element);
    }

    public static void HandleManagedIdentityAuthentication(
        XElement element,
        Configs.ManagedIdentityAuthenticationConfig config)
    {
        XElement certElement = new("authentication-managed-identity");
        certElement.Add(new XAttribute("resource", config.Resource.ToXmlValue()));
        certElement.TryAddAttribute("client-id", config.ClientId);
        certElement.TryAddAttribute("output-token-variable-name", config.OutputTokenVariableName);
        certElement.TryAddAttribute("ignore-error", config.IgnoreError);
        element.Add(certElement);
    }
}