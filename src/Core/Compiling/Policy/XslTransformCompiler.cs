// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml;
using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class XslTransformCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.XslTransform);

    private sealed class LocalXslTransformCompiledConfig
    {
        public required ExpressionValue<string> StyleSheet { get; init; }
        public InitializerValue? Parameters { get; init; }
    }

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<LocalXslTransformCompiledConfig>(
            node, context, "xsl-transform");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("xsl-transform");

        if (config.Parameters is { } parameters)
        {
            HandleParameters(context, parameters, element);
        }

        try
        {
            var xml = XElement.Parse(config.StyleSheet.ToXmlValue());
            element.Add(xml);
        }
        catch (XmlException ex)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.RequiredParameterHasXmlErrors,
                node.GetLocation(),
                "xsl-transform",
                nameof(XslTransformConfig.StyleSheet),
                ex.ToString()
            ));
        }

        context.AddPolicy(element);
    }

    private static void HandleParameters(IDocumentCompilationContext context, InitializerValue parametersValue,
        XElement parentElement)
    {
        foreach (var paramValue in parametersValue.UnnamedValues ?? [])
        {
            if (paramValue.Node is not ExpressionSyntax paramExpression)
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.PolicyArgumentIsNotOfRequiredType,
                    paramValue.Node.GetLocation(),
                    "xsl-transform.parameter",
                    nameof(XslTransformParameter)
                ));
                continue;
            }

            var configResult = CompiledConfigExtractor.ExtractFromExpression<CompiledConfigs.XslTransformParameter>(
                paramExpression, context, "xsl-transform.parameter");

            if (!configResult.IsSuccess)
            {
                configResult.ReportAll(context);
                continue;
            }

            var config = configResult.Value;
            var paramElement = new XElement("parameter");
            paramElement.Add(new XAttribute("name", config.Name.ToXmlValue()));
            paramElement.Value = config.Value.ToXmlValue();
            parentElement.Add(paramElement);
        }
    }
}