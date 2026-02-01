// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;

namespace Test.Generators;

/// <summary>
/// Test config class to verify the source generator works correctly.
/// These are NOT in the Authoring namespace, so they won't get compiled configs generated.
/// The tests verify the generator works by checking configs from the Authoring assembly.
/// </summary>
public class TestSimpleConfig
{
    [ExpressionAllowed]
    public required string Name { get; init; }
    public string? OptionalValue { get; init; }
    public int Count { get; init; }
    public bool IsEnabled { get; init; }
}

/// <summary>
/// Test config with enum property.
/// </summary>
public class TestEnumConfig
{
    [ExpressionAllowed]
    public required TestActionType Action { get; init; }
    [ExpressionAllowed]
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
public class TestXmlNameConfig
{
    [XmlName("custom-name")]
    [ExpressionAllowed]
    public required string Value { get; init; }
}
