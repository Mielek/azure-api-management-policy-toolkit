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

public class LlmTokenLimitCompiler()
    : BaseTokenLimitCompiler("llm-token-limit", nameof(IInboundContext.LlmTokenLimit));

public class AzureOpenAiTokenLimitCompiler()
    : BaseTokenLimitCompiler("azure-openai-token-limit", nameof(IInboundContext.AzureOpenAiTokenLimit));

public abstract class BaseTokenLimitCompiler : IMethodPolicyHandler
{
    private readonly string _policyName;

    protected BaseTokenLimitCompiler(string policyName, string methodName)
    {
        _policyName = policyName;
        MethodName = methodName;
    }

    public string MethodName { get; }

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.TokenLimitConfig>(
            node, context, _policyName);

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement(_policyName);

        element.Add(new XAttribute("counter-key", config.CounterKey.ToXmlValue()));
        element.Add(new XAttribute("estimate-prompt-token", config.EstimatePromptToken.ToXmlValue()));

        var tokensPerMinuteAdded = config.TokensPerMinute is not null;
        var quotaAdded = config.TokenQuota is not null;

        if (tokensPerMinuteAdded == quotaAdded)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.OnlyOneOfTwoShouldBeDefined,
                node.GetLocation(),
                "llm-token-limit",
                nameof(TokenLimitConfig.TokensPerMinute),
                nameof(TokenLimitConfig.TokenQuota)
            ));
            return;
        }

        // TokensPerMinute and TokenQuota are mutually exclusive but one is required
        element.AddOptionalAttribute("tokens-per-minute", config.TokensPerMinute);
        element.AddOptionalAttribute("token-quota", config.TokenQuota);
        element.AddOptionalAttribute("token-quota-period", config.TokenQuotaPeriod);
        element.AddOptionalAttribute("retry-after-header-name", config.RetryAfterHeaderName);
        element.AddOptionalAttribute("retry-after-variable-name", config.RetryAfterVariableName);
        element.AddOptionalAttribute("remaining-quota-tokens-header-name", config.RemainingQuotaTokensHeaderName);
        element.AddOptionalAttribute("remaining-quota-tokens-variable-name", config.RemainingQuotaTokensVariableName);
        element.AddOptionalAttribute("remaining-tokens-header-name", config.RemainingTokensHeaderName);
        element.AddOptionalAttribute("remaining-tokens-variable-name", config.RemainingTokensVariableName);
        element.AddOptionalAttribute("tokens-consumed-header-name", config.TokensConsumedHeaderName);
        element.AddOptionalAttribute("tokens-consumed-variable-name", config.TokensConsumedVariableName);

        context.AddPolicy(element);
    }
}