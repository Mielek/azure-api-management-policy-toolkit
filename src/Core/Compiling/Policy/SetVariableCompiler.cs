// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class SetVariableCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.SetVariable);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        if (node.ArgumentList.Arguments.Count != 2)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.ArgumentCountMissMatchForPolicy,
                node.ArgumentList.GetLocation(),
                "set-variable"));
            return;
        }

        var nameResult = ExpressionProcessor.Process(node.ArgumentList.Arguments[0].Expression, context);
        var valueResult = ExpressionProcessor.Process(node.ArgumentList.Arguments[1].Expression, context);
        var combined = Result.Combine(nameResult, valueResult);
        
        if (!combined.IsSuccess)
        {
            combined.ReportAll(context);
            return;
        }

        context.AddPolicy(new XElement("set-variable", 
            new XAttribute("name", nameResult.Value), 
            new XAttribute("value", valueResult.Value)));
    }
}