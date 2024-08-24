using Mielek.Azure.ApiManagement.PolicyToolkit.Authoring;
using Mielek.Azure.ApiManagement.PolicyToolkit.Authoring.Expressions;

using Newtonsoft.Json.Linq;

namespace Mielek.Azure.ApiManagement.PolicyToolkit.CodeContext;

[Document("echo-api.retrieve-resource")]
public class TestingDocument : IDocument
{
    public void Inbound(IInboundContext context)
    {
        context.Base();
        if (IsFromCompanyIp(context.ExpressionContext))
        {
            context.SetHeader("X-Company", "true");
            context.AuthenticationBasic("{{username}}", "{{password}}");
        }
        else
        {
            var testToken = context.AuthenticationManagedIdentity("test");
            context.SetHeader("Authorization", $"Bearer {testToken}");
        }
    }

    public void Outbound(IOutboundContext c)
    {
        c.RemoveHeader("Backend-Statistics");
        c.SetBody(FilterRequest(c.ExpressionContext));
        c.Base();
    }

    bool IsFromCompanyIp(IExpressionContext context) => context.Request.IpAddress.StartsWith("10.0.0.");

    string FilterRequest(IExpressionContext context)
    {
        var body = context.Response.Body.As<JObject>();
        foreach (var internalProperty in new string[] { "location", "secret" })
        {
            if (body.ContainsKey(internalProperty))
            {
                body.Remove(internalProperty);
            }
        }
        return body.ToString();
    }

}