using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using UrlShortener.Infrastructure.Persistence;
using Xunit;

namespace UrlShortener.IntegrationTests;

/// <summary>
/// Boots the real Api host against a real, disposable Postgres instance
/// (via Testcontainers) instead of a fake/in-memory database, so these
/// tests exercise the actual EF Core provider, migrations and SQL.
/// </summary>
public class ApiFactoryFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("urlshortener")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Program.cs already registered a DbContext pointed at the
            // appsettings connection string - swap it for the container's.
            services.RemoveAll<DbContextOptions<UrlShortenerDbContext>>();
            services.AddDbContext<UrlShortenerDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });
    }

    public Task InitializeAsync() => _postgres.StartAsync();

    Task IAsyncLifetime.DisposeAsync() => _postgres.StopAsync();
}
