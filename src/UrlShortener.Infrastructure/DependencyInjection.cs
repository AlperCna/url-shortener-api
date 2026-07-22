using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Infrastructure.Persistence;

namespace UrlShortener.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("UrlShortenerDb")
            ?? throw new InvalidOperationException("Connection string 'UrlShortenerDb' is not configured.");

        services.AddDbContext<UrlShortenerDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
