// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class SetStatusCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.SetStatus);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.StatusConfig>(
            node, context, "set-status");
        
        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var statusElement = new XElement("set-status");
        
        statusElement.Add(new XAttribute("code", config.Code.ToXmlValue()));
        statusElement.Add(new XAttribute("reason", config.Reason.ToXmlValue()));

        context.AddPolicy(statusElement);
    }

    public static void HandleStatus(IDocumentCompilationContext context, XElement element, InitializerValue status)
    {
        if (!status.TryGetValues<StatusConfig>(out var config))
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.PolicyArgumentIsNotOfRequiredType,
                status.Node.GetLocation(),
                $"{element.Name}.set-status",
                nameof(StatusConfig)
            ));
            return;
        }

        var statusElement = new XElement("set-status");

        if (!statusElement.AddAttribute(config, nameof(StatusConfig.Code), "code"))
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.RequiredParameterNotDefined,
                status.Node.GetLocation(),
                $"{element.Name}.set-status",
                nameof(StatusConfig.Code)
            ));
            return;
        }

        statusElement.AddAttribute(config, nameof(StatusConfig.Reason), "reason");
        element.Add(statusElement);
    }
}