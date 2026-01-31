// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;

namespace Test.Generators;

[TestClass]
public class ExpressionValueTests
{
    [TestMethod]
    public void FromConstant_ShouldCreateConstantValue()
    {
        // Arrange & Act
        var value = ExpressionValue<string>.FromConstant("test");

        // Assert
        value.IsConstant.Should().BeTrue();
        value.IsExpression.Should().BeFalse();
        value.ConstantValue.Should().Be("test");
    }

    [TestMethod]
    public void FromExpression_ShouldCreateExpressionValue()
    {
        // Arrange & Act
        var value = ExpressionValue<string>.FromExpression("@(context.Request.Headers[\"X-Test\"])");

        // Assert
        value.IsExpression.Should().BeTrue();
        value.IsConstant.Should().BeFalse();
        value.Expression.Should().Be("@(context.Request.Headers[\"X-Test\"])");
    }

    [TestMethod]
    public void ToXmlValue_WithConstantString_ShouldReturnString()
    {
        // Arrange
        var value = ExpressionValue<string>.FromConstant("test-value");

        // Act
        var result = value.ToXmlValue();

        // Assert
        result.Should().Be("test-value");
    }

    [TestMethod]
    public void ToXmlValue_WithConstantBool_ShouldReturnLowercaseString()
    {
        // Arrange
        var trueValue = ExpressionValue<bool>.FromConstant(true);
        var falseValue = ExpressionValue<bool>.FromConstant(false);

        // Act & Assert
        trueValue.ToXmlValue().Should().Be("true");
        falseValue.ToXmlValue().Should().Be("false");
    }

    [TestMethod]
    public void ToXmlValue_WithExpression_ShouldReturnExpression()
    {
        // Arrange
        var value = ExpressionValue<string>.FromExpression("@(context.Request.Url)");

        // Act
        var result = value.ToXmlValue();

        // Assert
        result.Should().Be("@(context.Request.Url)");
    }

    [TestMethod]
    public void ToXmlValue_WithEnum_ShouldReturnKebabCase()
    {
        // Arrange
        var value = ExpressionValue<TestEnum>.FromConstant(TestEnum.SomeValue);

        // Act
        var result = value.ToXmlValue();

        // Assert
        result.Should().Be("some-value");
    }

    [TestMethod]
    public void TryGetConstant_WithConstant_ShouldReturnTrue()
    {
        // Arrange
        var value = ExpressionValue<int>.FromConstant(42);

        // Act
        var result = value.TryGetConstant(out var constant);

        // Assert
        result.Should().BeTrue();
        constant.Should().Be(42);
    }

    [TestMethod]
    public void TryGetConstant_WithExpression_ShouldReturnFalse()
    {
        // Arrange
        var value = ExpressionValue<int>.FromExpression("@(1 + 1)");

        // Act
        var result = value.TryGetConstant(out var constant);

        // Assert
        result.Should().BeFalse();
        constant.Should().Be(default);
    }

    [TestMethod]
    public void TryGetExpression_WithExpression_ShouldReturnTrue()
    {
        // Arrange
        var value = ExpressionValue<string>.FromExpression("@(context.Request.Url)");

        // Act
        var result = value.TryGetExpression(out var expression);

        // Assert
        result.Should().BeTrue();
        expression.Should().Be("@(context.Request.Url)");
    }

    [TestMethod]
    public void TryGetExpression_WithConstant_ShouldReturnFalse()
    {
        // Arrange
        var value = ExpressionValue<string>.FromConstant("test");

        // Act
        var result = value.TryGetExpression(out var expression);

        // Assert
        result.Should().BeFalse();
        expression.Should().BeNull();
    }

    [TestMethod]
    public void Match_WithConstant_ShouldCallConstantFunc()
    {
        // Arrange
        var value = ExpressionValue<int>.FromConstant(42);

        // Act
        var result = value.Match(
            constant => $"Constant: {constant}",
            expression => $"Expression: {expression}"
        );

        // Assert
        result.Should().Be("Constant: 42");
    }

