using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Updraft.Tests;

public sealed class UpdraftWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Override only the Bearer signing key/issuer/audience so tests can mint valid tokens
        // without the user-secrets dev key. The real DbContext and repositories are left intact.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Schemes:Bearer:ValidIssuer"] = TestAuth.Issuer,
                ["Authentication:Schemes:Bearer:ValidAudiences:0"] = TestAuth.Audience,
                ["Authentication:Schemes:Bearer:SigningKeys:0:Issuer"] = TestAuth.Issuer,
                ["Authentication:Schemes:Bearer:SigningKeys:0:Value"] =
                    Convert.ToBase64String(TestAuth.SigningKeyBytes),
            });
        });
    }
}
