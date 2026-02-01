// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Syntax;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class LimitConcurrencyCompiler : IMethodPolicyHandler
{
    private readonly Lazy<BlockCompiler> _blockCompiler;

    public LimitConcurrencyCompiler(Lazy<BlockCompiler> blockCompiler)
    {
        _blockCompiler = blockCompiler;
    }

    public string MethodName => nameof(IInboundContext.LimitConcurrency);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        if (node.ArgumentList.Arguments.Count != 2)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.ArgumentCountMissMatchForPolicy,
                node.ArgumentList.GetLocation(),
                "limit-concurrency"));
            return;
        }

        ExpressionSyntax configExpression = node.ArgumentList.Arguments[0].Expression;
        var configResult = CompiledConfigExtractor.ExtractFromExpression<CompiledConfigs.LimitConcurrencyConfig>(
            configExpression, context, "limit-concurrency");
        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }
        var config = configResult.Value;

        ExpressionSyntax childPoliciesLambdaExpression = node.ArgumentList.Arguments[1].Expression;
        if (childPoliciesLambdaExpression is not LambdaExpressionSyntax lambda)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.ValueShouldBe,
                childPoliciesLambdaExpression.GetLocation(),
                "limit-concurrency",
                nameof(LambdaExpressionSyntax)));
            return;
        }

        if (lambda.Block is null)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.NotSupportedStatement,
                lambda.GetLocation(),
                childPoliciesLambdaExpression.GetType().FullName
            ));
            return;
        }

        XElement element = new("limit-concurrency");

        element.AddAttribute("key", config.Key);
        element.AddAttribute("max-count", config.MaxCount);

        var subContext = new DocumentCompilationContext(context, element);
        _blockCompiler.Value.Compile(subContext, lambda.Block);

        context.AddPolicy(element);
    }
}