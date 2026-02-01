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

        element.Add(new XAttribute("unspecified-content-type-action", config.UnspecifiedContentTypeAction.ToXmlValue()));
        element.Add(new XAttribute("max-size", config.MaxSize.ToXmlValue()));
        element.Add(new XAttribute("size-exceeded-action", config.SizeExceededAction.ToXmlValue()));

        if (config.ErrorsVariableName is { } errorsVar)
        {
            element.Add(new XAttribute("errors-variable-name", errorsVar));
        }

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
        
        if (contentTypeMap.AnyContentTypeValue is { } anyContentType)
        {
            mapElement.Add(new XAttribute("any-content-type-value", anyContentType));
        }
        
        if (contentTypeMap.MissingContentTypeValue is { } missingContentType)
        {
            mapElement.Add(new XAttribute("missing-content-type-value", missingContentType));
        }

        if (contentTypeMap.Types is { } types)
        {
            foreach (var typeMap in types)
            {
                XElement typeElement = new("type");
                typeElement.Add(new XAttribute("to", typeMap.To));
                
                if (typeMap.From is { } from)
                {
                    typeElement.Add(new XAttribute("from", from));
                }
                
                if (typeMap.When is { } when)
                {
                    typeElement.Add(new XAttribute("when", when.ToXmlValue()));
                }

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
            contentElement.Add(new XAttribute("validate-as", validateContent.ValidateAs));
            contentElement.Add(new XAttribute("action", validateContent.Action.ToXmlValue()));
            
            if (validateContent.Type is { } type)
            {
                contentElement.Add(new XAttribute("type", type));
            }
            
            if (validateContent.SchemaId is { } schemaId)
            {
                contentElement.Add(new XAttribute("schema-id", schemaId));
            }
            
            if (validateContent.SchemaRef is { } schemaRef)
            {
                contentElement.Add(new XAttribute("schema-ref", schemaRef));
            }
            
            if (validateContent.AllowAdditionalProperties is { } allowAdditional)
            {
                contentElement.Add(new XAttribute("allow-additional-properties", allowAdditional.ToString().ToLowerInvariant()));
            }
            
            if (validateContent.CaseInsensitivePropertyNames is { } caseInsensitive)
            {
                contentElement.Add(new XAttribute("case-insensitive-property-names", caseInsensitive.ToString().ToLowerInvariant()));
            }

            parentElement.Add(contentElement);
        }
    }
}