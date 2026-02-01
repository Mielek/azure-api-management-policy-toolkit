using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class ValidateStatusCodeCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IOutboundContext.ValidateStatusCode);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.ValidateStatusCodeConfig>(
            node, context, "validate-status-code");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("validate-status-code");

        element.AddAttribute("unspecified-status-code-action", config.UnspecifiedStatusCodeAction);
        element.AddOptionalAttribute("error-variable-name", config.ErrorVariableName);

        if (config.StatusCodes is { } statusCodes)
        {
            foreach (var statusCode in statusCodes)
            {
                var statusCodeElement = new XElement("status-code");
                statusCodeElement.AddAttribute("code", statusCode.Code);
                statusCodeElement.AddAttribute("action", statusCode.Action);
                element.Add(statusCodeElement);
            }
        }

        context.AddPolicy(element);
    }
}