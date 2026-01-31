// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class RewriteUriCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.RewriteUri);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var arguments = node.ArgumentList.Arguments;
        if (arguments.Count is > 2 or 0)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.ArgumentCountMissMatchForPolicy,
                node.ArgumentList.GetLocation(),
                "rewrite-uri"));
            return;
        }

        var element = new XElement("rewrite-uri");
        var templateResult = ExpressionProcessor.Process(node.ArgumentList.Arguments[0].Expression, context);
        if (!templateResult.IsSuccess)
        {
            templateResult.ReportAll(context);
            return;
        }
        element.Add(new XAttribute("template", templateResult.Value));

        if (node.ArgumentList.Arguments.Count == 2)
        {
            var copyUnmatchedParamsResult = ExpressionProcessor.Process(node.ArgumentList.Arguments[1].Expression, context);
            if (!copyUnmatchedParamsResult.IsSuccess)
            {
                copyUnmatchedParamsResult.ReportAll(context);
                return;
            }
            element.Add(new XAttribute("copy-unmatched-params", copyUnmatchedParamsResult.Value));
        }

        context.AddPolicy(element);
    }
}