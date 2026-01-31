// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class CacheStoreCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IOutboundContext.CacheStore);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var arguments = node.ArgumentList.Arguments;
        if (arguments.Count is > 2 or < 1)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.ArgumentCountMissMatchForPolicy,
                node.ArgumentList.GetLocation(),
                "cache-store"));
            return;
        }

        var element = new XElement("cache-store");

        var durationResult = ExpressionProcessor.Process(arguments[0].Expression, context);
        if (!durationResult.IsSuccess)
        {
            durationResult.ReportAll(context);
            return;
        }
        element.Add(new XAttribute("duration", durationResult.Value));

        if (arguments.Count == 2)
        {
            var cacheResponseResult = ExpressionProcessor.Process(arguments[1].Expression, context);
            if (!cacheResponseResult.IsSuccess)
            {
                cacheResponseResult.ReportAll(context);
                return;
            }
            element.Add(new XAttribute("cache-response", cacheResponseResult.Value));
        }

        context.AddPolicy(element);
    }
}