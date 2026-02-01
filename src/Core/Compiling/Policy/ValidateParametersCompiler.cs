// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class ValidateParametersCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.ValidateParameters);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.ValidateParametersConfig>(
            node, context, "validate-parameters");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        XElement element = new("validate-parameters");

        element.Add(new XAttribute("specified-parameter-action", config.SpecifiedParameterAction.ToXmlValue()));
        element.Add(new XAttribute("unspecified-parameter-action", config.UnspecifiedParameterAction.ToXmlValue()));

        element.TryAddAttribute("errors-variable-name", config.ErrorsVariableName);

        if (config.Headers is { } headers)
        {
            AddHeadersElement(element, headers);
        }

        if (config.Query is { } query)
        {
            AddQueryElement(element, query);
        }

        if (config.Path is { } path)
        {
            AddPathElement(element, path);
        }

        context.AddPolicy(element);
    }

    private static void AddHeadersElement(XElement parentElement, CompiledConfigs.ValidateHeaderParameters headers)
    {
        XElement headersElement = new("headers");

        headersElement.Add(new XAttribute("specified-parameter-action", headers.SpecifiedParameterAction.ToXmlValue()));
        headersElement.Add(new XAttribute("unspecified-parameter-action", headers.UnspecifiedParameterAction.ToXmlValue()));

        AddParameters(headersElement, headers.Parameters);

        parentElement.Add(headersElement);
    }

    private static void AddQueryElement(XElement parentElement, CompiledConfigs.ValidateQueryParameters query)
    {
        XElement queryElement = new("query");

        queryElement.Add(new XAttribute("specified-parameter-action", query.SpecifiedParameterAction.ToXmlValue()));
        queryElement.Add(new XAttribute("unspecified-parameter-action", query.UnspecifiedParameterAction.ToXmlValue()));

        AddParameters(queryElement, query.Parameters);

        parentElement.Add(queryElement);
    }

    private static void AddPathElement(XElement parentElement, CompiledConfigs.ValidatePathParameters path)
    {
        XElement pathElement = new("path");

        pathElement.Add(new XAttribute("specified-parameter-action", path.SpecifiedParameterAction.ToXmlValue()));

        AddParameters(pathElement, path.Parameters);

        parentElement.Add(pathElement);
    }

    private static void AddParameters(XElement parentElement, IReadOnlyList<CompiledConfigs.ValidateParameter>? parameters)
    {
        if (parameters is null)
        {
            return;
        }

        foreach (var param in parameters)
        {
            XElement paramElement = new("parameter");
            paramElement.Add(new XAttribute("name", param.Name));
            paramElement.Add(new XAttribute("action", param.Action.ToXmlValue()));
            parentElement.Add(paramElement);
        }
    }
}