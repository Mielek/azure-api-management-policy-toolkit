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

public class MockResponseCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.MockResponse);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var arguments = node.ArgumentList.Arguments;
        if (arguments.Count > 1)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.ArgumentCountMissMatchForPolicy,
                node.ArgumentList.GetLocation(),
                "mock-response"));
            return;
        }

        var element = new XElement("mock-response");
        if (arguments.Count == 1)
        {
            var configResult = CompiledConfigExtractor.ExtractFromExpression<CompiledConfigs.MockResponseConfig>(
                arguments[0].Expression, context, "mock-response");
            if (!configResult.IsSuccess)
            {
                configResult.ReportAll(context);
                return;
            }

            var config = configResult.Value;
            element.AddOptionalAttribute("status-code", config.StatusCode);
            element.AddOptionalAttribute("content-type", config.ContentType);
            element.AddOptionalAttribute("index", config.Index);
        }

        context.AddPolicy(element);
    }
}