// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class ValidateOdataRequestCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.ValidateOdataRequest);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.ValidateOdataRequestConfig>(
            node, context, "validate-odata-request");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        XElement element = new("validate-odata-request");

        if (config.ErrorVariableName is { } errorVariableName)
        {
            element.Add(new XAttribute("error-variable-name", errorVariableName.ToXmlValue()));
        }

        if (config.DefaultOdataVersion is { } defaultOdataVersion)
        {
            element.Add(new XAttribute("default-odata-version", defaultOdataVersion.ToXmlValue()));
        }

        if (config.MinOdataVersion is { } minOdataVersion)
        {
            element.Add(new XAttribute("min-odata-version", minOdataVersion.ToXmlValue()));
        }

        if (config.MaxOdataVersion is { } maxOdataVersion)
        {
            element.Add(new XAttribute("max-odata-version", maxOdataVersion.ToXmlValue()));
        }

        if (config.MaxSize is { } maxSize)
        {
            element.Add(new XAttribute("max-size", maxSize.ToXmlValue()));
        }

        context.AddPolicy(element);
    }
}