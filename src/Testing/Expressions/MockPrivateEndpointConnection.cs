// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring.Expressions;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Testing.Expressions;

public class MockPrivateEndpointConnection : IPrivateEndpointConnection
{
    public string Name { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
}