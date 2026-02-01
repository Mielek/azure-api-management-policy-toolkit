// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public static class GenericCompiler
{
    public static void HandleList(
        XElement element,
        IReadOnlyList<string> values,
        string listName,
        string elementName)
    {
        XElement listElement = new(listName);
        foreach (var value in values)
        {
            listElement.AddElement(elementName, value);
        }

        element.Add(listElement);
    }

    public static void HandleList(
        XElement element,
        IReadOnlyList<ExpressionValue<string>> values,
        string listName,
        string elementName)
    {
        XElement listElement = new(listName);
        foreach (var value in values)
        {
            listElement.AddElement(elementName, value);
        }

        element.Add(listElement);
    }
}