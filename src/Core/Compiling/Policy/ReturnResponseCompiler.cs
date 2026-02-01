// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class ReturnResponseCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.ReturnResponse);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<LocalReturnResponseCompiledConfig>(
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
            element.Add(new XAttribute("response-variable-name", responseVar.ToXmlValue()));
        }

        if (config.Status is { } statusConfig)
        {
            SetStatusCompiler.HandleStatus(context, element, statusConfig);
        }

        if (config.Headers is { } headers)
        {
            BaseSetHeaderCompiler.HandleHeaders(context, element, headers);
        }

        if (config.Body is { } body)
        {
            SetBodyCompiler.HandleBody(context, element, body);
        }

        context.AddPolicy(element);
    }

    private sealed class LocalReturnResponseCompiledConfig
    {
        public ExpressionValue<string>? ResponseVariableName { get; init; }
        public InitializerValue? Status { get; init; }
        public InitializerValue? Headers { get; init; }
        public InitializerValue? Body { get; init; }
    }
}