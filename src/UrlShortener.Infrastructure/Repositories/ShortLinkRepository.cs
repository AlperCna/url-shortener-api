using Microsoft.EntityFrameworkCore;
using UrlShortener.Core.Entities;
using UrlShortener.Core.Repositories;
using UrlShortener.Infrastructure.Persistence;

namespace UrlShortener.Infrastructure.Repositories;

public class ShortLinkRepository(UrlShortenerDbContext dbContext) : IShortLinkRepository
{
    public Task<ShortLink?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        dbContext.ShortLinks.SingleOrDefaultAsync(l => l.Code == code, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        dbContext.ShortLinks.AnyAsync(l => l.Code == code, cancellationToken);

    public Task AddAsync(ShortLink shortLink, CancellationToken cancellationToken = default)
    {
        dbContext.ShortLinks.Add(shortLink);
        return Task.CompletedTask;
    }

    public void Remove(ShortLink shortLink) => dbContext.ShortLinks.Remove(shortLink);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
