// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;

using CompiledConfigs = Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Configs;

namespace Microsoft.Azure.ApiManagement.PolicyToolkit.Compiling.Policy;

public static class ClaimsConfigCompiler
{
    public static XElement HandleRequiredClaims(IReadOnlyList<CompiledConfigs.ClaimConfig> claims)
    {
        XElement claimsElement = new("required-claims");
        foreach (var claim in claims)
        {
            XElement claimElement = new("claim");
            claimElement.Add(new XAttribute("name", claim.Name));
            claimElement.TryAddAttribute("match", claim.Match);
            claimElement.TryAddAttribute("separator", claim.Separator);

            if (claim.Values is { } values)
            {
                foreach (var value in values)
                {
                    claimElement.Add(new XElement("value", value));
                }
            }

            claimsElement.Add(claimElement);
        }

        return claimsElement;
    }
}