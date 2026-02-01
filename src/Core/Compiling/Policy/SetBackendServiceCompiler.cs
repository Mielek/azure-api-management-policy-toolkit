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

public class SetBackendServiceCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.SetBackendService);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.SetBackendServiceConfig>(
            node, context, "set-backend-service");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("set-backend-service");

        var baseUrlDefined = config.BaseUrl is not null;
        var backendIdDefined = config.BackendId is not null;
        if (!(baseUrlDefined ^ backendIdDefined))
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.OnlyOneOfTwoShouldBeDefined,
                node.GetLocation(),
                "set-backend-service",
                nameof(SetBackendServiceConfig.BaseUrl),
                nameof(SetBackendServiceConfig.BackendId)
            ));
            return;
        }

        if (config.BaseUrl is { } baseUrl)
        {
            element.Add(new XAttribute("base-url", baseUrl.ToXmlValue()));
        }

        if (config.BackendId is { } backendId)
        {
            element.Add(new XAttribute("backend-id", backendId.ToXmlValue()));
        }

        element.AddOptionalAttribute("sf-resolve-condition", config.SfResolveCondition);
        element.AddOptionalAttribute("sf-service-instance-name", config.SfServiceInstanceName);
        element.AddOptionalAttribute("sf-partition-key", config.SfPartitionKey);
        element.AddOptionalAttribute("sf-listener-name", config.SfListenerName);
        element.AddOptionalAttribute("dapr-app-id", config.DaprAppId);
        element.AddOptionalAttribute("dapr-method", config.DaprMethod);
        element.AddOptionalAttribute("dapr-namespace", config.DaprNamespace);

        context.AddPolicy(element);
    }
}