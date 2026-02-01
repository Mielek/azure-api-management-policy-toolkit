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

public class GetAuthorizationContextCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.GetAuthorizationContext);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.GetAuthorizationContextConfig>(
            node, context, "get-authorization-context");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("get-authorization-context");

        element.AddAttribute("provider-id", config.ProviderId);
        element.AddAttribute("authorization-id", config.AuthorizationId);
        element.AddAttribute("context-variable-name", config.ContextVariableName);
        element.AddOptionalAttribute("identity-type", config.IdentityType);
        element.AddOptionalAttribute("identity", config.Identity);
        element.AddOptionalAttribute("ignore-error", config.IgnoreError);

        context.AddPolicy(element);
    }
}