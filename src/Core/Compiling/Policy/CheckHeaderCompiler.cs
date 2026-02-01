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

public class CheckHeaderCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.CheckHeader);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.CheckHeaderConfig>(
            node, context, "check-header");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("check-header");

        element.AddAttribute("name", config.Name);
        element.AddAttribute("failed-check-httpcode", config.FailCheckHttpCode);
        element.AddAttribute("failed-check-error-message", config.FailCheckErrorMessage);
        element.AddAttribute("ignore-case", config.IgnoreCase);

        var values = config.Values;
        if (values.Count == 0)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.RequiredParameterIsEmpty,
                node.GetLocation(),
                "check-header",
                nameof(CheckHeaderConfig.Values)
            ));
            return;
        }

        foreach (var value in values)
        {
            element.AddElement("value", value);
        }

        context.AddPolicy(element);
    }
}