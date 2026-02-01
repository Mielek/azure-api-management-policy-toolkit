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

public class LogToEventHubCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.LogToEventHub);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.LogToEventHubConfig>(
            node, context, "log-to-eventhub");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("log-to-eventhub");

        element.Add(new XAttribute("logger-id", config.LoggerId.ToXmlValue()));

        var addedPartitionKey = config.PartitionKey is not null;
        var addedPartitionId = config.PartitionId is not null;

        if (addedPartitionKey && addedPartitionId)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.OnlyOneOfTwoShouldBeDefined,
                node.GetLocation(),
                "log-to-eventhub",
                nameof(LogToEventHubConfig.PartitionKey),
                nameof(LogToEventHubConfig.PartitionId)
            ));
            return;
        }

        if (config.PartitionKey is { } partitionKey)
        {
            element.Add(new XAttribute("partition-key", partitionKey.ToXmlValue()));
        }

        if (config.PartitionId is { } partitionId)
        {
            element.Add(new XAttribute("partition-id", partitionId.ToXmlValue()));
        }

        element.Add(config.Value.ToXmlValue());

        context.AddPolicy(element);
    }
}