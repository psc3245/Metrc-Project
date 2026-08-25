using API.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Unique per factory instance, shared by every test in a test class since
    // IClassFixture creates one factory per class.
    public string DbName { get; } = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Self-contained JWT/config so these tests never depend on
        // appsettings.Development.json existing or containing any particular
        // values. The same key configured here is what Program.cs's TokenService
        // (issuing) AND JwtBearer handler (validating) both read from - since
        // they pull from the same IConfiguration, they stay consistent automatically.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // ["Jwt:Key"] = "integration-test-signing-key-at-least-32-chars-long",
                // ["Jwt:Issuer"] = "MetrcApi.Tests",
                // ["Jwt:Audience"] = "MetrcApi.Tests.Client",
                // ["Jwt:ExpiryMinutes"] = "60",
                // Never actually connected to - the real DbContext registration
                // is removed and replaced below - this just guards against any
                // code path that might resolve the connection string before that swap.
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=1;Database=unused;Username=x;Password=x"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the real Npgsql-backed registration and replace it with an
            // isolated in-memory one, so these tests run without a live Postgres
            // instance or Docker.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(DbName);
            });
        });
    }
}