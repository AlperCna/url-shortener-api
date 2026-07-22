using UrlShortener.Core.Entities;

namespace UrlShortener.Core.Services;

public interface IShortLinkService
{
    /// <summary>
    /// Creates a short link for an already-validated URL. Callers are
    /// expected to run <see cref="Validation.UrlValidator"/> first.
    /// </summary>
    Task<ShortLink> CreateAsync(string originalUrl, CancellationToken cancellationToken = default);

    Task<ShortLink?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
