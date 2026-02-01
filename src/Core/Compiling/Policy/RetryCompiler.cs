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

public class RetryCompiler : IMethodPolicyHandler
{
    private readonly Lazy<BlockCompiler> _blockCompiler;

    public RetryCompiler(Lazy<BlockCompiler> blockCompiler)
    {
        _blockCompiler = blockCompiler;
    }

    public string MethodName => nameof(IInboundContext.Retry);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        if (node.ArgumentList.Arguments.Count != 2)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.ArgumentCountMissMatchForPolicy,
                node.ArgumentList.GetLocation(),
                "retry"));
            return;
        }

        ExpressionSyntax configExpression = node.ArgumentList.Arguments[0].Expression;
        var configResult = CompiledConfigExtractor.ExtractFromExpression<CompiledConfigs.RetryConfig>(
            configExpression, context, "retry");
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
                "retry",
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

        XElement element = new("retry");
        
        element.Add(new XAttribute("condition", config.Condition.ToXmlValue()));
        element.Add(new XAttribute("count", config.Count.ToXmlValue()));
        element.AddOptionalAttribute("interval", config.Interval);
        element.AddOptionalAttribute("max-interval", config.MaxInterval);
        element.AddOptionalAttribute("delta", config.Delta);
        element.AddOptionalAttribute("first-fast-retry", config.FirstFastRetry);

        var subContext = new DocumentCompilationContext(context, element);
        _blockCompiler.Value.Compile(subContext, lambda.Block);

        context.AddPolicy(element);
    }
}