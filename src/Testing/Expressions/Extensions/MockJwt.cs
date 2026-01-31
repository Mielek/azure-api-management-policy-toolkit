// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.ApiManagement.PolicyToolkit.Authoring.Expressions;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Testing.Expressions;

public class MockJwt : Jwt
{
    public string Id { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public IEnumerable<string> Audiences { get; set; } = [];
    public IReadOnlyDictionary<string, string[]> Claims { get; set; } = new Dictionary<string, string[]>();
    public DateTime? ExpirationTime { get; set; }
    public DateTime? NotBefore { get; set; }
    public DateTime? IssuedAt { get; set; }
}