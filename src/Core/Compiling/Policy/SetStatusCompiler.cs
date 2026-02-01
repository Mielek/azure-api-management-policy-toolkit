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
        
        statusElement.AddAttribute("code", config.Code);
        statusElement.AddOptionalAttribute("reason", config.Reason);

        context.AddPolicy(statusElement);
    }

    public static void HandleStatus(XElement element, CompiledConfigs.StatusConfig status)
    {
        var statusElement = new XElement("set-status");
        statusElement.AddAttribute("code", status.Code);
        statusElement.AddOptionalAttribute("reason", status.Reason);
        
        element.Add(statusElement);
    }
}