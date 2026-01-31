// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FluentAssertions;

using Microsoft.Azure.ApiManagement.PolicyToolkit.Results;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Tests.Results;

[TestClass]
public class ResultTests
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

    #region Success Factory

    [TestMethod]
    public void Success_ShouldPreserveValue()
    {
        var result = Result<string>.Success("test-value");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test-value");
    }

    [TestMethod]
    public void Success_ShouldHaveEmptyDiagnostics()
    {
        var result = Result<string>.Success("test");

        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void Success_IsFailure_ShouldBeFalse()
    {
        var result = Result<int>.Success(42);

        result.IsFailure.Should().BeFalse();
    }

    #endregion

    #region Failure Factory

    [TestMethod]
    public void Failure_ShouldCaptureAllDiagnostics()
    {
        var diag1 = CreateDiagnostic("TEST001", "Error 1");
        var diag2 = CreateDiagnostic("TEST002", "Error 2");

        var result = Result<string>.Failure(diag1, diag2);

        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().HaveCount(2);
        result.Diagnostics.Should().Contain(diag1);
        result.Diagnostics.Should().Contain(diag2);
    }

    [TestMethod]
    public void Failure_IsFailure_ShouldBeTrue()
    {
        var diag = CreateDiagnostic("TEST001", "Error");

        var result = Result<string>.Failure(diag);

        result.IsFailure.Should().BeTrue();
    }

    [TestMethod]
    public void Failure_AccessingValue_ShouldThrow()
    {
        var result = Result<string>.Failure(CreateDiagnostic("TEST001", "Error"));

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*failed Result*");
    }

    [TestMethod]
    public void Failure_WithEnumerable_ShouldCaptureAllDiagnostics()
    {
        var diagnostics = new[]
        {
            CreateDiagnostic("TEST001", "Error 1"),
            CreateDiagnostic("TEST002", "Error 2"),
            CreateDiagnostic("TEST003", "Error 3")
        };

        var result = Result<string>.Failure(diagnostics.AsEnumerable());

        result.Diagnostics.Should().HaveCount(3);
    }

    #endregion

    #region Map

    [TestMethod]
    public void Map_OnSuccess_ShouldTransformValue()
    {
        var result = Result<int>.Success(5);

        var mapped = result.Map(x => x * 2);

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(10);
    }

    [TestMethod]
    public void Map_OnFailure_ShouldPreserveDiagnostics()
    {
        var diag = CreateDiagnostic("TEST001", "Error");
        var result = Result<int>.Failure(diag);

        var mapped = result.Map(x => x * 2);

        mapped.IsFailure.Should().BeTrue();
        mapped.Diagnostics.Should().Contain(diag);
    }

    #endregion

    #region Bind

    [TestMethod]
    public void Bind_OnSuccess_ShouldChainOperation()
    {
        var result = Result<int>.Success(5);

        var bound = result.Bind(x => Result<string>.Success($"Value: {x}"));

        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("Value: 5");
    }

    [TestMethod]
    public void Bind_OnSuccess_WithFailingBinder_ShouldReturnFailure()
    {
        var result = Result<int>.Success(5);
        var diag = CreateDiagnostic("TEST001", "Binder failed");

        var bound = result.Bind(_ => Result<string>.Failure(diag));

        bound.IsFailure.Should().BeTrue();
        bound.Diagnostics.Should().Contain(diag);
    }

    [TestMethod]
    public void Bind_OnFailure_ShouldPreserveDiagnostics()
    {
        var diag = CreateDiagnostic("TEST001", "Original error");
        var result = Result<int>.Failure(diag);

        var bound = result.Bind(x => Result<string>.Success("Should not reach"));

        bound.IsFailure.Should().BeTrue();
        bound.Diagnostics.Should().Contain(diag);
    }

    #endregion

    #region Match

    [TestMethod]
    public void Match_OnSuccess_ShouldCallSuccessBranch()
    {
        var result = Result<int>.Success(42);

        var output = result.Match(
            onSuccess: v => $"Success: {v}",
            onFailure: _ => "Failure");

        output.Should().Be("Success: 42");
    }

    [TestMethod]
    public void Match_OnFailure_ShouldCallFailureBranch()
    {
        var diag = CreateDiagnostic("TEST001", "Error message");
        var result = Result<int>.Failure(diag);

        var output = result.Match(
            onSuccess: v => $"Success: {v}",
            onFailure: d => $"Failure: {d.Count}");

        output.Should().Be("Failure: 1");
    }

    [TestMethod]
    public void MatchAction_OnSuccess_ShouldCallSuccessBranch()
    {
        var result = Result<int>.Success(42);
        var successCalled = false;
        var failureCalled = false;

        result.Match(
            onSuccess: _ => successCalled = true,
            onFailure: _ => failureCalled = true);

        successCalled.Should().BeTrue();
        failureCalled.Should().BeFalse();
    }

    [TestMethod]
    public void MatchAction_OnFailure_ShouldCallFailureBranch()
    {
        var result = Result<int>.Failure(CreateDiagnostic("TEST001", "Error"));
        var successCalled = false;
        var failureCalled = false;

        result.Match(
            onSuccess: _ => successCalled = true,
            onFailure: _ => failureCalled = true);

        successCalled.Should().BeFalse();
        failureCalled.Should().BeTrue();
    }

    #endregion

    #region Combine

    [TestMethod]
    public void Combine_AllSuccess_ShouldReturnSuccessWithTuple()
    {
        var r1 = Result<int>.Success(1);
        var r2 = Result<string>.Success("two");

        var combined = Result.Combine(r1, r2);

        combined.IsSuccess.Should().BeTrue();
        combined.Value.Should().Be((1, "two"));
    }

    [TestMethod]
    public void Combine_OneFailure_ShouldReturnFailure()
    {
        var r1 = Result<int>.Success(1);
        var r2 = Result<string>.Failure(CreateDiagnostic("TEST001", "Error"));

        var combined = Result.Combine(r1, r2);

        combined.IsFailure.Should().BeTrue();
    }

    [TestMethod]
    public void Combine_MultipleFailures_ShouldAccumulateAllDiagnostics()
    {
        var diag1 = CreateDiagnostic("TEST001", "Error 1");
        var diag2 = CreateDiagnostic("TEST002", "Error 2");
        var r1 = Result<int>.Failure(diag1);
        var r2 = Result<string>.Failure(diag2);

        var combined = Result.Combine(r1, r2);

        combined.IsFailure.Should().BeTrue();
        combined.Diagnostics.Should().HaveCount(2);
        combined.Diagnostics.Should().Contain(diag1);
        combined.Diagnostics.Should().Contain(diag2);
    }

    [TestMethod]
    public void Combine_ThreeResults_AllSuccess_ShouldReturnTuple()
    {
        var r1 = Result<int>.Success(1);
        var r2 = Result<string>.Success("two");
        var r3 = Result<double>.Success(3.0);

        var combined = Result.Combine(r1, r2, r3);

        combined.IsSuccess.Should().BeTrue();
        combined.Value.Should().Be((1, "two", 3.0));
    }

    [TestMethod]
    public void Combine_FourResults_AllSuccess_ShouldReturnTuple()
    {
        var r1 = Result<int>.Success(1);
        var r2 = Result<string>.Success("two");
        var r3 = Result<double>.Success(3.0);
        var r4 = Result<bool>.Success(true);

        var combined = Result.Combine(r1, r2, r3, r4);

        combined.IsSuccess.Should().BeTrue();
        combined.Value.Should().Be((1, "two", 3.0, true));
    }

    [TestMethod]
    public void Combine_ArrayOfResults_AllSuccess_ShouldReturnList()
    {
        var results = new[]
        {
            Result<int>.Success(1),
            Result<int>.Success(2),
            Result<int>.Success(3)
        };

        var combined = Result.Combine(results);

        combined.IsSuccess.Should().BeTrue();
        combined.Value.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    #endregion

    #region Implicit Conversion

    [TestMethod]
    public void ImplicitConversion_FromValue_ShouldCreateSuccess()
    {
        Result<string> result = "test-value";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test-value");
    }

    #endregion
}
