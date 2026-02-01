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

public class SetBodyCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.SetBody);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var arguments = node.ArgumentList.Arguments;
        if (arguments.Count is > 2 or 0)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.ArgumentCountMissMatchForPolicy,
                node.ArgumentList.GetLocation(),
                "set-body"));
            return;
        }

        var valueResult = ExpressionProcessor.Process(node.ArgumentList.Arguments[0].Expression, context);
        if (!valueResult.IsSuccess)
        {
            valueResult.ReportAll(context);
            return;
        }
        var element = new XElement("set-body", valueResult.Value);
        if (node.ArgumentList.Arguments.Count == 2)
        {
            var configResult = CompiledConfigExtractor.ExtractFromExpression<CompiledConfigs.SetBodyConfig>(
                node.ArgumentList.Arguments[1].Expression, context, "set-body");
            if (!configResult.IsSuccess)
            {
                configResult.ReportAll(context);
                return;
            }
            var config = configResult.Value;

            if (config.Template is { } template)
            {
                if (template != "liquid")
                {
                    context.Report(Diagnostic.Create(
                        CompilationErrors.OnlyOneOfTwoShouldBeDefined,
                        node.ArgumentList.Arguments[1].GetLocation(),
                        "set-body.template",
                        "liquid"
                    ));
                }
                else
                {
                    element.AddAttribute("template", template);
                }
            }

            if (config.XsiNil is { } xsiNil)
            {
                if (xsiNil != "blank" && xsiNil != "null")
                {
                    context.Report(Diagnostic.Create(
                        CompilationErrors.OnlyOneOfTwoShouldBeDefined,
                        node.ArgumentList.Arguments[1].GetLocation(),
                        "set-body.xsi-nil",
                        "blank",
                        "null"
                    ));
                }
                else
                {
                    element.AddAttribute("xsi-nil", xsiNil);
                }
            }

            if (config.ParseDate is { } parseDate)
            {
                element.AddAttribute("parse-date", parseDate.ToString().ToLowerInvariant());
            }
        }

        context.AddPolicy(element);
    }

    public static void HandleBody(XElement element, CompiledConfigs.BodyConfig config)
    {
        // Content is required but nullable ExpressionValue<object>?
        // For required properties, we know the struct is set, so .Value is safe
        if (!config.Content.HasValue)
        {
            return;
        }
        
        var content = config.Content.Value;
        var contentValue = content.IsExpression 
            ? content.Expression 
            : content.ConstantValue?.ToString() ?? string.Empty;
        
        var bodyElement = new XElement("set-body", contentValue);
        bodyElement.AddOptionalAttribute("template", config.Template);
        bodyElement.AddOptionalAttribute("xsi-nil", config.XsiNil);
        
        if (config.ParseDate is { } parseDate)
        {
            bodyElement.AddAttribute("parse-date", parseDate.ToString().ToLowerInvariant());
        }
        
        element.Add(bodyElement);
    }
}