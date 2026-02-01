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

        if (config.TokensPerMinute is { } tokensPerMinute)
        {
            element.Add(new XAttribute("tokens-per-minute", tokensPerMinute.ToXmlValue()));
        }

        if (config.TokenQuota is { } tokenQuota)
        {
            element.Add(new XAttribute("token-quota", tokenQuota.ToXmlValue()));
        }

        if (config.TokenQuotaPeriod is { } tokenQuotaPeriod)
        {
            element.Add(new XAttribute("token-quota-period", tokenQuotaPeriod.ToXmlValue()));
        }

        if (config.RetryAfterHeaderName is { } retryAfterHeaderName)
        {
            element.Add(new XAttribute("retry-after-header-name", retryAfterHeaderName.ToXmlValue()));
        }

        if (config.RetryAfterVariableName is { } retryAfterVariableName)
        {
            element.Add(new XAttribute("retry-after-variable-name", retryAfterVariableName.ToXmlValue()));
        }

        if (config.RemainingQuotaTokensHeaderName is { } remainingQuotaTokensHeaderName)
        {
            element.Add(new XAttribute("remaining-quota-tokens-header-name", remainingQuotaTokensHeaderName.ToXmlValue()));
        }

        if (config.RemainingQuotaTokensVariableName is { } remainingQuotaTokensVariableName)
        {
            element.Add(new XAttribute("remaining-quota-tokens-variable-name", remainingQuotaTokensVariableName.ToXmlValue()));
        }

        if (config.RemainingTokensHeaderName is { } remainingTokensHeaderName)
        {
            element.Add(new XAttribute("remaining-tokens-header-name", remainingTokensHeaderName.ToXmlValue()));
        }

        if (config.RemainingTokensVariableName is { } remainingTokensVariableName)
        {
            element.Add(new XAttribute("remaining-tokens-variable-name", remainingTokensVariableName.ToXmlValue()));
        }

        if (config.TokensConsumedHeaderName is { } tokensConsumedHeaderName)
        {
            element.Add(new XAttribute("tokens-consumed-header-name", tokensConsumedHeaderName.ToXmlValue()));
        }

        if (config.TokensConsumedVariableName is { } tokensConsumedVariableName)
        {
            element.Add(new XAttribute("tokens-consumed-variable-name", tokensConsumedVariableName.ToXmlValue()));
        }

        context.AddPolicy(element);
    }
}