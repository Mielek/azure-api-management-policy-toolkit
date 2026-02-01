// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class ReturnResponseCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.ReturnResponse);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.ReturnResponseConfig>(
            node, context, "return-response");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("return-response");

        if (config.ResponseVariableName is { } responseVar)
        {
            element.Add(new XAttribute("response-variable-name", responseVar));
        }

        if (config.Status is { } status)
        {
            SetStatusCompiler.HandleStatus(element, status);
        }

        if (config.Headers is { } headers)
        {
            BaseSetHeaderCompiler.HandleHeaders(element, headers);
        }

        if (config.Body is { } body)
        {
            SetBodyCompiler.HandleBody(element, body);
        }

        context.AddPolicy(element);
    }
}