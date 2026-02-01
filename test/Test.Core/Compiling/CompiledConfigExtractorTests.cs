// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Tests.Compiling;

/// <summary>
/// Unit tests for <see cref="CompiledConfigExtractor"/>.
/// These tests verify the extractor correctly parses syntax trees into compiled configs.
/// </summary>
[TestClass]
public class CompiledConfigExtractorTests
{
    private static readonly IEnumerable<MetadataReference> References =
    [
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(XElement).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(IDocument).Assembly.Location)
    ];

    #region Basic Extraction

    [TestMethod]
    public void Extract_ShouldExtractSimpleConfig()
    {
        // Arrange
        var (context, invocation) = CreateInvocation("""
            context.MockResponse(new MockResponseConfig 
            { 
                StatusCode = 200,
                ContentType = "application/json"
            });
            """);

        // Act
        var result = CompiledConfigExtractor.Extract<CompiledConfigs.MockResponseConfig>(
            invocation, context, "mock-response");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StatusCode.Should().Be(200);
        result.Value.ContentType.Should().Be("application/json");
    }

    [TestMethod]
    public void Extract_ShouldExtractRequiredProperty()
    {
        // Arrange
        var (context, invocation) = CreateInvocation("""
            context.CacheLookupValue(new CacheLookupValueConfig 
            { 
                Key = "my-cache-key",
                VariableName = "result"
            });
            """);

        // Act
        var result = CompiledConfigExtractor.Extract<CompiledConfigs.CacheLookupValueConfig>(
            invocation, context, "cache-lookup-value");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Key.ConstantValue.Should().Be("my-cache-key");
        result.Value.VariableName.Should().Be("result");
    }

    #endregion

    #region ExpressionValue Extraction

    [TestMethod]
    public void Extract_ShouldExtractConstantExpressionValue()
    {
        // Arrange
        var (context, invocation) = CreateInvocation("""
            context.AuthenticationManagedIdentity(new ManagedIdentityAuthenticationConfig 
            { 
                Resource = "https://management.azure.com/"
            });
            """);

        // Act
        var result = CompiledConfigExtractor.Extract<CompiledConfigs.ManagedIdentityAuthenticationConfig>(
            invocation, context, "authentication-managed-identity");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Resource.IsConstant.Should().BeTrue();
        result.Value.Resource.ConstantValue.Should().Be("https://management.azure.com/");
    }

    [TestMethod]
    public void Extract_ShouldExtractExpressionExpressionValue()
    {
        // Arrange
        var (context, invocation) = CreateInvocation("""
            context.AuthenticationManagedIdentity(new ManagedIdentityAuthenticationConfig 
            { 
                Resource = GetResource(context.ExpressionContext)
            });
            """,
            helperMethods: """
            string GetResource(IExpressionContext ctx) => ctx.Variables["resource"].ToString();
            """);

        // Act
        var result = CompiledConfigExtractor.Extract<CompiledConfigs.ManagedIdentityAuthenticationConfig>(
            invocation, context, "authentication-managed-identity");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Resource.IsExpression.Should().BeTrue();
        // The expression uses 'ctx' as parameter name in the helper method
        result.Value.Resource.Expression.Should().Contain("ctx.Variables");
    }

    #endregion

    #region Collection Extraction

    [TestMethod]
    public void Extract_ShouldExtractStringCollection()
    {
        // Arrange
        var (context, invocation) = CreateInvocation("""
            context.ValidateJwt(new ValidateJwtConfig 
            { 
                HeaderName = "Authorization",
                Issuers = ["issuer1", "issuer2", "issuer3"]
            });
            """);

        // Act
        var result = CompiledConfigExtractor.Extract<CompiledConfigs.ValidateJwtConfig>(
            invocation, context, "validate-jwt");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Issuers.Should().HaveCount(3);
        result.Value.Issuers![0].ConstantValue.Should().Be("issuer1");
        result.Value.Issuers[1].ConstantValue.Should().Be("issuer2");
        result.Value.Issuers[2].ConstantValue.Should().Be("issuer3");
    }

    [TestMethod]
    public void Extract_ShouldExtractExpressionValueCollection()
    {
        // Arrange
        var (context, invocation) = CreateInvocation("""
            context.CheckHeader(new CheckHeaderConfig 
            { 
                Name = "X-Custom",
                FailCheckHttpCode = 400,
                FailCheckErrorMessage = "Missing header",
                IgnoreCase = true,
                Values = ["value1", GetValue(context.ExpressionContext), "value3"]
            });
            """,
            helperMethods: """
            string GetValue(IExpressionContext ctx) => ctx.Request.Headers["X-Dynamic"];
            """);

        // Act
        var result = CompiledConfigExtractor.Extract<CompiledConfigs.CheckHeaderConfig>(
            invocation, context, "check-header");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Values.Should().HaveCount(3);
        result.Value.Values![0].IsConstant.Should().BeTrue();
        result.Value.Values[0].ConstantValue.Should().Be("value1");
        result.Value.Values[1].IsExpression.Should().BeTrue();
        result.Value.Values[2].IsConstant.Should().BeTrue();
    }

    [TestMethod]
    public void Extract_ShouldExtractNestedConfigCollection()
    {
        // Arrange
        var (context, invocation) = CreateInvocation("""
            context.SendRequest(new SendRequestConfig 
            { 
                ResponseVariableName = "response",
                Headers = [
                    new HeaderConfig { Name = "Content-Type", Values = ["application/json"] },
                    new HeaderConfig { Name = "Accept", ExistsAction = "override", Values = ["text/plain"] }
                ]
            });
            """);

        // Act
        var result = CompiledConfigExtractor.Extract<CompiledConfigs.SendRequestConfig>(
            invocation, context, "send-request");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Headers.Should().HaveCount(2);
        result.Value.Headers![0].Name.ConstantValue.Should().Be("Content-Type");
        result.Value.Headers[1].Name.ConstantValue.Should().Be("Accept");
        result.Value.Headers[1].ExistsAction!.Value.ConstantValue.Should().Be("override");
    }

    #endregion

    #region Union Type Extraction

    [TestMethod]
    public void Extract_ShouldExtractUnionType_BasicAuthentication()
    {
        // Arrange
        var (context, invocation) = CreateInvocation("""
            context.SendRequest(new SendRequestConfig 
            { 
                ResponseVariableName = "response",
                Authentication = new BasicAuthenticationConfig 
                { 
                    Username = "user",
                    Password = "pass"
                }
            });
            """);

        // Act
        var result = CompiledConfigExtractor.Extract<CompiledConfigs.SendRequestConfig>(
            invocation, context, "send-request");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Authentication.Should().NotBeNull();
        result.Value.Authentication.Should().BeOfType<CompiledConfigs.AuthenticationConfigUnion.BasicAuthentication>();
        var basic = (CompiledConfigs.AuthenticationConfigUnion.BasicAuthentication)result.Value.Authentication!;
        basic.Config.Username.ConstantValue.Should().Be("user");
        basic.Config.Password.ConstantValue.Should().Be("pass");
    }

    [TestMethod]
    public void Extract_ShouldExtractUnionType_CertificateAuthentication()
    {
        // Arrange
        var (context, invocation) = CreateInvocation("""
            context.SendRequest(new SendRequestConfig 
            { 
                ResponseVariableName = "response",
                Authentication = new CertificateAuthenticationConfig 
                { 
                    CertificateId = "my-cert-id"
                }
            });
            """);

        // Act
        var result = CompiledConfigExtractor.Extract<CompiledConfigs.SendRequestConfig>(
            invocation, context, "send-request");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Authentication.Should().BeOfType<CompiledConfigs.AuthenticationConfigUnion.CertificateAuthentication>();
        var cert = (CompiledConfigs.AuthenticationConfigUnion.CertificateAuthentication)result.Value.Authentication!;
        cert.Config.CertificateId!.Value.ConstantValue.Should().Be("my-cert-id");
    }

    [TestMethod]
    public void Extract_ShouldExtractUnionType_ManagedIdentityAuthentication()
    {
        // Arrange
        var (context, invocation) = CreateInvocation("""
            context.SendRequest(new SendRequestConfig 
            { 
                ResponseVariableName = "response",
                Authentication = new ManagedIdentityAuthenticationConfig 
                { 
                    Resource = "https://api.example.com/",
                    ClientId = "client-id"
                }
            });
            """);

        // Act
        var result = CompiledConfigExtractor.Extract<CompiledConfigs.SendRequestConfig>(
            invocation, context, "send-request");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Authentication.Should().BeOfType<CompiledConfigs.AuthenticationConfigUnion.ManagedIdentityAuthentication>();
        var managed = (CompiledConfigs.AuthenticationConfigUnion.ManagedIdentityAuthentication)result.Value.Authentication!;
        managed.Config.Resource.ConstantValue.Should().Be("https://api.example.com/");
        managed.Config.ClientId!.Value.ConstantValue.Should().Be("client-id");
    }

    #endregion

    #region Abstract Base Class Extraction

    [TestMethod]
    public void Extract_ShouldExtractAbstractBaseClass_Base64Key()
    {
        // Arrange
        var (context, invocation) = CreateInvocation("""
            context.ValidateJwt(new ValidateJwtConfig 
            { 
                HeaderName = "Authorization",
                IssuerSigningKeys = [
                    new Base64KeyConfig { Id = "key1", Value = "base64value" }
                ]
            });
            """);

        // Act
        var result = CompiledConfigExtractor.Extract<CompiledConfigs.ValidateJwtConfig>(
            invocation, context, "validate-jwt");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IssuerSigningKeys.Should().HaveCount(1);
        result.Value.IssuerSigningKeys![0].Should().BeOfType<CompiledConfigs.Base64KeyConfig>();
        var key = (CompiledConfigs.Base64KeyConfig)result.Value.IssuerSigningKeys[0];
        key.Id.Should().Be("key1");
        key.Value.Should().Be("base64value");
    }

    [TestMethod]
    public void Extract_ShouldExtractAbstractBaseClass_MixedKeyTypes()
    {
        // Arrange
        var (context, invocation) = CreateInvocation("""
            context.ValidateJwt(new ValidateJwtConfig 
            { 
                HeaderName = "Authorization",
                IssuerSigningKeys = [
                    new Base64KeyConfig { Id = "key1", Value = "base64value" },
                    new CertificateKeyConfig { Id = "key2", CertificateId = "cert-id" },
                    new AsymmetricKeyConfig { Id = "key3", Modulus = "mod", Exponent = "exp" }
                ]
            });
            """);

        // Act
        var result = CompiledConfigExtractor.Extract<CompiledConfigs.ValidateJwtConfig>(
            invocation, context, "validate-jwt");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IssuerSigningKeys.Should().HaveCount(3);
        result.Value.IssuerSigningKeys![0].Should().BeOfType<CompiledConfigs.Base64KeyConfig>();
        result.Value.IssuerSigningKeys[1].Should().BeOfType<CompiledConfigs.CertificateKeyConfig>();
        result.Value.IssuerSigningKeys[2].Should().BeOfType<CompiledConfigs.AsymmetricKeyConfig>();
    }

    #endregion

    #region Error Cases

    [TestMethod]
    public void Extract_ShouldFail_WhenArgumentCountIsWrong()
    {
        // Arrange
        var (context, invocation) = CreateInvocation("""
            context.MockResponse();
            """);

        // Act
        var result = CompiledConfigExtractor.Extract<CompiledConfigs.MockResponseConfig>(
            invocation, context, "mock-response");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().Contain(d => d.Id == CompilationErrors.ArgumentCountMissMatchForPolicy.Id);
    }

    [TestMethod]
    public void Extract_ShouldFail_WhenArgumentIsNotObjectCreation()
    {
        // Arrange
        var (context, invocation) = CreateInvocation("""
            context.MockResponse(GetConfig());
            """,
            helperMethods: """
            MockResponseConfig GetConfig() => new MockResponseConfig();
            """);

        // Act
        var result = CompiledConfigExtractor.Extract<CompiledConfigs.MockResponseConfig>(
            invocation, context, "mock-response");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().Contain(d => d.Id == CompilationErrors.PolicyArgumentIsNotAnObjectCreation.Id);
    }

    [TestMethod]
    public void Extract_ShouldFail_WhenRequiredPropertyIsMissing()
    {
        // Arrange - CacheLookupValueConfig requires Key
        var (context, invocation) = CreateInvocation("""
            context.CacheLookupValue(new CacheLookupValueConfig 
            { 
                VariableName = "result"
            });
            """);

        // Act
        var result = CompiledConfigExtractor.Extract<CompiledConfigs.CacheLookupValueConfig>(
            invocation, context, "cache-lookup-value");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().Contain(d => d.Id == CompilationErrors.RequiredParameterNotDefined.Id);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Extract_ShouldHandleEmptyCollections()
    {
        // Arrange
        var (context, invocation) = CreateInvocation("""
            context.ValidateJwt(new ValidateJwtConfig 
            { 
                HeaderName = "Authorization",
                Issuers = []
            });
            """);

        // Act
        var result = CompiledConfigExtractor.Extract<CompiledConfigs.ValidateJwtConfig>(
            invocation, context, "validate-jwt");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Issuers.Should().BeEmpty();
    }

    [TestMethod]
    public void Extract_ShouldHandleNullOptionalProperties()
    {
        // Arrange
        var (context, invocation) = CreateInvocation("""
            context.ForwardRequest(new ForwardRequestConfig 
            { 
                Timeout = 30
            });
            """);

        // Act
        var result = CompiledConfigExtractor.Extract<CompiledConfigs.ForwardRequestConfig>(
            invocation, context, "forward-request");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Timeout.Should().NotBeNull();
        result.Value.TimeoutMs.Should().BeNull();
        result.Value.FollowRedirects.Should().BeNull();
    }

    [TestMethod]
    public void Extract_ShouldHandleBooleanProperties()
    {
        // Arrange
        var (context, invocation) = CreateInvocation("""
            context.ForwardRequest(new ForwardRequestConfig 
            { 
                FollowRedirects = true,
                BufferResponse = false
            });
            """);

        // Act
        var result = CompiledConfigExtractor.Extract<CompiledConfigs.ForwardRequestConfig>(
            invocation, context, "forward-request");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FollowRedirects!.Value.ConstantValue.Should().BeTrue();
        result.Value.BufferResponse.Should().BeFalse();
    }

    [TestMethod]
    public void Extract_ShouldHandleIntegerProperties()
    {
        // Arrange
        var (context, invocation) = CreateInvocation("""
            context.MockResponse(new MockResponseConfig 
            { 
                StatusCode = 404
            });
            """);

        // Act
        var result = CompiledConfigExtractor.Extract<CompiledConfigs.MockResponseConfig>(
            invocation, context, "mock-response");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StatusCode.Should().Be(404);
    }

    #endregion

    #region Helper Methods

    private static (TestCompilationContext context, InvocationExpressionSyntax invocation) CreateInvocation(
        string invocationCode,
        string helperMethods = "")
    {
        var code = $$"""
            using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring;
            using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring.Expressions;

            namespace Test;

            [Document]
            public class PolicyDocument : IDocument
            {
                public void Inbound(IInboundContext context) 
                {
                    {{invocationCode}}
                }

                {{helperMethods}}
            }
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            Guid.NewGuid().ToString(),
            syntaxTrees: [syntaxTree],
            references: References);

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var invocation = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First();

        var context = new TestCompilationContext(compilation);
        return (context, invocation);
    }

    private class TestCompilationContext : ICompilationContext
    {
        public TestCompilationContext(Compilation compilation)
        {
            Compilation = compilation;
        }

        public Compilation Compilation { get; }
    }

    #endregion
}
