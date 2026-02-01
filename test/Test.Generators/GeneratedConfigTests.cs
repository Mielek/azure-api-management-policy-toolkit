// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Test.Generators;

[TestClass]
public class GeneratedConfigTests
{
    [TestMethod]
    public void TestSimpleConfig_ShouldExist()
    {
        // Arrange & Act - This will fail to compile if the generated class doesn't exist
        var config = new CompiledConfigs.TestSimpleConfig
        {
            Name = ExpressionValue<string>.FromConstant("test"),
            OptionalValue = ExpressionValue<string>.FromExpression("@(context.Request.Headers[\"X-Test\"])"),
            Count = ExpressionValue<int>.FromConstant(42),
            IsEnabled = ExpressionValue<bool>.FromConstant(true)
        };

        // Assert
        config.Name.IsConstant.Should().BeTrue();
        config.Name.ConstantValue.Should().Be("test");
        
        config.OptionalValue.Should().NotBeNull();
        config.OptionalValue!.Value.IsExpression.Should().BeTrue();
        
        config.Count.ConstantValue.Should().Be(42);
        config.IsEnabled.ConstantValue.Should().BeTrue();
    }

    [TestMethod]
    public void TestSimpleConfig_WithNullOptional_ShouldWork()
    {
        // Arrange & Act
        var config = new CompiledConfigs.TestSimpleConfig
        {
            Name = ExpressionValue<string>.FromConstant("test"),
            OptionalValue = null,
            Count = ExpressionValue<int>.FromConstant(0),
            IsEnabled = ExpressionValue<bool>.FromConstant(false)
        };

        // Assert
        config.OptionalValue.Should().BeNull();
    }

    [TestMethod]
    public void TestEnumConfig_ShouldExist()
    {
        // Arrange & Act
        var config = new CompiledConfigs.TestEnumConfig
        {
            Action = ExpressionValue<TestActionType>.FromConstant(TestActionType.Override),
            OptionalAction = ExpressionValue<TestActionType>.FromExpression("@(GetAction())")
        };

        // Assert
        config.Action.ConstantValue.Should().Be(TestActionType.Override);
        config.OptionalAction.Should().NotBeNull();
        config.OptionalAction!.Value.IsExpression.Should().BeTrue();
    }

    [TestMethod]
    public void TestEnumConfig_ToXmlValue_ShouldReturnKebabCase()
    {
        // Arrange
        var config = new CompiledConfigs.TestEnumConfig
        {
            Action = ExpressionValue<TestActionType>.FromConstant(TestActionType.Override)
        };

        // Act
        var xmlValue = config.Action.ToXmlValue();

        // Assert
        xmlValue.Should().Be("override");
    }

    [TestMethod]
    public void TestXmlNameConfig_ShouldExist()
    {
        // Arrange & Act
        var config = new CompiledConfigs.TestXmlNameConfig
        {
            Value = ExpressionValue<string>.FromConstant("test")
        };

        // Assert
        config.Value.ConstantValue.Should().Be("test");
    }

    [TestMethod]
    public void ImplicitConversion_ShouldWorkWithGeneratedConfig()
    {
        // Arrange & Act - implicit conversion from T to ExpressionValue<T>
        var config = new CompiledConfigs.TestSimpleConfig
        {
            Name = "test-name", // implicit conversion
            OptionalValue = null,
            Count = 100, // implicit conversion
            IsEnabled = true // implicit conversion
        };

        // Assert
        config.Name.IsConstant.Should().BeTrue();
        config.Name.ConstantValue.Should().Be("test-name");
        config.Count.ConstantValue.Should().Be(100);
        config.IsEnabled.ConstantValue.Should().BeTrue();
    }
}
