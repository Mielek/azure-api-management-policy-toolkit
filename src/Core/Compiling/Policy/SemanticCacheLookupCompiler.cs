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

public class LlmSemanticCacheLookupCompiler()
    : BaseSemanticCacheLookupCompiler(nameof(IInboundContext.LlmSemanticCacheLookup), "llm-semantic-cache-lookup");

public class AzureOpenAiSemanticCacheLookupCompiler()
    : BaseSemanticCacheLookupCompiler(nameof(IInboundContext.AzureOpenAiSemanticCacheLookup),
        "azure-openai-semantic-cache-lookup");

public abstract class BaseSemanticCacheLookupCompiler : IMethodPolicyHandler
{
    private readonly string _policyName;
    public string MethodName { get; }

    protected BaseSemanticCacheLookupCompiler(string methodName, string policyName)
    {
        MethodName = methodName;
        _policyName = policyName;
    }

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.SemanticCacheLookupConfig>(
            node, context, _policyName);

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement(_policyName);

        element.Add(new XAttribute("score-threshold", config.ScoreThreshold.ToXmlValue()));
        element.Add(new XAttribute("embeddings-backend-id", config.EmbeddingsBackendId.ToXmlValue()));
        element.Add(new XAttribute("embeddings-backend-auth", config.EmbeddingsBackendAuth.ToXmlValue()));
        element.AddOptionalAttribute("ignore-system-messages", config.IgnoreSystemMessages);
        element.AddOptionalAttribute("max-message-count", config.MaxMessageCount);

        if (config.VaryBy is not null)
        {
            foreach (var varyBy in config.VaryBy)
            {
                element.Add(new XElement("vary-by", varyBy.ToXmlValue()));
            }
        }

        context.AddPolicy(element);
    }
}