// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class CacheRemoveValueCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.CacheRemoveValue);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = ConfigurationExtractor.Extract<CacheRemoveValueConfig>(node, context, "cache-remove-value");
        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }
        var values = configResult.Value;

        var element = new XElement("cache-remove-value");

        if (!element.AddAttribute(values, nameof(CacheRemoveValueConfig.Key), "key"))
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.RequiredParameterNotDefined,
                node.GetLocation(),
                "cache-remove-value",
                nameof(CacheRemoveValueConfig.Key)
            ));
            return;
        }

        element.AddAttribute(values, nameof(CacheRemoveValueConfig.CachingType), "caching-type");

        context.AddPolicy(element);
    }
}