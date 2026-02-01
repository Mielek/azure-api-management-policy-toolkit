// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class ValidateHeadersCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IOutboundContext.ValidateHeaders);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<LocalValidateHeadersCompiledConfig>(
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
            element.Add(new XAttribute("errors-variable-name", errorsVar.ToXmlValue()));
        }

        if (config.Headers is { } headerValues)
        {
            HandleHeaders(context, headerValues, element);
        }

        context.AddPolicy(element);
    }

    private static void HandleHeaders(IDocumentCompilationContext context, InitializerValue headerValues,
        XElement element)
    {
        foreach (var headerValue in headerValues.UnnamedValues ?? [])
        {
            if (!headerValue.TryGetValues<ValidateHeader>(out var validateHeaderValues))
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.PolicyArgumentIsNotOfRequiredType,
                    headerValue.Node.GetLocation(),
                    "validate-headers.header",
                    nameof(ValidateHeader)
                ));
                continue;
            }

            XElement header = new("header");
            if (!header.AddAttribute(validateHeaderValues, nameof(ValidateHeader.Name), "name"))
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.RequiredParameterNotDefined,
                    headerValue.Node.GetLocation(),
                    "validate-headers.header",
                    nameof(ValidateHeader.Name)
                ));
                continue;
            }

            if (!header.AddAttribute(validateHeaderValues, nameof(ValidateHeader.Action), "action"))
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.RequiredParameterNotDefined,
                    headerValue.Node.GetLocation(),
                    "validate-headers.header",
                    nameof(ValidateHeader.Action)
                ));
                continue;
            }

            element.Add(header);
        }
    }

    private sealed class LocalValidateHeadersCompiledConfig
    {
        public required ExpressionValue<string> SpecifiedHeaderAction { get; init; }
        public required ExpressionValue<string> UnspecifiedHeaderAction { get; init; }
        public ExpressionValue<string>? ErrorsVariableName { get; init; }
        public InitializerValue? Headers { get; init; }
    }
}