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

public class ForwardRequestCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IBackendContext.ForwardRequest);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        if (node.ArgumentList.Arguments.Count > 1)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.ArgumentCountMissMatchForPolicy,
                node.ArgumentList.GetLocation(),
                "forward-request"
            ));
            return;
        }

        var element = new XElement("forward-request");
        if (node.ArgumentList.Arguments.Count == 1)
        {
            var configResult = CompiledConfigExtractor.ExtractFromExpression<CompiledConfigs.ForwardRequestConfig>(
                node.ArgumentList.Arguments[0].Expression, context, "forward-request");

            if (!configResult.IsSuccess)
            {
                configResult.ReportAll(context);
                return;
            }

            var config = configResult.Value;

            // Validate mutual exclusion of Timeout and TimeoutMs
            if (config.Timeout is not null && config.TimeoutMs is not null)
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.OnlyOneOfTwoShouldBeDefined,
                    node.ArgumentList.Arguments[0].GetLocation(),
                    "forward-request",
                    nameof(ForwardRequestConfig.Timeout),
                    nameof(ForwardRequestConfig.TimeoutMs)
                ));
            }

            element.AddOptionalAttribute("timeout", config.Timeout);
            element.AddOptionalAttribute("timeout-ms", config.TimeoutMs);
            element.AddOptionalAttribute("continue-timeout", config.ContinueTimeout);
            element.AddOptionalAttribute("http-version", config.HttpVersion);
            element.AddOptionalAttribute("follow-redirects", config.FollowRedirects);
            element.AddOptionalAttribute("buffer-request-body", config.BufferRequestBody);
            element.AddOptionalAttribute("buffer-response", config.BufferResponse);
            element.AddOptionalAttribute("fail-on-error-status-code", config.FailOnErrorStatusCode);
        }

        context.AddPolicy(element);
    }
}