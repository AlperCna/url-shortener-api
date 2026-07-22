using UrlShortener.Core.Entities;

namespace UrlShortener.Core.Repositories;

public interface IShortLinkRepository
{
    Task<ShortLink?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

    Task AddAsync(ShortLink shortLink, CancellationToken cancellationToken = default);

    void Remove(ShortLink shortLink);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
