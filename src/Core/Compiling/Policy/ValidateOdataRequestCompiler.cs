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
        element.AddOptionalAttribute("error-variable-name", config.ErrorVariableName);
        element.AddOptionalAttribute("default-odata-version", config.DefaultOdataVersion);
        element.AddOptionalAttribute("min-odata-version", config.MinOdataVersion);
        element.AddOptionalAttribute("max-odata-version", config.MaxOdataVersion);
        element.AddOptionalAttribute("max-size", config.MaxSize);

        context.AddPolicy(element);
    }
}