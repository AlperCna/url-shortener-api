using UrlShortener.Core.Entities;

namespace UrlShortener.Core.Services;

public interface IShortLinkService
{
    /// <summary>
    /// Creates a short link for an already-validated URL. Callers are
    /// expected to run <see cref="Validation.UrlValidator"/> first.
    /// </summary>
    Task<ShortLink> CreateAsync(
        string originalUrl,
        DateTimeOffset? expiresAt,
        bool isOneTime,
        string? password,
        CancellationToken cancellationToken = default);

    Task<ShortLink?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    bool VerifyPassword(ShortLink shortLink, string password);

    /// <summary>
    /// Records a click and persists it. Deactivates the link if it is one-time.
    /// </summary>
    Task RegisterClickAsync(ShortLink shortLink, CancellationToken cancellationToken = default);

    /// <summary>Returns false if no link with that code exists.</summary>
    Task<bool> DeleteAsync(string code, CancellationToken cancellationToken = default);
}
