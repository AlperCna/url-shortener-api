namespace UrlShortener.Core.Entities;

/// <summary>
/// A shortened link and the domain rules that govern whether it can still
/// be resolved: expiration, one-time use, and optional password protection.
/// </summary>
public sealed class ShortLink
{
    public Guid Id { get; private set; }

    public string Code { get; private set; } = null!;

    public string OriginalUrl { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public bool IsOneTime { get; private set; }

    public string? PasswordHash { get; private set; }

    public int ClickCount { get; private set; }

    /// <summary>
    /// False once a one-time link has been used, or the link has been
    /// deactivated for any other reason. Independent of <see cref="ExpiresAt"/>.
    /// </summary>
    public bool IsActive { get; private set; }

    // EF Core materialization.
    private ShortLink()
    {
    }

    public static ShortLink Create(
        string code,
        string originalUrl,
        DateTimeOffset now,
        DateTimeOffset? expiresAt = null,
        bool isOneTime = false,
        string? passwordHash = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code cannot be empty.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            throw new ArgumentException("Original URL cannot be empty.", nameof(originalUrl));
        }

        if (expiresAt is not null && expiresAt <= now)
        {
            throw new ArgumentException("Expiration must be in the future.", nameof(expiresAt));
        }

        return new ShortLink
        {
            Id = Guid.NewGuid(),
            Code = code,
            OriginalUrl = originalUrl,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            IsOneTime = isOneTime,
            PasswordHash = passwordHash,
            ClickCount = 0,
            IsActive = true,
        };
    }

    public bool HasPassword => PasswordHash is not null;

    public bool IsExpired(DateTimeOffset now) => ExpiresAt is not null && now >= ExpiresAt;

    /// <summary>
    /// Whether a redirect should be honored right now: still active and not expired.
    /// Does not take the password check into account — that happens separately,
    /// since a correct password should still work on an accessible link.
    /// </summary>
    public bool IsAccessible(DateTimeOffset now) => IsActive && !IsExpired(now);

    /// <summary>
    /// Records a successful redirect. Deactivates the link afterwards if it is one-time.
    /// </summary>
    public void RegisterClick(DateTimeOffset now)
    {
        if (!IsAccessible(now))
        {
            throw new InvalidOperationException("Cannot register a click on a link that is not accessible.");
        }

        ClickCount++;

        if (IsOneTime)
        {
            IsActive = false;
        }
    }
}
