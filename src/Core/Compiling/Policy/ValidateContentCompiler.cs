// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public class ValidateContentCompiler : IMethodPolicyHandler
{
    public string MethodName => nameof(IInboundContext.ValidateContent);

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var configResult = CompiledConfigExtractor.Extract<LocalValidateContentCompiledConfig>(
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
            element.Add(new XAttribute("errors-variable-name", errorsVar.ToXmlValue()));
        }

        // Handle ContentTypeMap
        if (config.ContentTypeMap is { } contentTypeMapValue)
        {
            HandleContentTypeMap(context, contentTypeMapValue, element);
        }

        // Handle ContentTypes
        if (config.Contents is { } contentTypesValue)
        {
            HandleContents(context, contentTypesValue, element);
        }

        context.AddPolicy(element);
    }

    private static void HandleContentTypeMap(IDocumentCompilationContext context, InitializerValue contentTypeMapValue,
        XElement parentElement)
    {
        if (!contentTypeMapValue.TryGetValues<ContentTypeMapConfig>(out var mapConfigValues))
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.PolicyArgumentIsNotOfRequiredType,
                contentTypeMapValue.Node.GetLocation(),
                "validate-content.content-type-map",
                nameof(ContentTypeMapConfig)
            ));
            return;
        }

        XElement mapElement = new("content-type-map");
        mapElement.AddAttribute(mapConfigValues, nameof(ContentTypeMapConfig.AnyContentTypeValue),
            "any-content-type-value");
        mapElement.AddAttribute(mapConfigValues, nameof(ContentTypeMapConfig.MissingContentTypeValue),
            "missing-content-type-value");

        // Handle content type mappings
        if (mapConfigValues.TryGetValue(nameof(ContentTypeMapConfig.Types), out var typesValue))
        {
            HandleTypeMap(context, typesValue, mapElement);
        }

        parentElement.Add(mapElement);
    }

    private static void HandleTypeMap(IDocumentCompilationContext context, InitializerValue typesValue,
        XElement mapElement)
    {
        foreach (var typeValue in typesValue.UnnamedValues ?? [])
        {
            if (!typeValue.TryGetValues<ContentTypeMap>(out var typeMapValues))
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.PolicyArgumentIsNotOfRequiredType,
                    typeValue.Node.GetLocation(),
                    "validate-content.content-type-map.type",
                    nameof(ContentTypeMap)
                ));
                continue;
            }

            XElement typeElement = new("type");
            if (!typeElement.AddAttribute(typeMapValues, nameof(ContentTypeMap.To), "to"))
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.RequiredParameterNotDefined,
                    typeValue.Node.GetLocation(),
                    "validate-content.content-type-map.type",
                    nameof(ContentTypeMap.To)
                ));
                continue;
            }

            typeElement.AddAttribute(typeMapValues, nameof(ContentTypeMap.From), "from");
            typeElement.AddAttribute(typeMapValues, nameof(ContentTypeMap.When), "when");

            mapElement.Add(typeElement);
        }
    }

    private static void HandleContents(IDocumentCompilationContext context, InitializerValue contentTypesValue,
        XElement parentElement)
    {
        foreach (var contentTypeValue in contentTypesValue.UnnamedValues ?? [])
        {
            if (!contentTypeValue.TryGetValues<ValidateContent>(out var validateContentTypeValues))
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.PolicyArgumentIsNotOfRequiredType,
                    contentTypeValue.Node.GetLocation(),
                    "validate-content.content",
                    nameof(ValidateContent)
                ));
                continue;
            }

            XElement contentTypeElement = new("content");
            if (!contentTypeElement.AddAttribute(validateContentTypeValues, nameof(ValidateContent.ValidateAs),
                    "validate-as"))
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.RequiredParameterNotDefined,
                    contentTypeValue.Node.GetLocation(),
                    "validate-content.content",
                    nameof(ValidateContent.ValidateAs)
                ));
                continue;
            }

            if (!contentTypeElement.AddAttribute(validateContentTypeValues, nameof(ValidateContent.Action),
                    "action"))
            {
                context.Report(Diagnostic.Create(
                    CompilationErrors.RequiredParameterNotDefined,
                    contentTypeValue.Node.GetLocation(),
                    "validate-content.content",
                    nameof(ValidateContent.Action)
                ));
                continue;
            }

            contentTypeElement.AddAttribute(validateContentTypeValues, nameof(ValidateContent.Type), "type");
            contentTypeElement.AddAttribute(validateContentTypeValues, nameof(ValidateContent.SchemaId),
                "schema-id");
            contentTypeElement.AddAttribute(validateContentTypeValues, nameof(ValidateContent.SchemaRef),
                "schema-ref");
            contentTypeElement.AddAttribute(validateContentTypeValues,
                nameof(ValidateContent.AllowAdditionalProperties), "allow-additional-properties");
            contentTypeElement.AddAttribute(validateContentTypeValues,
                nameof(ValidateContent.CaseInsensitivePropertyNames), "case-insensitive-property-names");

            parentElement.Add(contentTypeElement);
        }
    }

    private sealed class LocalValidateContentCompiledConfig
    {
        public required ExpressionValue<string> UnspecifiedContentTypeAction { get; init; }
        public required ExpressionValue<int> MaxSize { get; init; }
        public required ExpressionValue<string> SizeExceededAction { get; init; }
        public ExpressionValue<string>? ErrorsVariableName { get; init; }
        public InitializerValue? ContentTypeMap { get; init; }
        public InitializerValue? Contents { get; init; }
    }
}