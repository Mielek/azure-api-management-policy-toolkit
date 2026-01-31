// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling;

/// <summary>
/// Pure processor for extracting values from initializer expressions.
/// No context parameter required - operates solely on syntax.
/// </summary>
public static class InitializerProcessor
{
    /// <summary>
    /// Processes an object creation expression and extracts its initializer values.
    /// </summary>
    public static Result<InitializerValue> Process(ObjectCreationExpressionSyntax creationSyntax)
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
            var valueResult = ProcessExpression(assignment.Right);
            
            if (valueResult.IsFailure)
            {
                diagnostics.AddRange(valueResult.Diagnostics);
                continue;
            }

            result[name] = valueResult.Value;
        }

        var typeName = (creationSyntax.Type as IdentifierNameSyntax)?.Identifier.ValueText;

        // Return success even with diagnostics - the object was processed
        // Diagnostics are for individual property failures
        if (diagnostics.Count > 0 && result.Count == 0)
        {
            return Result<InitializerValue>.Failure(diagnostics);
        }

        var initializerValue = InitializerValue.ForObject(result, typeName ?? string.Empty, creationSyntax);
        return diagnostics.Count > 0
            ? Result<InitializerValue>.Success(initializerValue)
            : initializerValue;
    }

    /// <summary>
    /// Processes an array creation expression and extracts its element values.
    /// </summary>
    public static Result<InitializerValue> Process(ArrayCreationExpressionSyntax creationSyntax)
    {
        var expressions = creationSyntax.Initializer?.Expressions ?? [];
        var diagnostics = new List<Diagnostic>();
        var values = new List<InitializerValue>();

        foreach (var expression in expressions)
        {
            var valueResult = ProcessExpression(expression);
            if (valueResult.IsFailure)
            {
                diagnostics.AddRange(valueResult.Diagnostics);
                continue;
            }
            values.Add(valueResult.Value);
        }

        var typeName = (creationSyntax.Type.ElementType as IdentifierNameSyntax)?.Identifier.ValueText;
        var initializerValue = InitializerValue.ForArray(values, creationSyntax, typeName);

        return diagnostics.Count > 0
            ? Result<InitializerValue>.Failure(diagnostics)
            : initializerValue;
    }

    /// <summary>
    /// Processes a collection expression and extracts its element values.
    /// </summary>
    public static Result<InitializerValue> Process(CollectionExpressionSyntax collectionSyntax)
    {
        var diagnostics = new List<Diagnostic>();
        var values = new List<InitializerValue>();

        var elements = collectionSyntax.Elements
            .OfType<ExpressionElementSyntax>()
            .Select(e => e.Expression);

        foreach (var expression in elements)
        {
            var valueResult = ProcessExpression(expression);
            if (valueResult.IsFailure)
            {
                diagnostics.AddRange(valueResult.Diagnostics);
                continue;
            }
            values.Add(valueResult.Value);
        }

        var initializerValue = InitializerValue.ForArray(values, collectionSyntax);

        return diagnostics.Count > 0
            ? Result<InitializerValue>.Failure(diagnostics)
            : initializerValue;
    }

    /// <summary>
    /// Processes an implicit array creation expression and extracts its element values.
    /// </summary>
    public static Result<InitializerValue> Process(ImplicitArrayCreationExpressionSyntax creationSyntax)
    {
        var diagnostics = new List<Diagnostic>();
        var values = new List<InitializerValue>();

        foreach (var expression in creationSyntax.Initializer.Expressions)
        {
            var valueResult = ProcessExpression(expression);
            if (valueResult.IsFailure)
            {
                diagnostics.AddRange(valueResult.Diagnostics);
                continue;
            }
            values.Add(valueResult.Value);
        }

        var initializerValue = InitializerValue.ForArray(values, creationSyntax);

        return diagnostics.Count > 0
            ? Result<InitializerValue>.Failure(diagnostics)
            : initializerValue;
    }

    /// <summary>
    /// Routes to the appropriate processor based on expression type.
    /// For literal expressions, returns a scalar InitializerValue.
    /// For complex expressions that need semantic analysis (invocations, member access),
    /// returns a placeholder that will be resolved by ExpressionProcessor.
    /// </summary>
    public static Result<InitializerValue> ProcessExpression(ExpressionSyntax expression)
    {
        return expression switch
        {
            ObjectCreationExpressionSyntax config => Process(config),
            ArrayCreationExpressionSyntax array => Process(array),
            ImplicitArrayCreationExpressionSyntax array => Process(array),
            CollectionExpressionSyntax collection => Process(collection),
            LiteralExpressionSyntax literal => InitializerValue.ForScalar(literal.Token.ValueText, expression),
            // For expressions that need semantic analysis, create a placeholder
            // The actual value resolution happens in ExpressionProcessor
            InvocationExpressionSyntax or MemberAccessExpressionSyntax =>
                new InitializerValue { Node = expression, Value = null },
            _ => Result<InitializerValue>.Failure(Diagnostic.Create(
                CompilationErrors.NotSupportedParameter,
                expression.GetLocation()
            ))
        };
    }
}
