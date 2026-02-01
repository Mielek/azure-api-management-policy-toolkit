// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class LlmContentSafetyCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.LlmContentSafety);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<LocalLlmContentSafetyCompiledConfig>(
            node, context, "llm-content-safety");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("llm-content-safety");

        element.Add(new XAttribute("backend-id", config.BackendId.ToXmlValue()));

        if (config.ShieldPrompt is { } shieldPrompt)
        {
            element.Add(new XAttribute("shield-prompt", shieldPrompt.ToXmlValue()));
        }

        if (config.Categories is { } categoriesValue)
        {
            HandleCategories(context, categoriesValue, element);
        }

        if (config.BlockLists is { } blockListsValue)
        {
            HandleBlockLists(blockListsValue, element);
        }

        context.AddPolicy(element);
    }

    private static void HandleCategories(IDocumentCompilationContext context, InitializerValue categoriesValue,
        XElement parent)
    {
        if (!categoriesValue.TryGetValues<ContentSafetyCategories>(out var categories))
        {
            return;
        }

        var categoriesElement = new XElement("categories");

        categoriesElement.AddAttribute(categories, nameof(ContentSafetyCategories.OutputType), "output-type");

        if (categories.TryGetValue(nameof(ContentSafetyCategories.Categories), out var categoryValues))
        {
            HandleCategory(context, categoryValues, categoriesElement);
        }

        parent.Add(categoriesElement);
    }

    private static void HandleCategory(IDocumentCompilationContext context, InitializerValue categoryValues,
        XElement categoriesElement)
    {
        foreach (var categoryValue in categoryValues.UnnamedValues ?? [])
        {
            if (!categoryValue.TryGetValues<ContentSafetyCategory>(out var category))
            {
                continue;
            }

            var categoryElement = new XElement("category");
            if (!categoryElement.AddAttribute(category, nameof(ContentSafetyCategory.Name), "name") ||
                !categoryElement.AddAttribute(category, nameof(ContentSafetyCategory.Threshold), "threshold"))
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.RequiredParameterNotDefined,
                    categoryValue.Node.GetLocation(),
                    "llm-content-safety.category",
                    nameof(ContentSafetyCategory)
                ));
                continue;
            }

            categoriesElement.Add(categoryElement);
        }
    }

    private static void HandleBlockLists(InitializerValue blockListsValue, XElement element)
    {
        if (!blockListsValue.TryGetValues<ContentSafetyBlockLists>(out var blockLists))
        {
            return;
        }

        if (!blockLists.TryGetValue(nameof(ContentSafetyBlockLists.Ids), out var idsValue))
        {
            return;
        }

        var blockListsElement = new XElement("block-lists");

        foreach (var idValue in idsValue.UnnamedValues ?? [])
        {
            blockListsElement.Add(new XElement("id", idValue.Value));
        }

        element.Add(blockListsElement);
    }

    private sealed class LocalLlmContentSafetyCompiledConfig
    {
        public required ExpressionValue<string> BackendId { get; init; }
        public ExpressionValue<bool>? ShieldPrompt { get; init; }
        public InitializerValue? Categories { get; init; }
        public InitializerValue? BlockLists { get; init; }
    }
}