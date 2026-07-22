using UrlShortener.Core.Entities;

namespace UrlShortener.Core.Repositories;

public interface IShortLinkRepository
{
    Task<ShortLink?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

    Task AddAsync(ShortLink shortLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a possibly-detached instance (e.g. one handed back from a cache
    /// in a different DbContext scope) for persistence on the next save.
    /// </summary>
    void Update(ShortLink shortLink);

    void Remove(ShortLink shortLink);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
