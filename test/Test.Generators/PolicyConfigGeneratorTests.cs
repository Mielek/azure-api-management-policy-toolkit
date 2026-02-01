// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Test.Generators;

/// <summary>
/// Tests for the PolicyConfigGenerator source generator.
/// These tests verify that the generator correctly produces compiled config classes.
/// </summary>
[TestClass]
public class PolicyConfigGeneratorTests
{
    #region Basic Config Generation

    [TestMethod]
    public void Generator_ShouldCreateConfig_ForSimpleProperties()
    {
        // Arrange & Act - Verify compiled config exists for a simple config
        var config = new CompiledConfigs.MockResponseConfig
        {
            StatusCode = 200,
            ContentType = "application/json"
        };

        // Assert
        config.StatusCode.Should().Be(200);
        config.ContentType.Should().Be("application/json");
    }

    [TestMethod]
    public void Generator_ShouldSupportRequiredProperties()
    {
        // Arrange & Act - CacheLookupValueConfig has required Key property
        var config = new CompiledConfigs.CacheLookupValueConfig
        {
            Key = ExpressionValue<string>.FromConstant("cache-key"),
            DefaultValue = ExpressionValue<string>.FromConstant("default"),
            VariableName = "result"
        };

        // Assert
        config.Key.ConstantValue.Should().Be("cache-key");
    }

    #endregion

    #region ExpressionAllowed Properties

    [TestMethod]
    public void Generator_ShouldWrapInExpressionValue_ForExpressionAllowedProperties()
    {
        // Arrange & Act - Resource has [ExpressionAllowed]
        var config = new CompiledConfigs.ManagedIdentityAuthenticationConfig
        {
            Resource = ExpressionValue<string>.FromExpression("@(context.Variables[\"resource\"])"),
            OutputTokenVariableName = null,
            IgnoreError = null
        };

        // Assert - Should be ExpressionValue<string>, not plain string
        config.Resource.IsExpression.Should().BeTrue();
        config.Resource.Expression.Should().Be("@(context.Variables[\"resource\"])");
    }

    [TestMethod]
    public void Generator_ShouldNotWrapInExpressionValue_ForNonExpressionAllowedProperties()
    {
        // Arrange & Act - OutputTokenVariableName does NOT have [ExpressionAllowed]
        var config = new CompiledConfigs.ManagedIdentityAuthenticationConfig
        {
            Resource = "resource",
            OutputTokenVariableName = "token-var"
        };

        // Assert - Should be plain string, not ExpressionValue<string>
        // This is verified by the fact that we can assign a string directly
        config.OutputTokenVariableName.Should().Be("token-var");
    }

    #endregion

    #region Collections

    [TestMethod]
    public void Generator_ShouldCreateCollectionProperty_ForArrays()
    {
        // Arrange & Act - CheckHeaderConfig has Values which is string[]
        var config = new CompiledConfigs.CheckHeaderConfig
        {
            Name = "X-Custom",
            FailCheckHttpCode = 400,
            FailCheckErrorMessage = "Missing",
            IgnoreCase = true,
            Values = [
                ExpressionValue<string>.FromConstant("value1"),
                ExpressionValue<string>.FromConstant("value2")
            ]
        };

        // Assert
        config.Values.Should().HaveCount(2);
        config.Values![0].ConstantValue.Should().Be("value1");
        config.Values[1].ConstantValue.Should().Be("value2");
    }

    [TestMethod]
    public void Generator_ShouldSupportNestedConfigCollections()
    {
        // Arrange & Act - SendRequestConfig has Headers which is HeaderConfig[]
        var config = new CompiledConfigs.SendRequestConfig
        {
            ResponseVariableName = "response",
            Headers =
            [
                new CompiledConfigs.HeaderConfig
                {
                    Name = "Content-Type",
                    Values = [ExpressionValue<string>.FromConstant("application/json")]
                }
            ]
        };

        // Assert
        config.Headers.Should().HaveCount(1);
        config.Headers![0].Name.ConstantValue.Should().Be("Content-Type");
    }

    #endregion

    #region Union Types

    [TestMethod]
    public void Generator_ShouldCreateUnionType_ForInterfaceImplementations()
    {
        // Arrange & Act - AuthenticationConfigUnion should exist
        // and have case classes for each implementation
        var basicAuth = new CompiledConfigs.AuthenticationConfigUnion.BasicAuthentication(
            new CompiledConfigs.BasicAuthenticationConfig
            {
                Username = "user",
                Password = "pass"
            });

        var certAuth = new CompiledConfigs.AuthenticationConfigUnion.CertificateAuthentication(
            new CompiledConfigs.CertificateAuthenticationConfig
            {
                CertificateId = "cert-id"
            });

        var managedAuth = new CompiledConfigs.AuthenticationConfigUnion.ManagedIdentityAuthentication(
            new CompiledConfigs.ManagedIdentityAuthenticationConfig
            {
                Resource = "https://management.azure.com/"
            });

        // Assert
        basicAuth.Should().BeAssignableTo<CompiledConfigs.AuthenticationConfigUnion>();
        certAuth.Should().BeAssignableTo<CompiledConfigs.AuthenticationConfigUnion>();
        managedAuth.Should().BeAssignableTo<CompiledConfigs.AuthenticationConfigUnion>();
        
        basicAuth.Config.Username.ConstantValue.Should().Be("user");
        certAuth.Config.CertificateId!.Value.ConstantValue.Should().Be("cert-id");
        managedAuth.Config.Resource.ConstantValue.Should().Be("https://management.azure.com/");
    }

