using ElevateHire.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ElevateHire.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Integration tests run against the real PostgreSQL database defined in
        // appsettings.json. Start the database with: docker compose up -d (see docker/).
        builder.UseEnvironment("Development");
    }

    public HttpClient CreateNoRedirectClient()
    {
        var handler = Server.CreateHandler();
        return new HttpClient(handler)
        {
            BaseAddress = Server.BaseAddress
        };
    }
}