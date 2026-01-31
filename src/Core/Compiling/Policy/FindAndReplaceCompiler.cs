// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class FindAndReplaceCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.FindAndReplace);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        if (node.ArgumentList.Arguments.Count != 2)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.ArgumentCountMissMatchForPolicy,
                node.ArgumentList.GetLocation(),
                "find-and-replace"));
            return;
        }

        var fromResult = ExpressionProcessor.Process(node.ArgumentList.Arguments[0].Expression, context);
        var toResult = ExpressionProcessor.Process(node.ArgumentList.Arguments[1].Expression, context);
        var combined = Result.Combine(fromResult, toResult);
        
        if (!combined.IsSuccess)
        {
            combined.ReportAll(context);
            return;
        }
        
        context.AddPolicy(new XElement("find-and-replace", new XAttribute("from", fromResult.Value), new XAttribute("to", toResult.Value)));
    }
}