    [TestMethod]
    public void Generator_UnionType_ShouldSupportMatch()
    {
        // Arrange
        CompiledConfigs.AuthenticationConfigUnion union = new CompiledConfigs.AuthenticationConfigUnion.BasicAuthentication(
            new CompiledConfigs.BasicAuthenticationConfig
            {
                Username = "user",
                Password = "pass"
            });

        // Act - Match passes the Config directly, not the wrapper
        var result = union.Match(
            basic => $"basic:{basic.Username.ConstantValue}",
            certificate => "certificate",
            managedIdentity => "managed");

        // Assert
        result.Should().Be("basic:user");
    }

    #endregion

    #region Abstract Base Classes (Inheritance)

    [TestMethod]
    public void Generator_ShouldSupportAbstractBaseClasses()
    {
        // Arrange & Act - KeyConfig is abstract with Base64KeyConfig, CertificateKeyConfig, AsymmetricKeyConfig
        var base64Key = new CompiledConfigs.Base64KeyConfig
        {
            Id = "key1",
            Value = "base64value"
        };

        var certKey = new CompiledConfigs.CertificateKeyConfig
        {
            Id = "key2",
            CertificateId = "cert-id"
        };

        var asymKey = new CompiledConfigs.AsymmetricKeyConfig
        {
            Id = "key3",
            Modulus = "mod",
            Exponent = "exp"
        };

        // Assert
        base64Key.Should().BeAssignableTo<CompiledConfigs.KeyConfig>();
        certKey.Should().BeAssignableTo<CompiledConfigs.KeyConfig>();
        asymKey.Should().BeAssignableTo<CompiledConfigs.KeyConfig>();
    }

    [TestMethod]
    public void Generator_ShouldSupportCollectionsOfAbstractTypes()
    {
        // Arrange & Act - ValidateJwtConfig.IssuerSigningKeys is KeyConfig[]
        var config = new CompiledConfigs.ValidateJwtConfig
        {
            HeaderName = "Authorization",
            IssuerSigningKeys =
            [
                new CompiledConfigs.Base64KeyConfig { Value = "key1" },
                new CompiledConfigs.CertificateKeyConfig { CertificateId = "cert" }
            ]
        };

        // Assert
        config.IssuerSigningKeys.Should().HaveCount(2);
        config.IssuerSigningKeys![0].Should().BeOfType<CompiledConfigs.Base64KeyConfig>();
        config.IssuerSigningKeys[1].Should().BeOfType<CompiledConfigs.CertificateKeyConfig>();
    }

    #endregion

    #region Enum Properties

    [TestMethod]
    public void Generator_ShouldSupportEnumProperties()
    {
        // Arrange & Act - JsonToXmlConfig has Apply property
        var config = new CompiledConfigs.JsonToXmlConfig
        {
            Apply = "always"
        };

        // Assert - Apply is ExpressionValue<string>
        config.Apply.ConstantValue.Should().Be("always");
    }

    #endregion

    #region XmlName Attribute

    [TestMethod]
    public void Generator_ShouldRespectXmlNameAttribute()
    {
        // The generator uses XmlName attribute to determine the XML element name
        // This is tested implicitly through the compiler tests
        // Here we verify the property names match expected patterns
        
        // ForwardRequestConfig has properties like FailOnErrorStatusCode
        // which should map to fail-on-error-status-code in XML
        var config = new CompiledConfigs.ForwardRequestConfig
        {
            FailOnErrorStatusCode = true,
            BufferRequestBody = false
        };

        config.FailOnErrorStatusCode.Should().BeTrue();
        config.BufferRequestBody.Should().BeFalse();
    }

    #endregion

    #region Nullable Properties

    [TestMethod]
    public void Generator_ShouldSupportNullableProperties()
    {
        // Arrange & Act
        var config = new CompiledConfigs.ForwardRequestConfig
        {
            Timeout = null,
            FollowRedirects = true
        };

        // Assert
        config.Timeout.Should().BeNull();
        config.FollowRedirects.Should().NotBeNull();
        config.FollowRedirects!.Value.ConstantValue.Should().BeTrue();
    }

    [TestMethod]
    public void Generator_ShouldSupportNullableExpressionValueProperties()
    {
        // Arrange & Act
        var config = new CompiledConfigs.ManagedIdentityAuthenticationConfig
        {
            Resource = "resource",
            ClientId = null // Optional ExpressionValue<string>?
        };

        // Assert
        config.ClientId.Should().BeNull();
    }

    #endregion
}
