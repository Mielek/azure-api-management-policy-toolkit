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

        element.Add(new XAttribute("name", config.Name.ToXmlValue()));
        element.Add(new XAttribute("failed-check-httpcode", config.FailCheckHttpCode.ToXmlValue()));
        element.Add(new XAttribute("failed-check-error-message", config.FailCheckErrorMessage.ToXmlValue()));
        element.Add(new XAttribute("ignore-case", config.IgnoreCase.ToXmlValue()));

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
            element.Add(new XElement("value", value.ToXmlValue()));
        }

        context.AddPolicy(element);
    }
}