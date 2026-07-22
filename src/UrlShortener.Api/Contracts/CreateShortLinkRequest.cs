namespace UrlShortener.Api.Contracts;

public record CreateShortLinkRequest(string Url, DateTimeOffset? ExpiresAt = null, bool IsOneTime = false);
