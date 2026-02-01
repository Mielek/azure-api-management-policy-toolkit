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

public class AppendHeaderCompiler() : BaseSetHeaderCompiler(nameof(IInboundContext.AppendHeader), "append");

public class SetHeaderCompiler() : BaseSetHeaderCompiler(nameof(IInboundContext.SetHeader), "override");

public class SetHeaderIfNotExistCompiler() : BaseSetHeaderCompiler(nameof(IInboundContext.SetHeaderIfNotExist), "skip");

public class RemoveHeaderCompiler() : BaseSetHeaderCompiler(nameof(IInboundContext.RemoveHeader), "delete");

public abstract class BaseSetHeaderCompiler : IMethodPolicyHandler
{
    private readonly string _type;

    protected BaseSetHeaderCompiler(string methodName, string type)
    {
        MethodName = methodName;
        _type = type;
    }

    public string MethodName { get; }

    public void Handle(IDocumentCompilationContext context, InvocationExpressionSyntax node)
    {
        var arguments = node.ArgumentList.Arguments;
        if (_type != "delete" && arguments.Count < 2 ||
            _type == "delete" && arguments.Count != 1)
        {
            context.Report(Diagnostic.Create(
                CompilationErrors.ArgumentCountMissMatchForPolicy,
                node.ArgumentList.GetLocation(),
                "set-header"));
            return;
        }

        var element = new XElement("set-header");

        var nameResult = ExpressionProcessor.Process(node.ArgumentList.Arguments[0].Expression, context);
        if (!nameResult.IsSuccess)
        {
            nameResult.ReportAll(context);
            return;
        }
        element.Add(new XAttribute("name", nameResult.Value));
        element.Add(new XAttribute("exists-action", _type));

        for (int i = 1; i < arguments.Count; i++)
        {
            var valueResult = ExpressionProcessor.Process(arguments[i].Expression, context);
            if (!valueResult.IsSuccess)
            {
                valueResult.ReportAll(context);
                return;
            }
            element.Add(new XElement("value", valueResult.Value));
        }

        context.AddPolicy(element);
    }

    public static void HandleHeaders(XElement root, IReadOnlyList<CompiledConfigs.HeaderConfig> headers)
    {
        foreach (var config in headers)
        {
            var headerElement = new XElement("set-header");
            
            headerElement.Add(new XAttribute("name", config.Name.ToXmlValue()));
            
            if (config.ExistsAction is { } existsAction)
            {
                headerElement.Add(new XAttribute("exists-action", existsAction.ToXmlValue()));
            }

            if (config.Values is not null)
            {
                foreach (var value in config.Values)
                {
                    headerElement.Add(new XElement("value", value.ToXmlValue()));
                }
            }

            root.Add(headerElement);
        }
    }
}