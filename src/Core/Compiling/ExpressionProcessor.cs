// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    /// Processes an expression syntax node and returns an InitializerValue.
    /// For complex initializers (objects, arrays), delegates to InitializerProcessor.
    /// For scalar expressions, uses Process to get the string value.
    /// </summary>
    public static Result<InitializerValue> ProcessToInitializerValue(
        ExpressionSyntax expression,
        ICompilationContext context)
    {
        return expression switch
        {
            ObjectCreationExpressionSyntax config => ProcessObjectCreation(config, context),
            ArrayCreationExpressionSyntax array => ProcessArrayCreation(array, context),
            ImplicitArrayCreationExpressionSyntax array => ProcessImplicitArrayCreation(array, context),
            CollectionExpressionSyntax collection => ProcessCollection(collection, context),
            _ => Process(expression, context).Map(value => InitializerValue.ForScalar(value, expression))
        };
    }

    private static Result<InitializerValue> ProcessObjectCreation(
        ObjectCreationExpressionSyntax creationSyntax,
        ICompilationContext context)
    {
        var diagnostics = new List<Diagnostic>();
        var result = new Dictionary<string, InitializerValue>();

        if (creationSyntax.Initializer is null)
        {
            diagnostics.Add(Diagnostic.Create(
                CompilationErrors.PolicyObjectCreationDoesNotContainInitializerSection,
                creationSyntax.GetLocation()
            ));
        }

        foreach (var expression in creationSyntax.Initializer?.Expressions ?? [])
        {
            if (expression is not AssignmentExpressionSyntax assignment)
            {
                diagnostics.Add(Diagnostic.Create(
                    CompilationErrors.ObjectInitializerContainsNotAnAssigmentExpression,
                    expression.GetLocation()
                ));
                continue;
            }

            var name = assignment.Left.ToString();
            var valueResult = ProcessToInitializerValue(assignment.Right, context);
            
            if (valueResult.IsFailure)
            {
                diagnostics.AddRange(valueResult.Diagnostics);
                continue;
            }

            result[name] = valueResult.Value;
        }

        var typeName = (creationSyntax.Type as IdentifierNameSyntax)?.Identifier.ValueText;

        if (diagnostics.Count > 0 && result.Count == 0)
        {
            return Result<InitializerValue>.Failure(diagnostics);
        }

        return InitializerValue.ForObject(result, typeName ?? string.Empty, creationSyntax);
    }

    private static Result<InitializerValue> ProcessArrayCreation(
        ArrayCreationExpressionSyntax creationSyntax,
        ICompilationContext context)
    {
        var expressions = creationSyntax.Initializer?.Expressions ?? [];
        var diagnostics = new List<Diagnostic>();
        var values = new List<InitializerValue>();

        foreach (var expression in expressions)
        {
            var valueResult = ProcessToInitializerValue(expression, context);
            if (valueResult.IsFailure)
            {
                diagnostics.AddRange(valueResult.Diagnostics);
                continue;
            }
            values.Add(valueResult.Value);
        }

        var typeName = (creationSyntax.Type.ElementType as IdentifierNameSyntax)?.Identifier.ValueText;

        return diagnostics.Count > 0
            ? Result<InitializerValue>.Failure(diagnostics)
            : InitializerValue.ForArray(values, creationSyntax, typeName);
    }

    private static Result<InitializerValue> ProcessImplicitArrayCreation(
        ImplicitArrayCreationExpressionSyntax creationSyntax,
        ICompilationContext context)
    {
        var diagnostics = new List<Diagnostic>();
        var values = new List<InitializerValue>();

        foreach (var expression in creationSyntax.Initializer.Expressions)
        {
            var valueResult = ProcessToInitializerValue(expression, context);
            if (valueResult.IsFailure)
            {
                diagnostics.AddRange(valueResult.Diagnostics);
                continue;
            }
            values.Add(valueResult.Value);
        }

        return diagnostics.Count > 0
            ? Result<InitializerValue>.Failure(diagnostics)
            : InitializerValue.ForArray(values, creationSyntax);
    }

    private static Result<InitializerValue> ProcessCollection(
        CollectionExpressionSyntax collectionSyntax,
        ICompilationContext context)
    {
        var diagnostics = new List<Diagnostic>();
        var values = new List<InitializerValue>();

        var elements = collectionSyntax.Elements
            .OfType<ExpressionElementSyntax>()
            .Select(e => e.Expression);

        foreach (var expression in elements)
        {
            var valueResult = ProcessToInitializerValue(expression, context);
            if (valueResult.IsFailure)
            {
                diagnostics.AddRange(valueResult.Diagnostics);
                continue;
            }
            values.Add(valueResult.Value);
        }

        return diagnostics.Count > 0
            ? Result<InitializerValue>.Failure(diagnostics)
            : InitializerValue.ForArray(values, collectionSyntax);
    }
}
