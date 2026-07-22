namespace UrlShortener.Api.Contracts;

public record ShortLinkStatsResponse(
    string Code,
    int ClickCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    bool IsOneTime,
    bool IsActive);
