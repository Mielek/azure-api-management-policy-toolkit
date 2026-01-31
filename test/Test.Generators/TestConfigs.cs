// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;

namespace Test.Generators;

/// <summary>
/// Test config class to verify the source generator works correctly.
/// </summary>
[GenerateCompiledConfig]
public class TestSimpleConfig
{
    public required string Name { get; init; }
    public string? OptionalValue { get; init; }
    public int Count { get; init; }
    public bool IsEnabled { get; init; }
}

/// <summary>
/// Test config with enum property.
/// </summary>
[GenerateCompiledConfig]
public class TestEnumConfig
{
    public required TestActionType Action { get; init; }
    public TestActionType? OptionalAction { get; init; }
}

public enum TestActionType
{
    Skip,
    Override,
    Append
}

/// <summary>
/// Test config with XmlName attribute.
/// </summary>
[GenerateCompiledConfig]
public class TestXmlNameConfig
{
    [XmlName("custom-name")]
    public required string Value { get; init; }
}
