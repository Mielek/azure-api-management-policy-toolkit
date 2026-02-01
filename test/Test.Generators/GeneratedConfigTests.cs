// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Test.Generators;

[TestClass]
public class GeneratedConfigTests
{
    [TestMethod]
    public void CompiledConfig_ShouldExist_ForManagedIdentityAuthenticationConfig()
    {
        // Arrange & Act - This will fail to compile if the generated class doesn't exist
        // Resource has [ExpressionAllowed], so it's ExpressionValue<string>
        // OutputTokenVariableName and IgnoreError don't have [ExpressionAllowed], so they're plain types
        var config = new CompiledConfigs.ManagedIdentityAuthenticationConfig
        {
            Resource = ExpressionValue<string>.FromConstant("https://management.azure.com/"),
            ClientId = ExpressionValue<string>.FromExpression("@(context.Request.Headers[\"X-Client-Id\"])"),
            OutputTokenVariableName = "token",
            IgnoreError = true
        };

        // Assert
        config.Resource.IsConstant.Should().BeTrue();
        config.Resource.ConstantValue.Should().Be("https://management.azure.com/");
        
        config.ClientId.Should().NotBeNull();
        config.ClientId!.Value.IsExpression.Should().BeTrue();
        
        config.OutputTokenVariableName.Should().Be("token");
        config.IgnoreError.Should().Be(true);
    }

    [TestMethod]
    public void CompiledConfig_ShouldSupportExpressions_ForExpressionAllowedProperties()
    {
        // Arrange & Act - Resource has [ExpressionAllowed] so it can hold expressions
        var config = new CompiledConfigs.ManagedIdentityAuthenticationConfig
        {
            Resource = ExpressionValue<string>.FromExpression("@(context.Request.Headers[\"X-Resource\"])"),
            OutputTokenVariableName = null,
            IgnoreError = null
        };

        // Assert
        config.Resource.IsExpression.Should().BeTrue();
        config.Resource.Expression.Should().Be("@(context.Request.Headers[\"X-Resource\"])");
    }

    [TestMethod]
    public void CompiledConfig_ExpressionAllowedCollectionProperty_ShouldExist()
    {
        // Arrange & Act - Values has [ExpressionAllowed] and is a string[]
        // So the compiled config should have IReadOnlyList<ExpressionValue<string>>
        var config = new CompiledConfigs.CheckHeaderConfig
        {
            Name = ExpressionValue<string>.FromConstant("X-Custom-Header"),
            FailCheckHttpCode = ExpressionValue<int>.FromConstant(400),
            FailCheckErrorMessage = ExpressionValue<string>.FromConstant("Missing header"),
            IgnoreCase = ExpressionValue<bool>.FromConstant(true),
            Values = new[] { ExpressionValue<string>.FromConstant("allowed-value") }
        };

        // Assert
        config.Name.ConstantValue.Should().Be("X-Custom-Header");
        config.Values.Should().HaveCount(1);
        config.Values[0].ConstantValue.Should().Be("allowed-value");
    }

    [TestMethod]
    public void ImplicitConversion_ShouldWorkWithExpressionAllowedProperties()
    {
        // Arrange & Act - implicit conversion from T to ExpressionValue<T>
        var config = new CompiledConfigs.ManagedIdentityAuthenticationConfig
        {
            Resource = "https://management.azure.com/", // implicit conversion to ExpressionValue<string>
            OutputTokenVariableName = null,
            IgnoreError = false
        };

        // Assert
        config.Resource.IsConstant.Should().BeTrue();
        config.Resource.ConstantValue.Should().Be("https://management.azure.com/");
    }
}
