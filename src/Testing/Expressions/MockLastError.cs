// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring.Expressions;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Testing.Expressions;

public class MockLastError : ILastError
{
    public string Source { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string PolicyId { get; set; } = string.Empty;
}