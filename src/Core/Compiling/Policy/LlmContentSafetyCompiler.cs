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

        element.Add(new XAttribute("backend-id", config.BackendId.ToXmlValue()));

        if (config.ShieldPrompt is { } shieldPrompt)
        {
            element.Add(new XAttribute("shield-prompt", shieldPrompt.ToXmlValue()));
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
            categoriesElement.Add(new XAttribute("output-type", outputType.ToXmlValue()));
        }

        if (categories.Categories is { } categoryList)
        {
            foreach (var category in categoryList)
            {
                var categoryElement = new XElement("category");
                categoryElement.Add(new XAttribute("name", category.Name.ToXmlValue()));
                categoryElement.Add(new XAttribute("threshold", category.Threshold.ToXmlValue()));
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
            blockListsElement.Add(new XElement("id", id.ToXmlValue()));
        }

        element.Add(blockListsElement);
    }
}