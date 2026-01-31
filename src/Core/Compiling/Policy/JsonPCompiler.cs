// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class JsonPCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IOutboundContext.JsonP);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var arguments = node.ArgumentList.Arguments;
        if (arguments.Count != 1)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.ArgumentCountMissMatchForPolicy,
                node.ArgumentList.GetLocation(),
                "jsonp"));
            return;
        }

        var valueResult = ExpressionProcessor.Process(node.ArgumentList.Arguments[0].Expression, context);
        if (!valueResult.IsSuccess)
        {
            valueResult.ReportAll(context);
            return;
        }
        context.AddPolicy(new XElement("jsonp", new XAttribute("callback-parameter-name", valueResult.Value)));
    }
}