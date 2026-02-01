using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class ValidateStatusCodeCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IOutboundContext.ValidateStatusCode);

    private sealed class LocalValidateStatusCodeConfigCompiledConfig
    {
        public required ExpressionValue<string> UnspecifiedStatusCodeAction { get; init; }
        public ExpressionValue<string>? ErrorVariableName { get; init; }
        public InitializerValue? StatusCodes { get; init; }
    }

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<LocalValidateStatusCodeConfigCompiledConfig>(
            node, context, "validate-status-code");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("validate-status-code");

        element.Add(new XAttribute("unspecified-status-code-action", config.UnspecifiedStatusCodeAction.ToXmlValue()));

        if (config.ErrorVariableName is { } errorVariableName)
        {
            element.Add(new XAttribute("error-variable-name", errorVariableName.ToXmlValue()));
        }

        if (config.StatusCodes is { } statusCodes)
        {
            HandleStatusCodes(context, statusCodes, element);
        }

        context.AddPolicy(element);
    }

    private static void HandleStatusCodes(IDocumentCompilationContext context, InitializerValue statusCodesValue,
        XElement parentElement)
    {
        foreach (var statusCodeValue in statusCodesValue.UnnamedValues ?? [])
        {
            if (statusCodeValue.Node is not ExpressionSyntax statusCodeExpression)
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.PolicyArgumentIsNotOfRequiredType,
                    statusCodeValue.Node.GetLocation(),
                    "validate-status-code.status-code",
                    nameof(ValidateStatusCode)
                ));
                continue;
            }

            var configResult = CompiledConfigExtractor.ExtractFromExpression<CompiledConfigs.ValidateStatusCode>(
                statusCodeExpression, context, "validate-status-code.status-code");

            if (!configResult.IsSuccess)
            {
                configResult.ReportAll(context);
                continue;
            }

            var config = configResult.Value;
            var statusCodeElement = new XElement("status-code");
            statusCodeElement.Add(new XAttribute("code", config.Code.ToXmlValue()));
            statusCodeElement.Add(new XAttribute("action", config.Action.ToXmlValue()));
            parentElement.Add(statusCodeElement);
        }
    }
}