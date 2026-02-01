// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class LlmContentSafetyCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.LlmContentSafety);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.LlmContentSafetyConfig>(
            node, context, "llm-content-safety");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        var element = new XElement("llm-content-safety");

        element.AddAttribute("backend-id", config.BackendId);

        if (config.ShieldPrompt is { } shieldPrompt)
        {
            element.AddAttribute("shield-prompt", shieldPrompt);
        }

        if (config.Categories is { } categories)
        {
            HandleCategories(categories, element);
        }

        if (config.BlockLists is { } blockLists)
        {
            HandleBlockLists(blockLists, element);
        }

        context.AddPolicy(element);
    }

    private static void HandleCategories(CompiledConfigs.ContentSafetyCategories categories, XElement parent)
    {
        var categoriesElement = new XElement("categories");

        if (categories.OutputType is { } outputType)
        {
            categoriesElement.AddAttribute("output-type", outputType);
        }

        if (categories.Categories is { } categoryList)
        {
            foreach (var category in categoryList)
            {
                var categoryElement = new XElement("category");
                categoryElement.AddAttribute("name", category.Name);
                categoryElement.AddAttribute("threshold", category.Threshold);
                categoriesElement.Add(categoryElement);
            }
        }

        parent.Add(categoriesElement);
    }

    private static void HandleBlockLists(CompiledConfigs.ContentSafetyBlockLists blockLists, XElement element)
    {
        var blockListsElement = new XElement("block-lists");

        foreach (var id in blockLists.Ids)
        {
            blockListsElement.AddElement("id", id);
        }

        element.Add(blockListsElement);
    }
}