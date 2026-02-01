// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class ValidateContentCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.ValidateContent);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<CompiledConfigs.ValidateContentConfig>(
            node, context, "validate-content");

        if (!configResult.IsSuccess)
        {
            configResult.ReportAll(context);
            return;
        }

        var config = configResult.Value;
        XElement element = new("validate-content");

        element.AddAttribute("unspecified-content-type-action", config.UnspecifiedContentTypeAction);
        element.AddAttribute("max-size", config.MaxSize);
        element.AddAttribute("size-exceeded-action", config.SizeExceededAction);
        element.AddOptionalAttribute("errors-variable-name", config.ErrorsVariableName);

        if (config.ContentTypeMap is { } contentTypeMap)
        {
            HandleContentTypeMap(contentTypeMap, element);
        }

        if (config.Contents is { } contents)
        {
            HandleContents(contents, element);
        }

        context.AddPolicy(element);
    }

    private static void HandleContentTypeMap(CompiledConfigs.ContentTypeMapConfig contentTypeMap, XElement parentElement)
    {
        XElement mapElement = new("content-type-map");
        mapElement.AddOptionalAttribute("any-content-type-value", contentTypeMap.AnyContentTypeValue);
        mapElement.AddOptionalAttribute("missing-content-type-value", contentTypeMap.MissingContentTypeValue);

        if (contentTypeMap.Types is { } types)
        {
            foreach (var typeMap in types)
            {
                XElement typeElement = new("type");
                typeElement.AddAttribute("to", typeMap.To);
                typeElement.AddOptionalAttribute("from", typeMap.From);
                typeElement.AddOptionalAttribute("when", typeMap.When);

                mapElement.Add(typeElement);
            }
        }

        parentElement.Add(mapElement);
    }

    private static void HandleContents(IReadOnlyList<CompiledConfigs.ValidateContent> contents, XElement parentElement)
    {
        foreach (var validateContent in contents)
        {
            XElement contentElement = new("content");
            contentElement.AddAttribute("validate-as", validateContent.ValidateAs);
            contentElement.AddAttribute("action", validateContent.Action);
            contentElement.AddOptionalAttribute("type", validateContent.Type);
            contentElement.AddOptionalAttribute("schema-id", validateContent.SchemaId);
            contentElement.AddOptionalAttribute("schema-ref", validateContent.SchemaRef);
            
            if (validateContent.AllowAdditionalProperties is { } allowAdditional)
            {
                contentElement.AddAttribute("allow-additional-properties", allowAdditional.ToString().ToLowerInvariant());
            }
            
            if (validateContent.CaseInsensitivePropertyNames is { } caseInsensitive)
            {
                contentElement.AddAttribute("case-insensitive-property-names", caseInsensitive.ToString().ToLowerInvariant());
            }

            parentElement.Add(contentElement);
        }
    }
}