// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class ValidateOdataRequestCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.ValidateOdataRequest);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = ConfigurationExtractor.Extract<ValidateOdataRequestConfig>(node, context, "validate-odata-request");
        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }
        var values = configResult.Value;

        XElement element = new("validate-odata-request");

        element.AddAttribute(values, nameof(ValidateOdataRequestConfig.ErrorVariableName), "error-variable-name");
        element.AddAttribute(values, nameof(ValidateOdataRequestConfig.DefaultOdataVersion), "default-odata-version");
        element.AddAttribute(values, nameof(ValidateOdataRequestConfig.MinOdataVersion), "min-odata-version");
        element.AddAttribute(values, nameof(ValidateOdataRequestConfig.MaxOdataVersion), "max-odata-version");
        element.AddAttribute(values, nameof(ValidateOdataRequestConfig.MaxSize), "max-size");

        context.AddPolicy(element);
    }
}