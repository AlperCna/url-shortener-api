using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Core.Repositories;
using UrlShortener.Core.Security;
using UrlShortener.Infrastructure.Persistence;
using UrlShortener.Infrastructure.Repositories;
using UrlShortener.Infrastructure.Security;

namespace UrlShortener.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("UrlShortenerDb")
            ?? throw new InvalidOperationException("Connection string 'UrlShortenerDb' is not configured.");

        services.AddDbContext<UrlShortenerDbContext>(options => options.UseNpgsql(connectionString));

        services.AddMemoryCache();
        services.AddScoped<ShortLinkRepository>();
        services.AddScoped<IShortLinkRepository, CachedShortLinkRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();

        return services;
    }
}
