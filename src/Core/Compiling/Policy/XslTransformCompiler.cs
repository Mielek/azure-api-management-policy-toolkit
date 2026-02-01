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

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.XslTransformConfig>(
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
            foreach (var param in parameters)
            {
                var paramElement = new XElement("parameter");
                paramElement.AddAttribute("name", param.Name);
                paramElement.Value = param.Value.ToXmlValue();
                element.Add(paramElement);
            }
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
}