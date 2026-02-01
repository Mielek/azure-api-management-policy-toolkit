// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class CheckHeaderCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.CheckHeader);

    private sealed class LocalCheckHeaderCompiledConfig
    {
        public required ExpressionValue<string> Name { get; init; }
        public required ExpressionValue<int> FailCheckHttpCode { get; init; }
        public required ExpressionValue<string> FailCheckErrorMessage { get; init; }
        public required ExpressionValue<bool> IgnoreCase { get; init; }
        public required InitializerValue Values { get; init; }
    }

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<LocalCheckHeaderCompiledConfig>(
            node, context, "check-header");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("check-header");

        element.Add(new XAttribute("name", config.Name.ToXmlValue()));
        element.Add(new XAttribute("failed-check-httpcode", config.FailCheckHttpCode.ToXmlValue()));
        element.Add(new XAttribute("failed-check-error-message", config.FailCheckErrorMessage.ToXmlValue()));
        element.Add(new XAttribute("ignore-case", config.IgnoreCase.ToXmlValue()));

        var values = config.Values.UnnamedValues ?? [];
        if (values.Count == 0)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.RequiredParameterIsEmpty,
                config.Values.Node.GetLocation(),
                "check-header",
                nameof(CheckHeaderConfig.Values)
            ));
            return;
        }

        foreach (var value in values)
        {
            element.Add(new XElement("value", value.Value!));
        }

        context.AddPolicy(element);
    }
}