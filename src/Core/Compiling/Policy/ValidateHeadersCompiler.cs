// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class ValidateHeadersCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IOutboundContext.ValidateHeaders);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.ValidateHeadersConfig>(
            node, context, "validate-headers");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        XElement element = new("validate-headers");

        element.Add(new XAttribute("specified-header-action", config.SpecifiedHeaderAction.ToXmlValue()));
        element.Add(new XAttribute("unspecified-header-action", config.UnspecifiedHeaderAction.ToXmlValue()));

        if (config.ErrorsVariableName is { } errorsVar)
        {
            element.Add(new XAttribute("errors-variable-name", errorsVar));
        }

        if (config.Headers is { } headers)
        {
            foreach (var header in headers)
            {
                XElement headerElement = new("header");
                headerElement.Add(new XAttribute("name", header.Name));
                headerElement.Add(new XAttribute("action", header.Action.ToXmlValue()));
                element.Add(headerElement);
            }
        }

        context.AddPolicy(element);
    }
}