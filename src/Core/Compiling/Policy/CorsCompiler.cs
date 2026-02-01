// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class CorsCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.Cors);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<LocalCorsCompiledConfig>(
            node, context, "cors");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("cors");

        if (config.AllowCredentials is { } allowCredentials)
        {
            element.Add(new XAttribute("allow-credentials", allowCredentials.ToXmlValue()));
        }

        if (config.TerminateUnmatchedRequest is { } terminate)
        {
            element.Add(new XAttribute("terminate-unmatched-request", terminate.ToXmlValue()));
        }

        var origins = (config.AllowedOrigins.UnnamedValues ?? [])
            .Select(origin => new XElement("origin", origin.Value!))
            .ToArray<object>();
        if (origins.Length == 0)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.RequiredParameterIsEmpty,
                config.AllowedOrigins.Node.GetLocation(),
                "cors",
                nameof(CorsConfig.AllowedOrigins)
            ));
            return;
        }

        element.Add(new XElement("allowed-origins", origins));

        var headers = (config.AllowedHeaders.UnnamedValues ?? [])
            .Select(origin => new XElement("header", origin.Value!))
            .ToArray<object>();
        if (headers.Length == 0)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.RequiredParameterIsEmpty,
                config.AllowedHeaders.Node.GetLocation(),
                "cors",
                nameof(CorsConfig.AllowedHeaders)
            ));
            return;
        }

        element.Add(new XElement("allowed-headers", headers));

        if (config.AllowedMethods is { } allowedMethods)
        {
            var allowedMethodsElement = new XElement("allowed-methods");
            if (config.PreflightResultMaxAge is { } maxAge)
            {
                allowedMethodsElement.Add(new XAttribute("preflight-result-max-age", maxAge.ToXmlValue()));
            }

            var methods = (allowedMethods.UnnamedValues ?? [])
                .Select(m => new XElement("method", m.Value!))
                .ToArray<object>();
            if (methods.Length == 0)
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.RequiredParameterIsEmpty,
                    allowedMethods.Node.GetLocation(),
                    "cors",
                    nameof(CorsConfig.AllowedMethods)
                ));
            }

            allowedMethodsElement.Add(methods);
            element.Add(allowedMethodsElement);
        }

        if (config.ExposeHeaders is { } exposeHeaders)
        {
            var exposeHeadersElements = (exposeHeaders.UnnamedValues ?? [])
                .Select(h => new XElement("header", h.Value!))
                .ToArray<object>();
            if (exposeHeadersElements.Length == 0)
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.RequiredParameterIsEmpty,
                    exposeHeaders.Node.GetLocation(),
                    "cors",
                    nameof(CorsConfig.ExposeHeaders)
                ));
            }

            element.Add(new XElement("expose-headers", exposeHeadersElements));
        }

        context.AddPolicy(element);
    }

    private sealed class LocalCorsCompiledConfig
    {
        public ExpressionValue<bool>? AllowCredentials { get; init; }
        public ExpressionValue<string>? TerminateUnmatchedRequest { get; init; }
        public required InitializerValue AllowedOrigins { get; init; }
        public InitializerValue? AllowedMethods { get; init; }
        public ExpressionValue<uint>? PreflightResultMaxAge { get; init; }
        public required InitializerValue AllowedHeaders { get; init; }
        public InitializerValue? ExposeHeaders { get; init; }
    }
}