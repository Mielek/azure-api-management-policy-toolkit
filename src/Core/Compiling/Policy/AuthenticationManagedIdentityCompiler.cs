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
        IDocumentCompilationContext context,
        XElement element,
        IReadOnlyDictionary<string, InitializerValue> values,
        SyntaxNode node)
    {
        XElement certElement = new("authentication-managed-identity");
        if (!certElement.AddAttribute(values, nameof(ManagedIdentityAuthenticationConfig.Resource), "resource"))
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.RequiredParameterNotDefined,
                node.GetLocation(),
                $"{element.Name}.authentication-managed-identity",
                nameof(ManagedIdentityAuthenticationConfig.Resource)
            ));
        }

        certElement.AddAttribute(values, nameof(ManagedIdentityAuthenticationConfig.ClientId), "client-id");
        certElement.AddAttribute(values, nameof(ManagedIdentityAuthenticationConfig.OutputTokenVariableName),
            "output-token-variable-name");
        certElement.AddAttribute(values, nameof(ManagedIdentityAuthenticationConfig.IgnoreError), "ignore-error");
        element.Add(certElement);
    }
}