// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class AuthenticationManageIdentityReturnValueCompiler : IReturnValueMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.AuthenticationManagedIdentity);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node, string variableName)
    {
        var policy = new XElement("authentication-managed-identity");
        var resourceResult = ExpressionProcessor.Process(node.ArgumentList.Arguments[0].Expression, context);
        if (!resourceResult.IsSuccess)
        {
            resourceResult.ReportAll(context);
            return;
        }
        policy.AddAttribute("resource", resourceResult.Value);
        policy.AddAttribute("output-token-variable-name", variableName);

        context.AddPolicy(policy);
    }
}