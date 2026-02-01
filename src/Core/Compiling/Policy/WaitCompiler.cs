using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Syntax;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class WaitCompiler : IMethodPolicyHandler
{
    private readonly Lazy<BlockCompiler> _blockCompiler;

    public WaitCompiler(Lazy<BlockCompiler> blockCompiler)
    {
        _blockCompiler = blockCompiler;
    }

    public string MethodName => nameof(IInboundContext.Wait);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        if (node.ArgumentList.Arguments.Count is > 2 or < 1)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.ArgumentCountMissMatchForPolicy,
                node.ArgumentList.GetLocation(),
                "wait"));
            return;
        }

        ExpressionSyntax childPoliciesLambdaExpression = node.ArgumentList.Arguments[0].Expression;
        if (childPoliciesLambdaExpression is not LambdaExpressionSyntax lambda)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.ValueShouldBe,
                childPoliciesLambdaExpression.GetLocation(),
                "wait",
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

        XElement element = new("wait");

        if (node.ArgumentList.Arguments.Count == 2)
        {
            var valueResult = ExpressionProcessor.Process(node.ArgumentList.Arguments[1].Expression, context);
            if (!valueResult.IsSuccess)
            {
                valueResult.ReportAll(context);
                return;
            }
            element.AddAttribute("for", valueResult.Value);
        }

        var subContext = new DocumentCompilationContext(context, element);
        _blockCompiler.Value.Compile(subContext, lambda.Block);

        context.AddPolicy(element);
    }
}