// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling;

/// <summary>
/// Central dispatcher for processing expression syntax into string values.
/// Routes to appropriate handlers based on expression type.
/// </summary>
public static class ExpressionProcessor
{
    /// <summary>
    /// Processes an expression syntax node and returns its string representation.
    /// Routes to CodeExtractor for expressions requiring semantic analysis,
    /// or handles literals directly.
    /// </summary>
    public static Result<string> Process(ExpressionSyntax expression, ICompilationContext context)
    {
        return expression switch
        {
            LiteralExpressionSyntax literal => literal.Token.ValueText,
            InvocationExpressionSyntax invocation => CodeExtractor.Extract(invocation, context),
            MemberAccessExpressionSyntax memberAccess => CodeExtractor.Extract(memberAccess, context),
            _ => Result<string>.Failure(Diagnostic.Create(
                CompilationErrors.NotSupportedParameter,
                expression.GetLocation()
            ))
        };
    }

    /// <summary>
    /// Processes an expression syntax node and returns an ExpressionValue&lt;T&gt;.
    /// For literals, returns a constant value. For expressions, returns an expression value.
    /// </summary>
    public static Result<ExpressionValue<T>> ProcessToExpressionValue<T>(
        ExpressionSyntax expression,
        ICompilationContext context)
    {
        return expression switch
        {
            LiteralExpressionSyntax literal => ProcessLiteralToExpressionValue<T>(literal),
            InvocationExpressionSyntax invocation => ProcessCodeToExpressionValue<T>(invocation, context),
            MemberAccessExpressionSyntax memberAccess => ProcessCodeToExpressionValue<T>(memberAccess, context),
            _ => Result<ExpressionValue<T>>.Failure(Diagnostic.Create(
                CompilationErrors.NotSupportedParameter,
                expression.GetLocation()
            ))
        };
    }

    private static Result<ExpressionValue<T>> ProcessLiteralToExpressionValue<T>(LiteralExpressionSyntax literal)
    {
        var value = literal.Token.Value;
        if (value is T typedValue)
        {
            return ExpressionValue<T>.FromConstant(typedValue);
        }

        // Try to convert the value
        try
        {
            if (typeof(T) == typeof(string))
            {
                return ExpressionValue<T>.FromConstant((T)(object)literal.Token.ValueText);
            }

            // Special case: byte[] properties represent base64-encoded strings in XML
            // When a string literal is provided for byte[], convert it to byte[] via UTF8 encoding
            if (typeof(T) == typeof(byte[]) && value is string stringValue)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(stringValue);
                return ExpressionValue<T>.FromConstant((T)(object)bytes);
            }
            
            var converted = Convert.ChangeType(value, typeof(T));
            return ExpressionValue<T>.FromConstant((T)converted!);
        }
        catch
        {
            return Result<ExpressionValue<T>>.Failure(Diagnostic.Create(
                CompilationErrors.NotSupportedParameter,
                literal.GetLocation()
            ));
        }
    }

    private static Result<ExpressionValue<T>> ProcessCodeToExpressionValue<T>(
        InvocationExpressionSyntax invocation,
        ICompilationContext context)
    {
        var codeResult = CodeExtractor.Extract(invocation, context);
        return codeResult.Map(code => ExpressionValue<T>.FromExpression(code));
    }

    private static Result<ExpressionValue<T>> ProcessCodeToExpressionValue<T>(
        MemberAccessExpressionSyntax memberAccess,
        ICompilationContext context)
    {
        var codeResult = CodeExtractor.Extract(memberAccess, context);
        return codeResult.Map(code => ExpressionValue<T>.FromExpression(code));
    }
}
