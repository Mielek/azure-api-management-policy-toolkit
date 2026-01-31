// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using FluentAssertions;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling;
using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Tests.Results;

[TestClass]
public class ResultExtensionsTests
{
    private static Diagnostic CreateDiagnostic(string id, string message)
    {
        return Diagnostic.Create(
            new DiagnosticDescriptor(
                id,
                "Test",
                message,
                "Test",
                DiagnosticSeverity.Error,
                true),
            Location.None);
    }

    #region Collect

    [TestMethod]
    public void Collect_AllSuccess_ShouldReturnListOfValues()
    {
        var results = new[]
        {
            Result<int>.Success(1),
            Result<int>.Success(2),
            Result<int>.Success(3)
        };

        var collected = results.Collect();

        collected.IsSuccess.Should().BeTrue();
        collected.Value.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [TestMethod]
    public void Collect_SomeFailures_ShouldAccumulateAllDiagnostics()
    {
        var diag1 = CreateDiagnostic("TEST001", "Error 1");
        var diag2 = CreateDiagnostic("TEST002", "Error 2");
        var results = new[]
        {
            Result<int>.Success(1),
            Result<int>.Failure(diag1),
            Result<int>.Success(3),
            Result<int>.Failure(diag2)
        };

        var collected = results.Collect();

        collected.IsFailure.Should().BeTrue();
        collected.Diagnostics.Should().HaveCount(2);
        collected.Diagnostics.Should().Contain(diag1);
        collected.Diagnostics.Should().Contain(diag2);
    }

    [TestMethod]
    public void Collect_EmptySequence_ShouldReturnEmptyList()
    {
        var results = Enumerable.Empty<Result<int>>();

        var collected = results.Collect();

        collected.IsSuccess.Should().BeTrue();
        collected.Value.Should().BeEmpty();
    }

    #endregion

    #region ReportAll

    [TestMethod]
    public void ReportAll_ShouldCallContextReportForEachDiagnostic()
    {
        var context = new TestDocumentCompilationContext();
        var diag1 = CreateDiagnostic("TEST001", "Error 1");
        var diag2 = CreateDiagnostic("TEST002", "Error 2");
        var result = Result<string>.Failure(diag1, diag2);

        result.ReportAll(context);

        context.Diagnostics.Should().HaveCount(2);
        context.Diagnostics.Should().Contain(diag1);
        context.Diagnostics.Should().Contain(diag2);
    }

    [TestMethod]
    public void ReportAll_OnSuccess_ShouldNotCallReport()
    {
        var context = new TestDocumentCompilationContext();
        var result = Result<string>.Success("test");

        result.ReportAll(context);

        context.Diagnostics.Should().BeEmpty();
    }

    #endregion

    #region GetValueOrDefault

    [TestMethod]
    public void GetValueOrDefault_OnSuccess_ShouldReturnValue()
    {
        var result = Result<string>.Success("actual");

        var value = result.GetValueOrDefault("default");

        value.Should().Be("actual");
    }

    [TestMethod]
    public void GetValueOrDefault_OnFailure_ShouldReturnDefault()
    {
        var result = Result<string>.Failure(CreateDiagnostic("TEST001", "Error"));

        var value = result.GetValueOrDefault("default");

        value.Should().Be("default");
    }

    #endregion

    #region GetValueOrElse

    [TestMethod]
    public void GetValueOrElse_OnSuccess_ShouldReturnValue()
    {
        var result = Result<string>.Success("actual");
        var factoryCalled = false;

        var value = result.GetValueOrElse(() =>
        {
            factoryCalled = true;
            return "default";
        });

        value.Should().Be("actual");
        factoryCalled.Should().BeFalse();
    }

    [TestMethod]
    public void GetValueOrElse_OnFailure_ShouldCallFactory()
    {
        var result = Result<string>.Failure(CreateDiagnostic("TEST001", "Error"));
        var factoryCalled = false;

        var value = result.GetValueOrElse(() =>
        {
            factoryCalled = true;
            return "default";
        });

        value.Should().Be("default");
        factoryCalled.Should().BeTrue();
    }

    #endregion

    #region ToNullable

    [TestMethod]
    public void ToNullable_OnSuccess_ShouldReturnValue()
    {
        var result = Result<string>.Success("value");

        var nullable = result.ToNullable();

        nullable.Should().Be("value");
    }

    [TestMethod]
    public void ToNullable_OnFailure_ShouldReturnNull()
    {
        var result = Result<string>.Failure(CreateDiagnostic("TEST001", "Error"));

        var nullable = result.ToNullable();

        nullable.Should().BeNull();
    }

    #endregion

    #region ToNullableValue

    [TestMethod]
    public void ToNullableValue_OnSuccess_ShouldReturnValue()
    {
        var result = Result<int>.Success(42);

        var nullable = result.ToNullableValue();

        nullable.Should().Be(42);
    }

    [TestMethod]
    public void ToNullableValue_OnFailure_ShouldReturnNull()
    {
        var result = Result<int>.Failure(CreateDiagnostic("TEST001", "Error"));

        var nullable = result.ToNullableValue();

        nullable.Should().BeNull();
    }

    #endregion

    #region Where

    [TestMethod]
    public void Where_PredicateTrue_ShouldReturnOriginalResult()
    {
        var result = Result<int>.Success(42);

        var filtered = result.Where(
            v => v > 0,
            () => CreateDiagnostic("TEST001", "Must be positive"));

        filtered.IsSuccess.Should().BeTrue();
        filtered.Value.Should().Be(42);
    }

    [TestMethod]
    public void Where_PredicateFalse_ShouldReturnFailure()
    {
        var result = Result<int>.Success(-5);
        var diag = CreateDiagnostic("TEST001", "Must be positive");

        var filtered = result.Where(
            v => v > 0,
            () => diag);

        filtered.IsFailure.Should().BeTrue();
        filtered.Diagnostics.Should().Contain(diag);
    }

    [TestMethod]
    public void Where_OnFailure_ShouldPreserveFailure()
    {
        var originalDiag = CreateDiagnostic("TEST001", "Original error");
        var result = Result<int>.Failure(originalDiag);

        var filtered = result.Where(
            v => v > 0,
            () => CreateDiagnostic("TEST002", "Should not be used"));

        filtered.IsFailure.Should().BeTrue();
        filtered.Diagnostics.Should().Contain(originalDiag);
        filtered.Diagnostics.Should().HaveCount(1);
    }

    #endregion

    /// <summary>
    /// Simple test implementation of IDocumentCompilationContext for testing ReportAll.
    /// </summary>
    private class TestDocumentCompilationContext : IDocumentCompilationContext
    {
        public Compilation Compilation => throw new NotImplementedException();
        public SyntaxNode SyntaxRoot => throw new NotImplementedException();
        public XElement RootElement => throw new NotImplementedException();
        public XElement CurrentElement => throw new NotImplementedException();
        public IList<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();

        public void AddPolicy(XNode element) => throw new NotImplementedException();
        public void Report(Diagnostic diagnostic) => Diagnostics.Add(diagnostic);
    }
}
