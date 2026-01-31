// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;

/// <summary>
/// Marks a config class for source generation of a compiled config class.
/// The generator will create a {ClassName}CompiledConfig class with all properties
/// wrapped in ExpressionValue&lt;T&gt;.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GenerateCompiledConfigAttribute : Attribute
{
}
