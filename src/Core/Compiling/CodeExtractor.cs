// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling;

/// <summary>
/// Extracts code from method invocations and member access expressions using semantic analysis.
/// Requires ICompilationContext for symbol resolution.
/// </summary>
public static class CodeExtractor
{
    /// <summary>
    /// Prefix for expression-bodied methods in APIM policy expressions.
    /// </summary>
    public const string ExpressionBodyPrefix = "@";

    /// <summary>
    /// Opening wrapper for block-bodied methods in APIM policy expressions.
    /// </summary>
    public const string ExpressionStatementPrefix = "@(";

    /// <summary>
    /// Closing wrapper for block-bodied methods in APIM policy expressions.
    /// </summary>
    public const string ExpressionStatementSuffix = ")";

    /// <summary>
    /// Extracts code from a method invocation expression.
    /// Uses semantic analysis to resolve the method symbol and find its declaration.
    /// </summary>
    public static Result<string> Extract(InvocationExpressionSyntax syntax, ICompilationContext context)
    {
        var compilation = context.Compilation;
        var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
        var symbolInfo = semanticModel.GetSymbolInfo(syntax.Expression);
        var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.SingleOrDefault(s => s is IMethodSymbol);

        if (symbol is not IMethodSymbol methodSymbol)
        {
            return Result<string>.Failure(Diagnostic.Create(
                CompilationErrors.InvalidExpression,
                syntax.GetLocation()
            ));
        }

        var expressionMethod = methodSymbol.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();

        if (expressionMethod is null)
        {
            return Result<string>.Failure(Diagnostic.Create(
                CompilationErrors.CannotFindMethodCode,
                syntax.GetLocation(),
                methodSymbol.Name
            ));
        }

        expressionMethod = SyntaxNormalizer.Normalize(expressionMethod);

        if (expressionMethod.Body != null)
        {
            return $"{ExpressionBodyPrefix}{expressionMethod.Body.ToFullString().TrimEnd()}";
        }
        
        if (expressionMethod.ExpressionBody != null)
        {
            return $"{ExpressionStatementPrefix}{expressionMethod.ExpressionBody.Expression.ToFullString().TrimEnd()}{ExpressionStatementSuffix}";
        }

        // This replaces the InvalidOperationException from the original code
        return Result<string>.Failure(Diagnostic.Create(
            CompilationErrors.InvalidExpression,
            syntax.GetLocation()
        ));
    }

    /// <summary>
    /// Extracts a constant value from a member access expression.
    /// Uses semantic analysis to resolve the field symbol and verify it's a constant.
    /// </summary>
    public static Result<string> Extract(MemberAccessExpressionSyntax syntax, ICompilationContext context)
    {
        var compilation = context.Compilation;
        var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
        var symbolInfo = semanticModel.GetSymbolInfo(syntax);
        var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.SingleOrDefault(s => s is IFieldSymbol);

        if (symbol is not IFieldSymbol fieldSymbol)
        {
            return Result<string>.Failure(Diagnostic.Create(
                CompilationErrors.InvalidConstantReference,
                syntax.GetLocation()
            ));
        }

        if (!fieldSymbol.IsConst)
        {
            return Result<string>.Failure(Diagnostic.Create(
                CompilationErrors.InvalidExpression,
                syntax.GetLocation()
            ));
        }

        var value = fieldSymbol.ConstantValue?.ToString();
        if (value is null)
        {
            return Result<string>.Failure(Diagnostic.Create(
                CompilationErrors.IsNotAConstant,
                syntax.GetLocation(),
                fieldSymbol.Name
            ));
        }

        return value;
    }
}