    [TestMethod]
    public void Match_WithExpression_ShouldCallExpressionFunc()
    {
        // Arrange
        var value = ExpressionValue<int>.FromExpression("@(1 + 1)");

        // Act
        var result = value.Match(
            constant => $"Constant: {constant}",
            expression => $"Expression: {expression}"
        );

        // Assert
        result.Should().Be("Expression: @(1 + 1)");
    }

    [TestMethod]
    public void Map_WithConstant_ShouldTransformValue()
    {
        // Arrange
        var value = ExpressionValue<int>.FromConstant(42);

        // Act
        var result = value.Map(x => x.ToString());

        // Assert
        result.IsConstant.Should().BeTrue();
        result.ConstantValue.Should().Be("42");
    }

    [TestMethod]
    public void Map_WithExpression_ShouldPreserveExpression()
    {
        // Arrange
        var value = ExpressionValue<int>.FromExpression("@(1 + 1)");

        // Act
        var result = value.Map(x => x.ToString());

        // Assert
        result.IsExpression.Should().BeTrue();
        result.Expression.Should().Be("@(1 + 1)");
    }

    [TestMethod]
    public void ImplicitConversion_ShouldCreateConstant()
    {
        // Arrange & Act
        ExpressionValue<string> value = "test";

        // Assert
        value.IsConstant.Should().BeTrue();
        value.ConstantValue.Should().Be("test");
    }

    [TestMethod]
    public void Equality_SameConstant_ShouldBeEqual()
    {
        // Arrange
        var value1 = ExpressionValue<string>.FromConstant("test");
        var value2 = ExpressionValue<string>.FromConstant("test");

        // Assert
        value1.Should().Be(value2);
        (value1 == value2).Should().BeTrue();
    }

    [TestMethod]
    public void Equality_DifferentConstant_ShouldNotBeEqual()
    {
        // Arrange
        var value1 = ExpressionValue<string>.FromConstant("test1");
        var value2 = ExpressionValue<string>.FromConstant("test2");

        // Assert
        value1.Should().NotBe(value2);
        (value1 != value2).Should().BeTrue();
    }

    [TestMethod]
    public void Equality_SameExpression_ShouldBeEqual()
    {
        // Arrange
        var value1 = ExpressionValue<string>.FromExpression("@(x)");
        var value2 = ExpressionValue<string>.FromExpression("@(x)");

        // Assert
        value1.Should().Be(value2);
    }

    [TestMethod]
    public void Equality_ConstantVsExpression_ShouldNotBeEqual()
    {
        // Arrange
        var value1 = ExpressionValue<string>.FromConstant("@(x)");
        var value2 = ExpressionValue<string>.FromExpression("@(x)");

        // Assert
        value1.Should().NotBe(value2);
    }

    [TestMethod]
    public void ToString_WithConstant_ShouldShowConstant()
    {
        // Arrange
        var value = ExpressionValue<int>.FromConstant(42);

        // Act
        var result = value.ToString();

        // Assert
        result.Should().Be("Constant(42)");
    }

    [TestMethod]
    public void ToString_WithExpression_ShouldShowExpression()
    {
        // Arrange
        var value = ExpressionValue<int>.FromExpression("@(1 + 1)");

        // Act
        var result = value.ToString();

        // Assert
        result.Should().Be("Expression(@(1 + 1))");
    }

    [TestMethod]
    public void AccessingConstant_WhenExpression_ShouldThrow()
    {
        // Arrange
        var value = ExpressionValue<string>.FromExpression("@(x)");

        // Act & Assert
        var act = () => value.ConstantValue;
        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void AccessingExpression_WhenConstant_ShouldThrow()
    {
        // Arrange
        var value = ExpressionValue<string>.FromConstant("test");

        // Act & Assert
        var act = () => value.Expression;
        act.Should().Throw<InvalidOperationException>();
    }

    private enum TestEnum
    {
        SomeValue,
        AnotherValue
    }
}
