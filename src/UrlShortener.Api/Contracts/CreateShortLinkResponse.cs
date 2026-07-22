namespace UrlShortener.Api.Contracts;

public record CreateShortLinkResponse(
    string Code,
    string ShortUrl,
    string OriginalUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    bool IsOneTime,
    bool HasPassword);
