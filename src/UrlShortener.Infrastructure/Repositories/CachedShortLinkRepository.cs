using Microsoft.Extensions.Caching.Memory;
using UrlShortener.Core.Entities;
using UrlShortener.Core.Repositories;

namespace UrlShortener.Infrastructure.Repositories;

/// <summary>
/// Cache-aside decorator around <see cref="ShortLinkRepository"/> for the
/// redirect hot path. IMemoryCache stores object references, not copies, so
/// a mutation made through <see cref="Update"/> to a cached instance is
/// visible to the next reader without any explicit invalidation - only
/// <see cref="Remove"/> needs to evict, since after that the code should
/// resolve to nothing at all.
/// </summary>
public class CachedShortLinkRepository(ShortLinkRepository inner, IMemoryCache cache) : IShortLinkRepository
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<ShortLink?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue<ShortLink>(CacheKey(code), out var cached))
        {
            return cached;
        }

        var shortLink = await inner.GetByCodeAsync(code, cancellationToken);

        if (shortLink is not null)
        {
            cache.Set(CacheKey(code), shortLink, CacheDuration);
        }

        return shortLink;
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        inner.CodeExistsAsync(code, cancellationToken);

    public Task AddAsync(ShortLink shortLink, CancellationToken cancellationToken = default) =>
        inner.AddAsync(shortLink, cancellationToken);

    public void Update(ShortLink shortLink) => inner.Update(shortLink);

    public void Remove(ShortLink shortLink)
    {
        cache.Remove(CacheKey(shortLink.Code));
        inner.Remove(shortLink);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        inner.SaveChangesAsync(cancellationToken);

    private static string CacheKey(string code) => $"shortlink:{code}";
}
