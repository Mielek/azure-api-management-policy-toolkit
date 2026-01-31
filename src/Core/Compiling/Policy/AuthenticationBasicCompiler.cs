// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class AuthenticationBasicCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.AuthenticationBasic);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        if (node.ArgumentList.Arguments.Count != 2)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.ArgumentCountMissMatchForPolicy,
                node.ArgumentList.GetLocation(),
                "authentication-basic"));
            return;
        }

        var usernameResult = ExpressionProcessor.Process(node.ArgumentList.Arguments[0].Expression, context);
        var passwordResult = ExpressionProcessor.Process(node.ArgumentList.Arguments[1].Expression, context);
        var combined = Result.Combine(usernameResult, passwordResult);
        
        if (!combined.IsSuccess)
        {
            combined.ReportAll(context);
            return;
        }

        context.AddPolicy(new XElement("authentication-basic", 
            new XAttribute("username", usernameResult.Value),
            new XAttribute("password", passwordResult.Value)));
    }

    public static void HandleBasicAuthentication(
        IDocumentCompilationContext context,
        XElement element,
        IReadOnlyDictionary<string, InitializerValue> values,
        SyntaxNode node)
    {
        XElement basicElement = new("authentication-basic");
        if (!basicElement.AddAttribute(values, nameof(BasicAuthenticationConfig.Username), "username"))
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.RequiredParameterNotDefined,
                node.GetLocation(),
                "authentication-basic",
                nameof(BasicAuthenticationConfig.Username)
            ));
            return;
        }

        if (!basicElement.AddAttribute(values, nameof(BasicAuthenticationConfig.Password), "password"))
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.RequiredParameterNotDefined,
                node.GetLocation(),
                "authentication-basic",
                nameof(BasicAuthenticationConfig.Password)
            ));
            return;
        }

        element.Add(basicElement);
    }
}